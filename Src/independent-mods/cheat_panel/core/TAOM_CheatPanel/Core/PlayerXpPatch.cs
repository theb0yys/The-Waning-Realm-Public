using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TAOM_CheatPanel.Settings;

namespace TAOM_CheatPanel.Runtime
{
    [HarmonyPatch(typeof(HeroDeveloper), nameof(HeroDeveloper.AddSkillXp))]
    internal static class PlayerXpPatch
    {
        private static void Prefix(HeroDeveloper __instance, ref float rawXp, bool isAffectedByFocusFactor)
        {
            if (__instance.Hero == null || __instance.Hero != Hero.MainHero || !isAffectedByFocusFactor) return;
            rawXp = ProgressionRules.ScaleXp(rawXp, true, true, CheatPanelSettings.Instance);
        }
    }
}
