// ═══════════════════════════════════════════════════════════════════════════
//  LOTRAOM_FactionBuff  ·  PartyMoralePatch.cs  (v3)
//
//  Single postfix on DefaultPartyMoraleModel.GetEffectivePartyMorale.
//  Replaces FoodMoralePatch + PrisonerMoralePatch (v2) — one culture lookup,
//  one player guard, both bonuses applied in one pass.
//
//  Applied bonuses (player party only):
//    · Tiered food bonus     (Scarce × 0.4 / Stocked × 1.0 / Surplus × 1.5)
//    · Seasonal flat bonus   (+2 on the culture's peak season)
//    · Prisoner morale delta (alignment + count scaled by campaign age)
//      Evil  → High: +5 / Normal: +2 / Low: -5
//      Good  → Low: +3 / Normal: 0  / High: -3
//      Neutral → 0 always
//
//  Food tier results are cached for the current in-game day so that the
//  ItemRoster scan does not run on every morale recalculation (BL calls
//  GetEffectivePartyMorale on every UI hover, settlement entry, etc.).
// ═══════════════════════════════════════════════════════════════════════════
using HarmonyLib;
using LOTRAOM.FactionBuff.Data;
using LOTRAOM.FactionBuff.Evaluation;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace LOTRAOM.FactionBuff.Patches
{
    [HarmonyPatch(
        typeof(DefaultPartyMoraleModel),
        nameof(DefaultPartyMoraleModel.GetEffectivePartyMorale))]
    public static class PartyMoralePatch
    {
        // ── Food tier cache ───────────────────────────────────────────────────
        // Keyed on the day the result was computed. Invalidated when the day
        // decreases (new session / new campaign) to prevent stale cross-session data.
        private static double   _cachedDay  = -1.0;
        private static FoodTier _cachedTier = FoodTier.None;

        private static FoodTier GetCachedFoodTier(MobileParty party, string itemId)
        {
            double today = Campaign.Current != null ? CampaignTime.Now.ToDays : -1.0;
            // Invalidate on day decrease (new game / load into earlier save) or day change.
            if (today < _cachedDay || today >= _cachedDay + 1.0)
            {
                _cachedTier = DailyStateEvaluator.GetFoodTier(party, itemId);
                _cachedDay  = today;
            }
            return _cachedTier;
        }

        // ── Postfix ───────────────────────────────────────────────────────────
        public static void Postfix(MobileParty mobileParty, ref ExplainedNumber __result)
        {
            // Player party only — AI parties of matching cultures must not receive
            // this bonus or the faction-wide simulation will be unbalanced.
            if (mobileParty == null || !mobileParty.IsMainParty) return;
            if (!FactionBuffSettings.EffectsEnabled) return;

            CultureObject? culture = mobileParty.Party?.Culture;
            if (culture == null) return;

            if (!FactionData.Profiles.TryGetValue(
                culture.StringId, out CultureProfile profile)) return;

            // ── Food bonus ────────────────────────────────────────────────────
            FoodTier tier = GetCachedFoodTier(mobileParty, profile.Food.ItemId);
            if (tier != FoodTier.None)
            {
                float foodBonus = DailyStateEvaluator.GetFoodMoraleValue(tier, profile.Food.BaseBonus);

                string foodKey = tier == FoodTier.Scarce  ? FactionData.KEY_FOOD_SCARCE
                               : tier == FoodTier.Surplus ? FactionData.KEY_FOOD_SURPLUS
                               : FactionData.KEY_FOOD_STOCKED;

                TextObject foodLabel = GameTexts.FindText(foodKey)
                                       ?? new TextObject("Faction Sustenance");
                __result.Add(foodBonus * FactionBuffSettings.MoraleMultiplier, foodLabel);

                // ── Seasonal flat bonus (own tooltip line) ────────────────────
                Season current = DailyStateEvaluator.GetCurrentSeason();
                if (DailyStateEvaluator.IsSeasonalPeak(current, profile.Food.PeakSeason))
                {
                    TextObject seasonLabel = GameTexts.FindText(FactionData.KEY_SEASONAL)
                                             ?? new TextObject("Peak Season");
                    __result.Add(FactionData.SEASONAL_BONUS * FactionBuffSettings.MoraleMultiplier, seasonLabel);
                }
            }

            // ── Prisoner bonus ────────────────────────────────────────────────
            if (profile.Alignment == Alignment.Neutral) return;

            PrisonerState priState = DailyStateEvaluator.GetPrisonerState(
                mobileParty.PrisonRoster.TotalManCount,
                mobileParty.MemberRoster.TotalManCount,
                DailyStateEvaluator.GetElapsedDays());

            float delta = FactionData.GetPrisonerMoraleDelta(
                profile.Alignment, priState, out string? tooltipKey);

            if (delta == 0f || tooltipKey == null) return;

            TextObject priLabel = GameTexts.FindText(tooltipKey)
                                  ?? new TextObject("Prisoner Morale");
            __result.Add(delta * FactionBuffSettings.MoraleMultiplier, priLabel);
        }
    }
}
