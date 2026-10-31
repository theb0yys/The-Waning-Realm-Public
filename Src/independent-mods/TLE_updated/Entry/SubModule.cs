using HarmonyLib;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TroopLimitExpander.UI;

namespace TroopLimitExpander
{
    public sealed class SubModule : MBSubModuleBase
    {
        public const string Version   = "2.2.13";
        public const string HarmonyId = "com.lotraom.waning-troop-limit-expander";

        private static readonly Color InfoColor  = Color.FromUint(0x00CC77FF);
        private static readonly Color ErrorColor = Color.FromUint(0xFF4500FF);

        private Harmony? _harmony;

        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();
        }

        protected override void OnBeforeInitialModuleScreenSetAsRoot()
        {
            base.OnBeforeInitialModuleScreenSetAsRoot();
            InitializeRuntimeAfterEarlyStartup();

            InformationManager.DisplayMessage(new InformationMessage(
                $"[Troop Limit Expander v{Version}] Active - " +
                TroopLimitSettings.BuildRuntimeSummary(),
                InfoColor));

            if (TroopLimitSettings.ShowRuntimeDiagnostics)
            {
                InformationManager.DisplayMessage(new InformationMessage(
                    "[Troop Limit Expander] " + TroopLimitSettings.BuildSafetyNote(),
                    InfoColor));
            }
        }

        private void InitializeRuntimeAfterEarlyStartup()
        {
            if (_harmony != null)
                return;

            try
            {
                _harmony = new Harmony(HarmonyId);
                _harmony.PatchAll(typeof(SubModule).Assembly);
            }
            catch (System.Exception ex)
            {
                InformationManager.DisplayMessage(new InformationMessage(
                    $"[TroopLimitExpander] Harmony patch FAILED: {ex.Message}", ErrorColor));
            }
        }

        public override void OnMissionBehaviorInitialize(Mission mission)
        {
            base.OnMissionBehaviorInitialize(mission);

            if (mission == null || GameNetwork.IsMultiplayer)
                return;

            try
            {
                mission.AddMissionBehavior(new TroopLimitBattleOverlayView());
            }
            catch (System.Exception ex)
            {
                InformationManager.DisplayMessage(new InformationMessage(
                    $"[TroopLimitExpander] Battle overlay disabled after init failure: {ex.Message}", ErrorColor));
            }
        }

        protected override void OnSubModuleUnloaded()
        {
            base.OnSubModuleUnloaded();
            try { _harmony?.UnpatchAll(HarmonyId); }
            catch { /* swallow — game is shutting down */ }
        }
    }
}
