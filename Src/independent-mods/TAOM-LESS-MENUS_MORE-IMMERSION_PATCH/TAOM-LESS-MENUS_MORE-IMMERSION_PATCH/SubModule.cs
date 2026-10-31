using HarmonyLib;
using TaleWorlds.MountAndBlade;

namespace LessMenusMoreImmersion
{
    public sealed class SubModule : MBSubModuleBase
    {
        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();
            var harmony = new Harmony("LessMenusMoreImmersion");
            harmony.PatchAll();
        }
    }
}