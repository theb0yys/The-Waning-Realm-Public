using HarmonyLib;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace XorberaxTFTAOMPackControl
{
    public sealed class SubModule : MBSubModuleBase
    {
        private Harmony _harmony;
        private readonly ManagedModuleHost _managedModules = new ManagedModuleHost();
        private bool _gateInstalled;
        private bool _startupConfigApplied;
        private bool _campaignPatchesInstalled;

        protected override void OnSubModuleLoad()
        {
            DependencyResolver.Install();
            base.OnSubModuleLoad();
            PackLog.Info("Loading Xorberax TFTAOM Experience.");

            _harmony = new Harmony("taom.xorberax.experience");
            PackLog.Info("Original Xorberax systems will load after early engine localization startup.");
        }

        protected override void OnBeforeInitialModuleScreenSetAsRoot()
        {
            base.OnBeforeInitialModuleScreenSetAsRoot();
            InstallGate();
            ApplyStartupConfig();
        }

        protected override void OnGameStart(Game game, IGameStarter gameStarterObject)
        {
            base.OnGameStart(game, gameStarterObject);
            InstallGate();
            ApplyStartupConfig();
            _managedModules.OnGameStart(PackSettings.Current, game, gameStarterObject);
            InstallCampaignModulePatches();
        }

        public override void OnMissionBehaviorInitialize(Mission mission)
        {
            base.OnMissionBehaviorInitialize(mission);
            InstallMissionModulePatches();
            _managedModules.OnMissionBehaviorInitialize(PackSettings.Current, mission);
        }

        protected override void OnApplicationTick(float dt)
        {
            base.OnApplicationTick(dt);
            _managedModules.OnApplicationTick(PackSettings.Current, dt);
        }

        private void InstallGate()
        {
            if (_gateInstalled)
                return;

            if (_harmony == null)
                _harmony = new Harmony("taom.xorberax.experience");

            ModuleGate.Install(_harmony);
            _gateInstalled = true;
            PackLog.Info("Installed Xorberax TFTAOM Experience gate.");
        }

        private void ApplyStartupConfig()
        {
            if (_startupConfigApplied)
                return;

            ConfigBridge.ApplyAll(PackSettings.Current);
            _startupConfigApplied = true;
            PackLog.Info("Applied Xorberax TFTAOM Experience config.");
        }

        private void InstallCampaignModulePatches()
        {
            if (_campaignPatchesInstalled)
                return;

            ModulePatchManager.InstallCampaign(PackSettings.Current);
            _campaignPatchesInstalled = true;
        }

        private void InstallMissionModulePatches()
        {
            ModulePatchManager.InstallMission(PackSettings.Current);
        }

        protected override void OnSubModuleUnloaded()
        {
            _managedModules.OnSubModuleUnloaded();
            DependencyResolver.Uninstall();
            ModuleGate.Uninstall();
            ModulePatchManager.Uninstall();
            _harmony?.UnpatchAll("taom.xorberax.experience");
            base.OnSubModuleUnloaded();
        }
    }
}
