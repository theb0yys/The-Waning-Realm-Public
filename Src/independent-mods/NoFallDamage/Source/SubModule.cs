using System.Runtime.CompilerServices;
using HarmonyLib;
using TaleWorlds.MountAndBlade;

[assembly: InternalsVisibleTo("NoFallDamage.Tests")]

namespace NoFallDamage
{
    public sealed class SubModule : MBSubModuleBase
    {
        private const string PatchId = "NoFallDamage.v1";
        private Harmony harmony;

        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();
            harmony = new Harmony(PatchId);
            try { harmony.PatchAll(typeof(SubModule).Assembly); }
            catch
            {
                harmony.UnpatchAll(PatchId);
                throw;
            }
        }

        protected override void OnSubModuleUnloaded()
        {
            harmony?.UnpatchAll(PatchId);
            base.OnSubModuleUnloaded();
        }
    }
}
