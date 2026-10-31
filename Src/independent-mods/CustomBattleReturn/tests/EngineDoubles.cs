using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

// Only the managed contract used by the mod. No native UI/engine acceptance is implied.
namespace TaleWorlds.Core
{
    public class GameType { }
    public class Game
    {
        public static Game Current;
        public GameType GameType;
        public GameStateManager GameStateManager;
    }
    public class GameState { }
    public class GameStateManager
    {
        public readonly List<GameState> States = new List<GameState>();
        public IEnumerable<GameState> GameStates => States.AsReadOnly();
        public GameState ActiveState => States.Count == 0 ? null : States[States.Count - 1];
    }
}
namespace TaleWorlds.Localization
{
    public class TextObject
    {
        public readonly string Value;
        public TextObject(string value) { Value = value; }
    }
}
namespace TaleWorlds.MountAndBlade
{
    public class MBSubModuleBase
    {
        protected virtual void OnSubModuleLoad() { }
        protected virtual void OnSubModuleUnloaded() { }
        public void Load() { OnSubModuleLoad(); }
        public void Unload() { OnSubModuleUnloaded(); }
    }
    public class MBGameManager
    {
        public static MBGameManager Current;
        public bool IsEnding;
    }
    public static class GameNetwork
    {
        public static bool IsMultiplayer;
        public static bool IsReplay;
    }
    public class Mission
    {
        public enum State { New, Continuing, EndingNextFrame, Over }
        public static Mission Current;
        public State CurrentState = State.Continuing;
        public int EndCalls;
        public Action OnEnd;
        public readonly List<string> Events = new List<string>();
        public void EndMission()
        {
            EndCalls++;
            Events.Add("end");
            OnEnd?.Invoke();
            CurrentState = State.EndingNextFrame;
        }
    }
    public class MissionState : TaleWorlds.Core.GameState
    {
        public Mission CurrentMission;
    }
}
namespace TaleWorlds.MountAndBlade.CustomBattle
{
    public class CustomGame : TaleWorlds.Core.GameType { }
    public class CustomBattleState : TaleWorlds.Core.GameState
    {
        public readonly object Settings = new object();
    }
}
namespace TaleWorlds.MountAndBlade.ViewModelCollection.EscapeMenu
{
    public class EscapeMenuItemVM
    {
        public readonly TaleWorlds.Localization.TextObject Name;
        public readonly Action<object> Action;
        public readonly Func<Tuple<bool, TaleWorlds.Localization.TextObject>> Disabled;
        public EscapeMenuItemVM(TaleWorlds.Localization.TextObject name, Action<object> action, object parameter,
            Func<Tuple<bool, TaleWorlds.Localization.TextObject>> disabled, bool isDefault)
        { Name = name; Action = action; Disabled = disabled; }
    }
}
namespace TaleWorlds.MountAndBlade.GauntletUI.Mission
{
    public class MissionGauntletEscapeMenuBase
    {
        public TaleWorlds.MountAndBlade.Mission Mission;
        public int CloseCalls;
        public bool PauseActive = true;
        protected bool OnEscapeMenuToggled(bool isOpened)
        {
            CloseCalls++;
            PauseActive = isOpened;
            Mission.Events.Add(isOpened ? "open" : "close");
            return true;
        }
    }
}
namespace TaleWorlds.MountAndBlade.GauntletUI.Mission.Singleplayer
{
    using TaleWorlds.Localization;
    using TaleWorlds.MountAndBlade.ViewModelCollection.EscapeMenu;
    public class MissionGauntletSingleplayerEscapeMenu : MissionGauntletEscapeMenuBase
    {
        public readonly EscapeMenuItemVM Resume = new EscapeMenuItemVM(new TextObject("Resume"), _ => { }, null, null, true);
        public readonly EscapeMenuItemVM MainMenu = new EscapeMenuItemVM(new TextObject("Main menu"), _ => { }, null, null, false);
        [MethodImpl(MethodImplOptions.NoInlining)]
        protected virtual List<EscapeMenuItemVM> GetEscapeMenuItems()
        { return new List<EscapeMenuItemVM> { Resume, MainMenu }; }
        public List<EscapeMenuItemVM> BuildItems() { return GetEscapeMenuItems(); }
    }
}
