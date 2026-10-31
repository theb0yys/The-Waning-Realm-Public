using HarmonyLib;
using TaleWorlds.MountAndBlade;

namespace TroopLimitExpander.Patches
{
    [HarmonyPatch(typeof(BannerlordConfig), "GetRealBattleSize")]
    public static class FieldBattleSizePatch
    {
        private static void Postfix(ref int __result)
        {
            if (!TroopLimitSettings.IsExpandedActiveBattleSizeEnabled)
                return;

            if (__result > 0 && __result < TroopLimitSettings.EffectiveFieldBattleTroopCap)
                __result = TroopLimitSettings.EffectiveFieldBattleTroopCap;
        }
    }

    [HarmonyPatch(typeof(BannerlordConfig), "GetRealBattleSizeForSallyOut")]
    public static class SallyOutBattleSizePatch
    {
        private static void Postfix(ref int __result)
        {
            if (!TroopLimitSettings.IsExpandedActiveBattleSizeEnabled)
                return;

            if (__result > 0 && __result < TroopLimitSettings.EffectiveSiegeBattleTroopCap)
                __result = TroopLimitSettings.EffectiveSiegeBattleTroopCap;
        }
    }

    [HarmonyPatch(typeof(BannerlordConfig), "GetReinforcementWaveCount")]
    public static class ReinforcementWaveCountPatch
    {
        private static void Postfix(ref int __result)
        {
            if (!TroopLimitSettings.IsExpandedActiveBattleSizeEnabled)
                return;

            if (__result < TroopLimitSettings.EffectiveLandReinforcementWaves)
                __result = TroopLimitSettings.EffectiveLandReinforcementWaves;
        }
    }

    [HarmonyPatch(typeof(BannerlordConfig), "get_MaxReinforcementWaveCount")]
    public static class MaxReinforcementWaveCountPatch
    {
        private static void Postfix(ref int __result)
        {
            if (!TroopLimitSettings.IsExpandedActiveBattleSizeEnabled)
                return;

            if (__result < TroopLimitSettings.EffectiveLandReinforcementWaves)
                __result = TroopLimitSettings.EffectiveLandReinforcementWaves;
        }
    }
}
