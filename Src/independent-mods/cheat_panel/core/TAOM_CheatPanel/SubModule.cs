using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using HarmonyLib;

namespace TAOM_CheatPanel
{
    public sealed class SubModule : MBSubModuleBase
    {
        private Harmony _harmony;

        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();
            _ = Settings.CheatPanelSettings.Instance; // Load XML before any XP callbacks.
            _ = UI.CheatPanelHotkeyOptions.Resolve(Settings.CheatPanelSettings.Instance.OpenPanelKey);
            _harmony = new Harmony("taom.cheatpanel.progression");
            _harmony.CreateClassProcessor(typeof(Runtime.PlayerXpPatch)).Patch();
        }

        protected override void OnBeforeInitialModuleScreenSetAsRoot()
        {
            base.OnBeforeInitialModuleScreenSetAsRoot();
            UI.CheatPanelMcm.Register();
        }

        protected override void OnSubModuleUnloaded()
        {
            UI.CheatPanelMcm.Unregister();
            _harmony?.UnpatchAll("taom.cheatpanel.progression");
            base.OnSubModuleUnloaded();
        }

        protected override void OnGameStart(Game game, IGameStarter starter)
        {
            base.OnGameStart(game, starter);
            var campaignStarter = starter as CampaignGameStarter;
            if (campaignStarter == null) return;
            campaignStarter.AddBehavior(new UI.CheatPanelMenuBehavior());
            campaignStarter.AddBehavior(new Runtime.CheatAdjustmentBehavior());
            campaignStarter.AddBehavior(new Runtime.ProgressionCheatBehavior());
        }

        protected override void OnApplicationTick(float dt)
        {
            base.OnApplicationTick(dt);
            UI.CheatPanelHotkey.Tick();
        }
    }
}
