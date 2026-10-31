using HarmonyLib;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.CustomBattle;

namespace TroopLimitExpander.Patches
{
    [HarmonyPatch(typeof(ArmyCompositionGroupVM), "get_MaxArmySize")]
    public static class ArmySizeMaxPatch
    {
        private static void Postfix(ref int __result)
        {
            if (__result < TroopLimitSettings.EffectiveCustomBattleTroopsPerSideCap)
                __result = TroopLimitSettings.EffectiveCustomBattleTroopsPerSideCap;
        }
    }

    [HarmonyPatch(typeof(BannerlordConfig), "get_MaxBattleSize")]
    public static class BannerlordOptionsMaxBattleSizePatch
    {
        private static void Postfix(ref int __result)
        {
            if (!TroopLimitSettings.IsExpandedActiveBattleSizeEnabled)
                return;

            if (__result < TroopLimitSettings.EffectiveMissionBattleSizeHardCap)
                __result = TroopLimitSettings.EffectiveMissionBattleSizeHardCap;
        }
    }

    [HarmonyPatch(typeof(DefaultBattleMissionAgentSpawnLogic), "get_MaxNumberOfTroopsForMission")]
    public static class MissionHardCapPatch
    {
        private static void Postfix(ref int __result)
        {
            if (!TroopLimitSettings.IsExpandedActiveBattleSizeEnabled)
                return;

            if (__result < TroopLimitSettings.EffectiveMissionBattleSizeHardCap)
                __result = TroopLimitSettings.EffectiveMissionBattleSizeHardCap;
        }
    }

    [HarmonyPatch(typeof(MissionSpawnSettings), "CreateDefaultSpawnSettings")]
    public static class MissionSpawnSettingsPatch
    {
        private static void Postfix(ref MissionSpawnSettings __result)
        {
            if (!TroopLimitSettings.IsExpandedActiveBattleSizeEnabled)
                return;

            TroopLimitSettings.ApplyToSpawnSettings(ref __result);
        }
    }
}
