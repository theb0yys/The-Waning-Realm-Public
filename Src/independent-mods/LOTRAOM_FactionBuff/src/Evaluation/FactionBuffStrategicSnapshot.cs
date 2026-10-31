using System;
using LOTRAOM.FactionBuff.Data;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;

namespace LOTRAOM.FactionBuff.Evaluation
{
    public sealed class FactionBuffStrategicSnapshot
    {
        public string CultureId { get; }
        public string CultureName { get; }
        public string StatusLabel { get; }
        public string DoctrineLabel { get; }
        public string ReadinessBand { get; }
        public string DoctrineDirective { get; }
        public int StrategicScore { get; }
        public float EstimatedMoraleDelta { get; }
        public float MoraleMultiplier { get; }
        public bool EffectsEnabled { get; }
        public string AdviceLine { get; }
        public string SupplyLine { get; }
        public string PrisonerLine { get; }
        public string MomentumLine { get; }
        public FoodTier Food { get; }
        public PrisonerState Prisoner { get; }
        public Season Season { get; }
        public TerrainContext Terrain { get; }
        public PoliticalStatus Political { get; }
        public RenownTier Renown { get; }
        public bool IsPeak { get; }
        public bool IsDire { get; }

        internal FactionBuffStrategicSnapshot(
            string cultureId,
            string cultureName,
            string statusLabel,
            string doctrineLabel,
            string readinessBand,
            string doctrineDirective,
            int strategicScore,
            float estimatedMoraleDelta,
            float moraleMultiplier,
            bool effectsEnabled,
            string adviceLine,
            string supplyLine,
            string prisonerLine,
            string momentumLine,
            FoodTier food,
            PrisonerState prisoner,
            Season season,
            TerrainContext terrain,
            PoliticalStatus political,
            RenownTier renown,
            bool isPeak,
            bool isDire)
        {
            CultureId = cultureId ?? string.Empty;
            CultureName = cultureName ?? cultureId ?? "Unknown";
            StatusLabel = statusLabel ?? "steady";
            DoctrineLabel = doctrineLabel ?? "Balanced";
            ReadinessBand = readinessBand ?? "watch";
            DoctrineDirective = doctrineDirective ?? "Maintain doctrine";
            StrategicScore = strategicScore;
            EstimatedMoraleDelta = estimatedMoraleDelta;
            MoraleMultiplier = moraleMultiplier;
            EffectsEnabled = effectsEnabled;
            AdviceLine = adviceLine ?? "hold course";
            SupplyLine = supplyLine ?? "supply unknown";
            PrisonerLine = prisonerLine ?? "prisoners unknown";
            MomentumLine = momentumLine ?? "momentum steady";
            Food = food;
            Prisoner = prisoner;
            Season = season;
            Terrain = terrain;
            Political = political;
            Renown = renown;
            IsPeak = isPeak;
            IsDire = isDire;
        }

        public string BuildCompactLine()
        {
            string moraleSign = EstimatedMoraleDelta >= 0f ? "+" : string.Empty;
            return CultureName + " | " + DoctrineLabel + " doctrine | " +
                   StatusLabel + " " + StrategicScore + "/100" +
                   " | " + ReadinessBand +
                   " | morale " + moraleSign + EstimatedMoraleDelta.ToString("0.0") +
                   " x" + MoraleMultiplier.ToString("0.00") +
                   " | " + SupplyLine +
                   " | " + PrisonerLine +
                   " | " + DoctrineDirective;
        }
    }

    public static class FactionBuffPublisher
    {
        private static FactionBuffStrategicSnapshot? _lastSnapshot;

        public static bool HasSnapshot => _lastSnapshot != null;
        public static FactionBuffStrategicSnapshot? LastSnapshot => _lastSnapshot;
        public static string StatusLine => _lastSnapshot != null ? _lastSnapshot.BuildCompactLine() : "FactionBuff awaiting player party state";

        internal static void Update(FactionBuffStrategicSnapshot snapshot)
        {
            if (snapshot != null)
                _lastSnapshot = snapshot;
        }

        internal static void Clear()
        {
            _lastSnapshot = null;
        }
    }

    public static class FactionBuffStrategicScorer
    {
        public static FactionBuffStrategicSnapshot Create(
            CultureProfile profile,
            PartyBuffState state,
            MobileParty? party,
            int peakStreak,
            int direStreak)
        {
            string cultureName = party?.Party?.Culture?.Name?.ToString() ?? profile.CultureId;
            int memberCount = Math.Max(1, party?.MemberRoster.TotalManCount ?? 1);
            int prisonerCount = Math.Max(0, party?.PrisonRoster.TotalManCount ?? 0);

            float morale = EstimateMoraleDelta(profile, state);
            int score = Score(profile, state, morale);

            string status = BuildStatusLabel(score, state);
            string readiness = BuildReadinessBand(score, state);
            string supply = BuildSupplyLine(profile, party, memberCount);
            string prisoners = BuildPrisonerLine(prisonerCount, memberCount, profile.Alignment, state.Prisoner);
            string momentum = BuildMomentumLine(state, peakStreak, direStreak);
            string advice = BuildAdviceLine(profile, state);
            string directive = BuildDoctrineDirective(profile, state, score, morale);

            return new FactionBuffStrategicSnapshot(
                profile.CultureId,
                cultureName,
                status,
                FactionBuffSettings.ModeLabel,
                readiness,
                directive,
                score,
                morale,
                FactionBuffSettings.MoraleMultiplier,
                FactionBuffSettings.EffectsEnabled,
                advice,
                supply,
                prisoners,
                momentum,
                state.Food,
                state.Prisoner,
                state.CurrentSeason,
                state.Terrain,
                state.Political,
                state.Renown,
                state.IsCombinedPeak,
                state.IsCombinedDire);
        }

        private static float EstimateMoraleDelta(CultureProfile profile, PartyBuffState state)
        {
            float morale = DailyStateEvaluator.GetFoodMoraleValue(state.Food, profile.Food.BaseBonus);
            if (DailyStateEvaluator.IsSeasonalPeak(state.CurrentSeason, profile.Food.PeakSeason))
                morale += FactionData.SEASONAL_BONUS;

            string? tooltip;
            morale += FactionData.GetPrisonerMoraleDelta(profile.Alignment, state.Prisoner, out tooltip);
            return morale * FactionBuffSettings.MoraleMultiplier;
        }

        private static int Score(CultureProfile profile, PartyBuffState state, float morale)
        {
            int score = 50 + (int)Math.Round(morale * 4f);

            switch (state.Food)
            {
                case FoodTier.Surplus: score += 10; break;
                case FoodTier.Stocked: score += 4; break;
                case FoodTier.Scarce: score -= 14; break;
                case FoodTier.None: score -= 18; break;
            }

            if (DailyStateEvaluator.IsSeasonalPeak(state.CurrentSeason, profile.Food.PeakSeason))
                score += 6;
            if (profile.Terrain.IsHome(state.Terrain))
                score += 5;
            if (profile.Terrain.IsForeign(state.Terrain))
                score -= 5;
            if (state.HeroSynergy)
                score += 6;
            if (state.HeroConflict)
                score -= 10;
            if (state.ArmyAllyCultureIds != null)
                score += Math.Min(9, state.ArmyAllyCultureIds.Length * 3);
            if (state.IsPlayerWounded)
                score -= 8;
            if (state.Political == PoliticalStatus.Ruler)
                score += 4;
            else if (state.Political == PoliticalStatus.Vassal)
                score += 2;
            score += Math.Min(6, (int)state.Renown * 2);
            if (state.IsCombinedPeak)
                score += 8;
            if (state.IsCombinedDire)
                score -= 14;

            if (score < 0) return 0;
            if (score > 100) return 100;
            return score;
        }

        private static string BuildStatusLabel(int score, PartyBuffState state)
        {
            if (state.IsCombinedPeak) return "ascendant";
            if (state.IsCombinedDire) return "dire";
            if (score >= 80) return "favored";
            if (score >= 65) return "strong";
            if (score >= 50) return "steady";
            if (score >= 35) return "strained";
            return "faltering";
        }

        private static string BuildReadinessBand(int score, PartyBuffState state)
        {
            if (!FactionBuffSettings.EffectsEnabled) return "doctrine effects suspended";
            if (state.IsCombinedPeak) return "doctrine peak";
            if (state.IsCombinedDire) return "doctrine crisis";
            if (score >= 80) return "war-ready";
            if (score >= 65) return "ready";
            if (score >= 50) return "watch";
            if (score >= 35) return "unstable";
            return "critical";
        }

        private static string BuildSupplyLine(CultureProfile profile, MobileParty? party, int memberCount)
        {
            try
            {
                ItemObject? food = Game.Current?.ObjectManager?.GetObject<ItemObject>(profile.Food.ItemId);
                int count = food != null && party != null ? party.ItemRoster.GetItemNumber(food) : 0;
                float ratio = memberCount > 0 ? (float)count / memberCount : 0f;
                return profile.Food.ItemId + " " + count + "/" + memberCount + " (" + ratio.ToString("0.00") + ")";
            }
            catch
            {
                return profile.Food.ItemId + " supply unknown";
            }
        }

        private static string BuildPrisonerLine(int prisoners, int members, Alignment alignment, PrisonerState state)
        {
            string quality;
            if (alignment == Alignment.Evil)
                quality = state == PrisonerState.High ? "useful" : state == PrisonerState.Low ? "thin" : "serviceable";
            else if (alignment == Alignment.Good)
                quality = state == PrisonerState.High ? "burdensome" : state == PrisonerState.Low ? "merciful" : "contained";
            else
                quality = "neutral";

            return "prisoners " + prisoners + "/" + Math.Max(1, members) + " " + quality;
        }

        private static string BuildMomentumLine(PartyBuffState state, int peakStreak, int direStreak)
        {
            if (state.IsCombinedPeak)
                return "peak streak " + Math.Max(1, peakStreak);
            if (state.IsCombinedDire)
                return "dire streak " + Math.Max(1, direStreak);
            return "momentum steady";
        }

        private static string BuildAdviceLine(CultureProfile profile, PartyBuffState state)
        {
            if (state.Food == FoodTier.None || state.Food == FoodTier.Scarce)
                return "secure " + profile.Food.ItemId;
            if (IsBadPrisonerState(profile.Alignment, state.Prisoner))
                return profile.Alignment == Alignment.Evil ? "take captives" : "reduce prisoner burden";
            if (state.IsPlayerWounded)
                return "rest the leader";
            if (state.HeroConflict)
                return "resolve command tension";
            if (profile.Terrain.IsForeign(state.Terrain))
                return "seek familiar ground";
            if (!DailyStateEvaluator.IsSeasonalPeak(state.CurrentSeason, profile.Food.PeakSeason))
                return "prepare for " + profile.Food.PeakSeason;
            if (state.IsCombinedPeak)
                return "press the advantage";
            return "hold course";
        }

        private static string BuildDoctrineDirective(CultureProfile profile, PartyBuffState state, int score, float morale)
        {
            if (!FactionBuffSettings.EffectsEnabled)
                return "Doctrine effects are suspended";
            if (state.IsCombinedDire || score < 35)
                return "Stabilize doctrine immediately";
            if (state.Food == FoodTier.None || state.Food == FoodTier.Scarce)
                return "Secure " + profile.Food.ItemId + " before morale buckles";
            if (IsBadPrisonerState(profile.Alignment, state.Prisoner))
                return profile.Alignment == Alignment.Evil
                    ? "Take captives to feed the war machine"
                    : "Reduce prisoner burden before mercy becomes strain";
            if (state.IsPlayerWounded)
                return "Rest the commander before the host loses nerve";
            if (state.HeroConflict)
                return "Resolve command culture tension";
            if (profile.Terrain.IsForeign(state.Terrain))
                return "Shift doctrine toward familiar ground";
            if (state.IsCombinedPeak || score >= 80 || morale >= 8f)
                return "Exploit doctrine peak while morale is high";
            if (score >= 65)
                return "Hold formation and build momentum";
            return "Maintain doctrine and watch supply";
        }

        private static bool IsBadPrisonerState(Alignment alignment, PrisonerState state)
        {
            return (alignment == Alignment.Evil && state == PrisonerState.Low)
                || (alignment == Alignment.Good && state == PrisonerState.High);
        }
    }
}
