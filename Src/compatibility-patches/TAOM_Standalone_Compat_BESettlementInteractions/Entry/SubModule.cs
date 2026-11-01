using HarmonyLib;
using System.Reflection;
using TAOM.Compat.Shared;
using TaleWorlds.MountAndBlade;

namespace TAOM_Standalone_Compat_BESettlementInteractions
{
    public sealed class SubModule : MBSubModuleBase
    {
        private Harmony _harmony;

        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();
            CompatLog.Configure("be_settlement_interactions_compatpatch.log");
            CompatLog.Info("SubModule loading.");
            _harmony = new Harmony("taom.compat.be.settlementinteractions.1.4.5");
            CompatBootstrap.ApplyAnnotatedPatchClasses(_harmony, Assembly.GetExecutingAssembly());
            CompatLog.Info("[BESettlementInteractions_CompatPatch] Loaded.");
        }

        protected override void OnSubModuleUnloaded()
        {
            try { _harmony?.UnpatchAll("taom.compat.be.settlementinteractions.1.4.5"); }
            catch (System.Exception ex) { CompatLog.Info("Unload failed: " + ex); }
            base.OnSubModuleUnloaded();
        }
    }
}
