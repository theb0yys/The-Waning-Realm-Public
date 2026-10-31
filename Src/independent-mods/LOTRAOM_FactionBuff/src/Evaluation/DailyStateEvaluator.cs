// ═══════════════════════════════════════════════════════════════════════════
//  LOTRAOM_FactionBuff  ·  DailyStateEvaluator.cs  (v5)
//
//  v5 additions to PartyBuffState:
//    · Scale         — party size bracket (Skirmish / Warband / Host / Horde)
//    · Terrain       — map terrain context at current position
//    · HeroConflict  — Good hero leading Evil party (or vice versa)
//    · HeroSynergy   — hero culture exactly matches party culture (named lord)
//    · ArmyAllyCultureIds — up to 3 distinct allied culture IDs in current army
// ═══════════════════════════════════════════════════════════════════════════
using System;
using System.Collections.Generic;
using LOTRAOM.FactionBuff.Data;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace LOTRAOM.FactionBuff.Evaluation
{
    public readonly struct PartyBuffState
    {
        // ── Core state ────────────────────────────────────────────────────────
        public readonly FoodTier       Food;
        public readonly PrisonerState  Prisoner;
        public readonly Season         CurrentSeason;
        public readonly bool           IsCombinedPeak;
        public readonly bool           IsCombinedDire;
        // ── Extended state (v5) ───────────────────────────────────────────────
        public readonly PartyScale     Scale;
        public readonly TerrainContext Terrain;
        public readonly bool           HeroConflict;
        public readonly bool           HeroSynergy;
        public readonly string[]       ArmyAllyCultureIds;
        // ── Extended state (v6) ───────────────────────────────────────────────
        public readonly PoliticalStatus Political;
        public readonly RenownTier      Renown;
        public readonly bool            IsPlayerWounded;

        public PartyBuffState(
            FoodTier food, PrisonerState prisoner, Season season,
            bool isCombinedPeak, bool isCombinedDire,
            PartyScale scale, TerrainContext terrain,
            bool heroConflict, bool heroSynergy, string[] armyAllyCultureIds,
            PoliticalStatus political, RenownTier renown, bool isPlayerWounded)
        {
            Food               = food;
            Prisoner           = prisoner;
            CurrentSeason      = season;
            IsCombinedPeak     = isCombinedPeak;
            IsCombinedDire     = isCombinedDire;
            Scale              = scale;
            Terrain            = terrain;
            HeroConflict       = heroConflict;
            HeroSynergy        = heroSynergy;
            ArmyAllyCultureIds = armyAllyCultureIds;
            Political          = political;
            Renown             = renown;
            IsPlayerWounded    = isPlayerWounded;
        }
    }

    public static class DailyStateEvaluator
    {
        // ── Food tier ─────────────────────────────────────────────────────────

        public static FoodTier GetFoodTier(MobileParty party, string itemId)
        {
            if (Game.Current?.ObjectManager == null) return FoodTier.None;
            ItemObject food = Game.Current.ObjectManager.GetObject<ItemObject>(itemId);
            if (food == null) return FoodTier.None;
            int count   = party.ItemRoster.GetItemNumber(food);
            if (count <= 0) return FoodTier.None;
            int   members = Math.Max(1, party.MemberRoster.TotalManCount);
            float ratio   = (float)count / members;
            if (ratio < FactionData.SCARCE_RATIO)  return FoodTier.Scarce;
            if (ratio < FactionData.SURPLUS_RATIO) return FoodTier.Stocked;
            return FoodTier.Surplus;
        }

        public static float GetFoodMoraleValue(FoodTier tier, float baseBonus)
        {
            switch (tier)
            {
                case FoodTier.Scarce:  return baseBonus * FactionData.SCARCE_MULT;
                case FoodTier.Stocked: return baseBonus;
                case FoodTier.Surplus: return baseBonus * FactionData.SURPLUS_MULT;
                default:               return 0f;
            }
        }

        // ── Prisoner state ────────────────────────────────────────────────────

        public static PrisonerState GetPrisonerState(int prisoners, int members, float elapsedDays)
        {
            var (lowCeiling, highFloor) = FactionData.GetScaledPrisonerThresholds(elapsedDays);
            if (prisoners <= lowCeiling) return PrisonerState.Low;
            float highThreshold = Math.Max(highFloor, members * FactionData.HIGH_RATIO);
            if (prisoners >= highThreshold) return PrisonerState.High;
            return PrisonerState.Normal;
        }

        // ── Season ────────────────────────────────────────────────────────────

        public static Season GetCurrentSeason()
        {
            if (Campaign.Current == null) return Season.Spring;
            // Cast to long before modulo — double precision degrades at large values
            // (~year 30+), causing wrong season at the boundaries of each year.
            int dayOfYear = (int)((long)CampaignTime.Now.ToDays % FactionData.DAYS_PER_YEAR);
            int seasonIdx = dayOfYear / FactionData.DAYS_PER_SEASON;
            return (Season)Math.Min(seasonIdx, 3);
        }

        public static bool IsSeasonalPeak(Season current, Season culturePeak)
            => current == culturePeak;

        // ── Combined state ────────────────────────────────────────────────────

        public static (bool peak, bool dire) GetCombinedStates(
            Alignment alignment, FoodTier foodTier, PrisonerState prisonerState)
        {
            bool foodGood = foodTier == FoodTier.Surplus;
            bool foodBad  = foodTier == FoodTier.Scarce || foodTier == FoodTier.None;

            if (alignment == Alignment.Neutral)
                return (foodGood && prisonerState == PrisonerState.Low,
                        foodBad  && prisonerState == PrisonerState.High);

            bool prisonerBest =
                (alignment == Alignment.Evil && prisonerState == PrisonerState.High) ||
                (alignment == Alignment.Good && prisonerState == PrisonerState.Low);
            bool prisonerWorst =
                (alignment == Alignment.Evil && prisonerState == PrisonerState.Low) ||
                (alignment == Alignment.Good && prisonerState == PrisonerState.High);

            return (foodGood && prisonerBest, foodBad && prisonerWorst);
        }

        // ── Elapsed days ──────────────────────────────────────────────────────

        public static float GetElapsedDays()
            => Campaign.Current != null ? (float)CampaignTime.Now.ToDays : 0f;

        // ── Party scale ───────────────────────────────────────────────────────

        /// <summary>Classifies party size into a narrative bracket.</summary>
        public static PartyScale GetPartyScale(int memberCount)
        {
            if (memberCount <= FactionData.SCALE_SKIRMISH_MAX) return PartyScale.Skirmish;
            if (memberCount <= FactionData.SCALE_WARBAND_MAX)  return PartyScale.Warband;
            if (memberCount <= FactionData.SCALE_HOST_MAX)     return PartyScale.Host;
            return PartyScale.Horde;
        }

        // ── Terrain context ───────────────────────────────────────────────────

        /// <summary>
        /// Returns the terrain context at the party's current map position.
        /// Uses the current navigation face record exposed by the active campaign map scene.
        /// </summary>
        public static TerrainContext GetTerrainContext(MobileParty party)
        {
            try
            {
                if (Campaign.Current?.MapSceneWrapper == null) return TerrainContext.Default;
                PathFaceRecord currentFace = party.CurrentNavigationFace;
                if (!currentFace.IsValid()) return TerrainContext.Default;
                var terrainType = Campaign.Current.MapSceneWrapper
                                           .GetFaceTerrainType(currentFace);
                return MapTerrainType((int)terrainType);
            }
            catch { return TerrainContext.Default; }
        }

        private static TerrainContext MapTerrainType(int t)
        {
            // Bannerlord TerrainType values (stable across 1.2.x):
            // Plain=5, Mountain=6, Forest=9, Steppe=10, Desert=11, Snow=12, Valley=7
            switch (t)
            {
                case 5:  case 7:  return TerrainContext.Plain;
                case 6:           return TerrainContext.Mountain;
                case 9:  case 14: return TerrainContext.Forest;
                case 10:          return TerrainContext.Steppe;
                case 11:          return TerrainContext.Desert;
                case 12:          return TerrainContext.Snow;
                default:          return TerrainContext.Default;
            }
        }

        // ── Hero context ──────────────────────────────────────────────────────

        /// <summary>
        /// Returns (conflict, synergy) for the party's leader hero.
        ///
        /// Conflict = hero's culture alignment differs from party alignment
        ///            (e.g. Gondor hero leading Mordor party).
        /// Synergy  = hero IS a named lord whose culture exactly matches the
        ///            party culture — reinforces identity.
        ///
        /// Both require a valid LeaderHero with a known culture profile.
        /// </summary>
        public static (bool conflict, bool synergy) GetHeroContext(
            MobileParty party, CultureProfile partyProfile)
        {
            var hero = party.LeaderHero;
            if (hero?.Culture == null) return (false, false);

            // Synergy: a named companion/lord whose culture matches the party —
            // explicitly excludes the player character to avoid
            // firing every day for the most common case of "player leads their own faction".
            bool synergy = hero.IsLord && hero != Hero.MainHero &&
                           string.Equals(hero.Culture.StringId, partyProfile.CultureId,
                                         StringComparison.OrdinalIgnoreCase);

            // Conflict: hero's culture has a known alignment that differs from the party's
            bool conflict = false;
            if (!synergy &&
                FactionData.Profiles.TryGetValue(hero.Culture.StringId, out var heroCulture))
            {
                // Neutral cultures don't create conflict — only Good↔Evil crossing
                bool heroIsGood    = heroCulture.Alignment == Alignment.Good;
                bool heroIsEvil    = heroCulture.Alignment == Alignment.Evil;
                bool partyIsGood   = partyProfile.Alignment == Alignment.Good;
                bool partyIsEvil   = partyProfile.Alignment == Alignment.Evil;

                conflict = (heroIsGood && partyIsEvil) || (heroIsEvil && partyIsGood);
            }

            return (conflict, synergy);
        }

        // ── Army ally cultures ────────────────────────────────────────────────

        /// <summary>
        /// Returns up to 3 distinct culture IDs that are present as allied parties
        /// in the player's current army, excluding the player's own culture.
        /// Only cultures with known profiles are included.
        /// Returns empty array if not in an army or no distinct allies.
        /// </summary>
        public static string[] GetArmyAllyCultures(MobileParty party, string playerCultureId)
        {
            if (party.Army == null) return _emptyAllyArray;

            var found = new List<string>(4);
            foreach (var allied in party.Army.Parties)
            {
                if (allied == party) continue;
                string? cultId = allied.Party?.Culture?.StringId;
                if (cultId == null) continue;
                if (string.Equals(cultId, playerCultureId, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (!FactionData.Profiles.ContainsKey(cultId)) continue;
                if (found.Contains(cultId)) continue;
                found.Add(cultId);
                if (found.Count >= 3) break;
            }
            return found.Count > 0 ? found.ToArray() : _emptyAllyArray;
        }

        // ── Static empty array ────────────────────────────────────────────────
        // Returned by GetArmyAllyCultures when there are no results, avoiding
        // a heap allocation on every no-army tick.
        private static readonly string[] _emptyAllyArray = Array.Empty<string>();

        // ── Political status ──────────────────────────────────────────────────

        public static PoliticalStatus GetPoliticalStatus()
        {
            try
            {
                var hero = Hero.MainHero;
                if (hero?.Clan?.Kingdom == null) return PoliticalStatus.Independent;
                return hero.Clan.Kingdom.RulingClan == hero.Clan
                    ? PoliticalStatus.Ruler
                    : PoliticalStatus.Vassal;
            }
            catch { return PoliticalStatus.Independent; }
        }

        // ── Renown tier ───────────────────────────────────────────────────────

        public static RenownTier GetRenownTier()
        {
            try
            {
                float r = Hero.MainHero?.Clan?.Renown ?? 0f;
                if (r >= FactionExtrasData.RENOWN_LEGENDARY) return RenownTier.Legendary;
                if (r >= FactionExtrasData.RENOWN_RENOWNED)  return RenownTier.Renowned;
                if (r >= FactionExtrasData.RENOWN_NOTICED)   return RenownTier.Noticed;
                return RenownTier.Unknown;
            }
            catch { return RenownTier.Unknown; }
        }

        // ── Wound state ───────────────────────────────────────────────────────

        public static bool GetIsPlayerWounded(MobileParty party)
        {
            try   { return party.LeaderHero?.IsWounded == true; }
            catch { return false; }
        }

        // ── Full evaluation ───────────────────────────────────────────────────

        public static PartyBuffState Evaluate(MobileParty party, CultureProfile profile)
        {
            float elapsedDays = GetElapsedDays();
            int   members     = party.MemberRoster.TotalManCount;

            FoodTier      foodTier = GetFoodTier(party, profile.Food.ItemId);
            PrisonerState priState = GetPrisonerState(
                party.PrisonRoster.TotalManCount, members, elapsedDays);

            Season currentSeason = GetCurrentSeason();
            var (peak, dire)     = GetCombinedStates(profile.Alignment, foodTier, priState);

            PartyScale     scale   = GetPartyScale(members);
            TerrainContext terrain = GetTerrainContext(party);
            var (heroConflict, heroSynergy) = GetHeroContext(party, profile);
            string[] allyCultures = GetArmyAllyCultures(party, profile.CultureId);

            PoliticalStatus political = GetPoliticalStatus();
            RenownTier      renown    = GetRenownTier();
            bool            isWounded = GetIsPlayerWounded(party);

            return new PartyBuffState(
                foodTier, priState, currentSeason, peak, dire,
                scale, terrain, heroConflict, heroSynergy, allyCultures,
                political, renown, isWounded);
        }
    }
}
