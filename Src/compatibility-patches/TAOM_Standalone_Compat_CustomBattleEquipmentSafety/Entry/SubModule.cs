using HarmonyLib;
using System.Reflection;
using TAOM.Compat.Shared;
using TaleWorlds.MountAndBlade;

namespace TAOM_Standalone_Compat_CustomBattleEquipmentSafety
{
    public sealed class SubModule : MBSubModuleBase
    {
        private Harmony _harmony;

        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();
            CompatLog.Configure("custom_battle_equipment_safety_compatpatch.log");
            CompatLog.Info("SubModule loading.");
            _harmony = new Harmony("taom.compat.custombattleequipmentsafety.1.4.5");
            CompatBootstrap.ApplyAnnotatedPatchClasses(_harmony, Assembly.GetExecutingAssembly());
            CompatLog.Info("[CustomBattleEquipmentSafety_CompatPatch] Loaded.");
        }

        protected override void OnSubModuleUnloaded()
        {
            try { _harmony?.UnpatchAll("taom.compat.custombattleequipmentsafety.1.4.5"); }
            catch (System.Exception ex) { CompatLog.Info("Unload failed: " + ex); }
            base.OnSubModuleUnloaded();
        }
    }
}
