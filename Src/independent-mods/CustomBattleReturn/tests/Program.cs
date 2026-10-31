using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.CustomBattle;
using TaleWorlds.MountAndBlade.GauntletUI.Mission.Singleplayer;

namespace CustomBattleReturn.Tests
{
    internal static class Program
    {
        private static int _checks;
        private static void Check(bool condition, string message)
        {
            if (!condition) throw new Exception(message);
            _checks++;
        }

        private static MissionGauntletSingleplayerEscapeMenu Setup()
        {
            var mission = new Mission();
            var manager = new GameStateManager();
            manager.States.Add(new CustomBattleState());
            manager.States.Add(new MissionState { CurrentMission = mission });
            Game.Current = new Game { GameType = new CustomGame(), GameStateManager = manager };
            MBGameManager.Current = new MBGameManager();
            GameNetwork.IsMultiplayer = false;
            GameNetwork.IsReplay = false;
            Mission.Current = mission;
            return new MissionGauntletSingleplayerEscapeMenu { Mission = mission };
        }

        private static void Rejected(string name, Action<MissionGauntletSingleplayerEscapeMenu> mutate)
        {
            var menu = Setup();
            mutate(menu);
            Check(!ReturnRequest.TryCreate(menu.Mission, out _), name + " request rejected");
            Check(menu.BuildItems().Count == 2, name + " menu unchanged");
        }

        private static int Main(string[] args)
        {
            try
            {
                if (args.Length == 1) return NativeApiProbe.Run(args[0]);
                var module = new SubModule();
                module.Load(); // Real Harmony patches the double's native menu method.
                MenuAndLifecycle();
                Guards();
                StaleAndReentrant();
                var menu = Setup();
                module.Unload();
                Check(menu.BuildItems().Count == 2, "module unload removes its menu postfix");
                module.Load();
                Check(Setup().BuildItems().Count == 3, "reload installs one postfix");
                module.Unload();
                Console.WriteLine("PASS: " + _checks + " assertions; production source + engine doubles + actual Harmony.");
                return 0;
            }
            catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
        }

        private static void MenuAndLifecycle()
        {
            var menu = Setup();
            var manager = Game.Current.GameStateManager;
            var game = Game.Current;
            var setup = (CustomBattleState)manager.States[0];
            var settings = setup.Settings;
            var items = menu.BuildItems();
            Check(items.Count == 3, "one return row inserted");
            Check(ReferenceEquals(items[0], menu.Resume) && ReferenceEquals(items[2], menu.MainMenu), "native rows preserved");
            Check(items[1].Name.Value.Contains("Return to Custom Battle"), "player-facing return text");
            Check(!items[1].Disabled().Item1, "eligible row enabled");
            Check(menu.BuildItems().Count == 3, "reopening does not accumulate duplicate rows");
            items[1].Action(null);
            Check(menu.CloseCalls == 1 && !menu.PauseActive, "pause closed once");
            Check(string.Join(",", menu.Mission.Events) == "close,end", "resume engine before ending mission");
            Check(menu.Mission.EndCalls == 1, "mission ended once");
            Check(menu.Mission.CurrentState == Mission.State.EndingNextFrame, "native deferred ending requested");
            Check(ReferenceEquals(manager.ActiveState, manager.States[1]), "mod does not directly pop mission");
            Check(items[1].Disabled().Item1, "consumed request disabled");
            items[1].Action(null);
            Check(menu.Mission.EndCalls == 1 && menu.CloseCalls == 1, "double click rejected");

            // Simulate the documented native teardown/pop, not its internal implementation.
            menu.Mission.CurrentState = Mission.State.Over;
            manager.States.RemoveAt(1);
            Check(ReferenceEquals(manager.ActiveState, setup), "retained setup exposed after simulated native pop");
            Check(ReferenceEquals(setup.Settings, settings), "settings object never reset or copied");
            Check(ReferenceEquals(Game.Current, game) && !MBGameManager.Current.IsEnding, "same custom game remains alive");

            var secondMission = new Mission();
            Mission.Current = secondMission;
            manager.States.Add(new MissionState { CurrentMission = secondMission });
            var secondMenu = new MissionGauntletSingleplayerEscapeMenu { Mission = secondMission };
            Check(secondMenu.BuildItems().Count == 3, "second battle gets return row");
            items[1].Action(null);
            Check(secondMission.EndCalls == 0, "first battle callback cannot end second battle");
            secondMenu.BuildItems()[1].Action(null);
            Check(secondMission.EndCalls == 1 && ReferenceEquals(setup.Settings, settings), "second battle return retains setup");
        }

        private static void Guards()
        {
            Rejected("no game", _ => Game.Current = null);
            Rejected("no state manager", _ => Game.Current.GameStateManager = null);
            Rejected("campaign", _ => Game.Current.GameType = new GameType());
            Rejected("multiplayer", _ => GameNetwork.IsMultiplayer = true);
            Rejected("replay", _ => GameNetwork.IsReplay = true);
            Rejected("missing game manager", _ => MBGameManager.Current = null);
            Rejected("game shutting down", _ => MBGameManager.Current.IsEnding = true);
            Rejected("missing mission", menu => menu.Mission = null);
            Rejected("different current mission", _ => Mission.Current = new Mission());
            Rejected("ending mission", menu => menu.Mission.CurrentState = Mission.State.EndingNextFrame);
            Rejected("completed mission", menu => menu.Mission.CurrentState = Mission.State.Over);
            Rejected("not initialized", menu => menu.Mission.CurrentState = Mission.State.New);
            Rejected("missing setup", _ => Game.Current.GameStateManager.States.RemoveAt(0));
            Rejected("intervening state", _ => Game.Current.GameStateManager.States.Insert(1, new GameState()));
            Rejected("mission not active", _ => Game.Current.GameStateManager.States.Add(new GameState()));
            Rejected("state mission mismatch", _ => ((MissionState)Game.Current.GameStateManager.ActiveState).CurrentMission = new Mission());
            Rejected("empty stack", _ => Game.Current.GameStateManager.States.Clear());
            var menu = Setup();
            ReturnMenuPatch.Postfix(menu, null);
            Check(menu.Mission.EndCalls == 0, "null row list safely ignored");
        }

        private static void StaleAndReentrant()
        {
            var changes = new List<Action>
            {
                () => Game.Current.GameStateManager = new GameStateManager(),
                () => Game.Current.GameStateManager.States[0] = new CustomBattleState(),
                () => Game.Current.GameStateManager.States.Add(new GameState()),
                () => ((MissionState)Game.Current.GameStateManager.ActiveState).CurrentMission = new Mission(),
                () => Game.Current.GameType = new GameType(),
                () => GameNetwork.IsMultiplayer = true,
                () => GameNetwork.IsReplay = true,
                () => MBGameManager.Current.IsEnding = true,
                () => Mission.Current.CurrentState = Mission.State.EndingNextFrame,
                () => Game.Current = null
            };
            foreach (var change in changes)
            {
                var menu = Setup();
                var row = menu.BuildItems()[1];
                change();
                Check(row.Disabled().Item1, "stale row disabled");
                row.Action(null);
                Check(menu.Mission.EndCalls == 0 && menu.CloseCalls == 0, "stale callback has no effects");
            }

            var active = Setup();
            Check(ReturnRequest.TryCreate(active.Mission, out var request), "valid request created");
            bool nestedAccepted = true;
            Check(request.Execute(() => nestedAccepted = request.Execute(() => { })), "first request accepted");
            Check(!nestedAccepted && active.Mission.EndCalls == 1, "reentrant request rejected before close callback");
            var handoff = Setup();
            ReturnRequest.TryCreate(handoff.Mission, out var changed);
            Check(!changed.Execute(() => Mission.Current = new Mission()), "ownership change during menu close rejected");
            Check(handoff.Mission.EndCalls == 0, "old mission not ended after close-menu handoff");
            var incomplete = Setup();
            ReturnRequest.TryCreate(incomplete.Mission, out var failed);
            try { failed.Execute(() => throw new InvalidOperationException("close failed")); }
            catch (InvalidOperationException) { }
            Check(incomplete.Mission.EndCalls == 0, "failed pause cleanup cannot end mission");
            Check(!failed.Execute(() => { }), "failed action cannot be repeated from stale menu");
        }
    }
}
