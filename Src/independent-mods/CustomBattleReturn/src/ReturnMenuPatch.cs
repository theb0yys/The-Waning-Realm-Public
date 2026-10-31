using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.CustomBattle;
using TaleWorlds.MountAndBlade.GauntletUI.Mission;
using TaleWorlds.MountAndBlade.GauntletUI.Mission.Singleplayer;
using TaleWorlds.MountAndBlade.ViewModelCollection.EscapeMenu;

namespace CustomBattleReturn
{
    internal static class ReturnMenuPatch
    {
        internal static Func<MissionGauntletEscapeMenuBase, bool, bool> CloseMenu;

        public static void Postfix(MissionGauntletSingleplayerEscapeMenu __instance, List<EscapeMenuItemVM> __result)
        {
            if (__result == null || CloseMenu == null || !ReturnRequest.TryCreate(__instance.Mission, out var request))
                return;

            __result.Insert(Math.Min(1, __result.Count), new EscapeMenuItemVM(
                new TextObject("{=cbr_return_to_setup}Return to Custom Battle"),
                _ => request.Execute(() => CloseMenu(__instance, false)), null,
                () => new Tuple<bool, TextObject>(!request.IsAvailable, null), false));
        }
    }

    // Each menu opening owns its request. No mission, setup, or selection is cached globally.
    internal sealed class ReturnRequest
    {
        private readonly GameStateManager _manager;
        private readonly MissionState _missionState;
        private readonly CustomBattleState _setupState;
        private readonly Mission _mission;
        private bool _requested;

        private ReturnRequest(GameStateManager manager, MissionState missionState, CustomBattleState setupState, Mission mission)
        {
            _manager = manager;
            _missionState = missionState;
            _setupState = setupState;
            _mission = mission;
        }

        internal static bool TryCreate(Mission mission, out ReturnRequest request)
        {
            request = null;
            var manager = Game.Current?.GameStateManager;
            if (!IsCustomBattle(mission, manager) || !(manager.ActiveState is MissionState missionState) ||
                !ReferenceEquals(missionState.CurrentMission, mission))
                return false;

            GameState previous = null;
            foreach (var state in manager.GameStates)
            {
                if (ReferenceEquals(state, missionState))
                {
                    if (!(previous is CustomBattleState setupState))
                        return false;
                    request = new ReturnRequest(manager, missionState, setupState, mission);
                    return true;
                }
                previous = state;
            }
            return false;
        }

        internal bool IsAvailable => !_requested && HasCurrentContext();

        private bool HasCurrentContext()
        {
                if (!ReferenceEquals(Game.Current?.GameStateManager, _manager) ||
                    !ReferenceEquals(_manager.ActiveState, _missionState) ||
                    !ReferenceEquals(_missionState.CurrentMission, _mission) || !IsCustomBattle(_mission, _manager))
                    return false;

                GameState previous = null;
                foreach (var state in _manager.GameStates)
                {
                    if (ReferenceEquals(state, _missionState))
                        return ReferenceEquals(previous, _setupState);
                    previous = state;
                }
                return false;
        }

        internal bool Execute(Action closeMenu)
        {
            if (!IsAvailable)
                return false;

            _requested = true;
            // This resumes the engine and releases native pause/input ownership before ending.
            closeMenu();
            // Native close-menu notifications may have changed mission or screen ownership.
            if (!HasCurrentContext())
                return false;
            _mission.EndMission();
            // MissionState performs the native cleanup/pop on a later tick. Keep its predecessor alive.
            return true;
        }

        private static bool IsCustomBattle(Mission mission, GameStateManager manager)
        {
            return manager != null && Game.Current.GameType is CustomGame && !GameNetwork.IsMultiplayer &&
                !GameNetwork.IsReplay && MBGameManager.Current != null && !MBGameManager.Current.IsEnding &&
                mission != null && ReferenceEquals(Mission.Current, mission) && mission.CurrentState == Mission.State.Continuing;
        }
    }
}
