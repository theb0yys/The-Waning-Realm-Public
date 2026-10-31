using System;
using System.Reflection;
using HarmonyLib;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace Fourberie_CompatPatch
{
    public class SubModule : MBSubModuleBase
    {
        private Harmony _harmony;
        private const string HarmonyId = "fourberie.compatpatch.1.4.7";

        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();

            CompatLog.Info("SubModule loading...");
            CompatLog.Info("Food patch pre-load status: " + FourberieFoodSafetyPatch.GetDiagnosticStatus());

            try
            {
                _harmony = new Harmony(HarmonyId);
                _harmony.PatchAll(Assembly.GetExecutingAssembly());
                CompatLog.Info("Harmony.PatchAll() completed.");
                CompatLog.Info("Food patch registration status: " + FourberieFoodSafetyPatch.GetDiagnosticStatus());
                InformationManager.DisplayMessage(new InformationMessage("[Fourberie_CompatPatch] Loaded."));
            }
            catch (Exception ex)
            {
                CompatLog.Info("Harmony.PatchAll() failed: " + ex);
            }
        }

        protected override void OnSubModuleUnloaded()
        {
            CompatLog.Info("SubModule unloading...");

            try
            {
                _harmony?.UnpatchAll(HarmonyId);
            }
            catch (Exception ex)
            {
                CompatLog.Info("UnpatchAll failed: " + ex);
            }

            base.OnSubModuleUnloaded();
        }

    }
}
