using HarmonyLib;
using TaleWorlds.MountAndBlade;

namespace TroopLimitExpander.Patches
{
    [HarmonyPatch(typeof(BannerlordConfig), "GetRealBattleSizeForSiege")]
    public static class SiegeSpawnPatch
    {
        private static void Postfix(ref int __result)
        {
            if (!TroopLimitSettings.IsExpandedActiveBattleSizeEnabled)
                return;

            if (__result > 0 && __result < TroopLimitSettings.EffectiveSiegeBattleTroopCap)
                __result = TroopLimitSettings.EffectiveSiegeBattleTroopCap;
        }
    }
}
