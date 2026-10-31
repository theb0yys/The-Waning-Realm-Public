// ═══════════════════════════════════════════════════════════════════════════
//  LOTRAOM_FactionBuff  ·  FactionData.cs  (v5)
//
//  v5 additions:
//    · PartyScale enum  — Skirmish / Warband / Host / Horde (food scale suffix)
//    · TerrainContext enum — Default / Plain / Steppe / Mountain / Forest / Desert / Snow
//    · TerrainProfile class — home/foreign terrain arrays + message pools
//    · CultureProfile gains TerrainProfile field (optional, defaults to empty)
//    · SCALE_* + COLOUR_TERRAIN_* + COLOUR_HERO_* + COLOUR_ARMY_ALLY constants
//    · KEY_HERO_* constants for cross-alignment tension and named-lord synergy
// ═══════════════════════════════════════════════════════════════════════════
using System;
using System.Collections.Generic;

namespace LOTRAOM.FactionBuff.Data
{
    public enum Alignment     { Good, Neutral, Evil }
    public enum PrisonerState { Low, Normal, High }
    public enum FoodTier      { None, Scarce, Stocked, Surplus }
    public enum Season        { Spring = 0, Summer = 1, Autumn = 2, Winter = 3 }

    /// <summary>Rough size bracket for the player's party. Affects food message tone and scale suffix.</summary>
    public enum PartyScale    { Skirmish, Warband, Host, Horde }

    /// <summary>Terrain context at the party's current map position.</summary>
    public enum TerrainContext { Default, Plain, Steppe, Mountain, Forest, Desert, Snow }

    /// <summary>Player's current political standing within a kingdom.</summary>
    public enum PoliticalStatus { Independent, Vassal, Ruler }

    /// <summary>Player renown bracket — message fires once on crossing each boundary.</summary>
    public enum RenownTier { Unknown = 0, Noticed = 1, Renowned = 2, Legendary = 3 }

    // ── Data containers ───────────────────────────────────────────────────────

    public sealed class FoodProfile
    {
        public readonly string   ItemId;
        public readonly float    BaseBonus;
        public readonly Season   PeakSeason;
        public readonly string[] ScarceKeys;
        public readonly string[] StockedKeys;
        public readonly string[] SurplusKeys;

        public FoodProfile(string itemId, float baseBonus, Season peakSeason,
            string[] scarceKeys, string[] stockedKeys, string[] surplusKeys)
        {
            ItemId      = itemId;
            BaseBonus   = baseBonus;
            PeakSeason  = peakSeason;
            ScarceKeys  = scarceKeys;
            StockedKeys = stockedKeys;
            SurplusKeys = surplusKeys;
        }

        public string[] KeysForTier(FoodTier tier)
        {
            switch (tier)
            {
                case FoodTier.Scarce:  return ScarceKeys;
                case FoodTier.Surplus: return SurplusKeys;
                default:               return StockedKeys;
            }
        }
    }

    public sealed class PrisonerProfile
    {
        public readonly string[] HighKeys;
        public readonly string[] NormalKeys;
        public readonly string[] LowKeys;

        public PrisonerProfile(string[] highKeys, string[] normalKeys, string[] lowKeys)
        {
            HighKeys   = highKeys;
            NormalKeys = normalKeys;
            LowKeys    = lowKeys;
        }
    }

    public sealed class CombinedProfile
    {
        public readonly string[] PeakKeys;
        public readonly string[] DireKeys;

        public CombinedProfile(string[] peakKeys, string[] direKeys)
        {
            PeakKeys = peakKeys;
            DireKeys = direKeys;
        }
    }

    /// <summary>
    /// Per-culture messages for all four season transitions.
    /// GetKeys(season) returns the pool for the incoming season.
    /// Peak-season variants should mention the morale bonus in their text.
    /// </summary>
    public sealed class SeasonProfile
    {
        public readonly string[] SpringKeys;
        public readonly string[] SummerKeys;
        public readonly string[] AutumnKeys;
        public readonly string[] WinterKeys;

        public SeasonProfile(
            string[] springKeys, string[] summerKeys,
            string[] autumnKeys, string[] winterKeys)
        {
            SpringKeys = springKeys;
            SummerKeys = summerKeys;
            AutumnKeys = autumnKeys;
            WinterKeys = winterKeys;
        }

        public string[] GetKeys(Season season)
        {
            switch (season)
            {
                case Season.Spring: return SpringKeys;
                case Season.Summer: return SummerKeys;
                case Season.Autumn: return AutumnKeys;
                default:            return WinterKeys;
            }
        }
    }

    /// <summary>
    /// Escalated messages that fire when Peak or Dire state has held for
    /// FactionData.STREAK_THRESHOLD or more consecutive days. Intended to
    /// feel more intense than the normal combined message pools.
    /// </summary>
    public sealed class StreakProfile
    {
        public readonly string[] PeakKeys;
        public readonly string[] DireKeys;

        public StreakProfile(string[] peakKeys, string[] direKeys)
        {
            PeakKeys = peakKeys;
            DireKeys = direKeys;
        }
    }

    /// <summary>
    /// Defines which terrain contexts feel like home or foreign for this culture,
    /// and the message pools that fire when the player enters each.
    /// HomeTerrains → positive flavour. ForeignTerrains → fish-out-of-water flavour.
    /// Either array may be empty — no message fires if the current terrain isn't listed.
    /// </summary>
    public sealed class TerrainProfile
    {
        public readonly TerrainContext[] HomeTerrains;
        public readonly TerrainContext[] ForeignTerrains; // empty = no foreign messages
        public readonly string[]         HomeKeys;
        public readonly string[]?        ForeignKeys;     // null if no foreign messages

        public TerrainProfile(
            TerrainContext[] homeTerrains, string[] homeKeys,
            TerrainContext[]? foreignTerrains = null, string[]? foreignKeys = null)
        {
            HomeTerrains    = homeTerrains    ?? new TerrainContext[0];
            HomeKeys        = homeKeys        ?? new string[0];
            ForeignTerrains = foreignTerrains ?? new TerrainContext[0];
            ForeignKeys     = foreignKeys;
        }

        public bool IsHome(TerrainContext ctx)
        {
            foreach (var t in HomeTerrains) if (t == ctx) return true;
            return false;
        }

        public bool IsForeign(TerrainContext ctx)
        {
            foreach (var t in ForeignTerrains) if (t == ctx) return true;
            return false;
        }
    }

    public sealed class VictoryProfile
    {
        public readonly string[] Keys;
        public VictoryProfile(string[] keys) { Keys = keys; }
    }

    public sealed class PoliticalProfile
    {
        public readonly string[] RulerKeys;
        public readonly string[] VassalKeys;
        public readonly string[] IndependentKeys;
        public PoliticalProfile(string[] ruler, string[] vassal, string[] independent)
        { RulerKeys = ruler; VassalKeys = vassal; IndependentKeys = independent; }
        public string[] GetKeys(PoliticalStatus s)
        {
            switch (s)
            {
                case PoliticalStatus.Ruler:  return RulerKeys;
                case PoliticalStatus.Vassal: return VassalKeys;
                default:                     return IndependentKeys;
            }
        }
    }

    public sealed class RenownProfile
    {
        public readonly string[] UnknownKeys;
        public readonly string[] NoticedKeys;
        public readonly string[] RenownedKeys;
        public readonly string[] LegendaryKeys;
        public RenownProfile(string[] unknown, string[] noticed, string[] renowned, string[] legendary)
        { UnknownKeys = unknown; NoticedKeys = noticed; RenownedKeys = renowned; LegendaryKeys = legendary; }
        public string[] GetKeys(RenownTier t)
        {
            switch (t)
            {
                case RenownTier.Noticed:   return NoticedKeys;
                case RenownTier.Renowned:  return RenownedKeys;
                case RenownTier.Legendary: return LegendaryKeys;
                default:                   return UnknownKeys;
            }
        }
    }

    public sealed class WoundProfile
    {
        public readonly string[] Keys;
        public WoundProfile(string[] keys) { Keys = keys; }
    }

    /// <summary>
    /// Holds the four expansion profiles for a culture.
    /// Stored in FactionExtrasData.Extras — separate from CultureProfile so no existing
    /// constructors need modification. Behavior accesses via TryGetValue on the Extras dict.
    /// </summary>
    public sealed class FactionExtras
    {
        public readonly VictoryProfile  Victory;
        public readonly PoliticalProfile Political;
        public readonly RenownProfile   Renown;
        public readonly WoundProfile    Wound;
        public FactionExtras(VictoryProfile v, PoliticalProfile p, RenownProfile r, WoundProfile w)
        { Victory = v; Political = p; Renown = r; Wound = w; }
    }

    public sealed class CultureProfile
    {
        public readonly string          CultureId;
        public readonly Alignment       Alignment;
        public readonly FoodProfile     Food;
        public readonly PrisonerProfile Prisoner;
        public readonly CombinedProfile Combined;
        public readonly SeasonProfile   Season;
        public readonly StreakProfile   Streak;
        public readonly TerrainProfile  Terrain;

        public CultureProfile(
            string cultureId, Alignment alignment,
            FoodProfile food, PrisonerProfile prisoner,
            CombinedProfile combined, SeasonProfile season, StreakProfile streak,
            TerrainProfile? terrain = null)
        {
            CultureId = cultureId;
            Alignment = alignment;
            Food      = food;
            Prisoner  = prisoner;
            Combined  = combined;
            Season    = season;
            Streak    = streak;
            Terrain   = terrain ?? new TerrainProfile(new TerrainContext[0], new string[0]);
        }
    }

    // ── Constants ─────────────────────────────────────────────────────────────
    public static class FactionData
    {
        public const float SCARCE_RATIO   = 0.3f;
        public const float SURPLUS_RATIO  = 1.5f;
        public const float SCARCE_MULT    = 0.4f;
        public const float SURPLUS_MULT   = 1.5f;
        public const float SEASONAL_BONUS = 2f;
        public const float HIGH_RATIO     = 0.15f;

        public const float EVIL_HIGH_BONUS    =  5f;
        public const float EVIL_NORMAL_BONUS  =  2f;
        public const float EVIL_LOW_PENALTY   = -5f;
        public const float GOOD_LOW_BONUS     =  3f;
        public const float GOOD_HIGH_PENALTY  = -3f;

        // Consecutive Peak/Dire days before streak message fires
        public const int STREAK_THRESHOLD = 3;

        public const string KEY_FOOD_SCARCE   = "lotr_buff_label_food_scarce";
        public const string KEY_FOOD_STOCKED  = "lotr_buff_label_food_stocked";
        public const string KEY_FOOD_SURPLUS  = "lotr_buff_label_food_surplus";
        public const string KEY_SEASONAL      = "lotr_buff_label_seasonal";
        public const string KEY_PRI_BUFF      = "lotr_buff_label_prisoner_buff";
        public const string KEY_PRI_DEB       = "lotr_buff_label_prisoner_deb";
        public const string KEY_PRI_NORMAL    = "lotr_buff_label_prisoner_normal";
        public const string KEY_TIER_SCARCE   = "lotr_buff_tier_scarce";
        public const string KEY_TIER_SURPLUS  = "lotr_buff_tier_surplus";

        public const uint COLOUR_FOOD_GOOD     = 0xFFD4AF37u;
        public const uint COLOUR_FOOD_EVIL     = 0xFFCC2200u;
        public const uint COLOUR_FOOD_SCARCE   = 0xFFAA6600u;
        public const uint COLOUR_PRI_BUFF      = 0xFF44CC88u;
        public const uint COLOUR_PRI_DEB       = 0xFFCC6622u;
        public const uint COLOUR_PRI_NORMAL    = 0xFFAABBCCu;
        public const uint COLOUR_COMBINED_PEAK = 0xFFFFD700u;
        public const uint COLOUR_COMBINED_DIRE = 0xFF880000u;
        public const uint COLOUR_SEASON_TRANS  = 0xFF99CCBBu;  // all four season transitions
        public const uint COLOUR_STREAK_PEAK   = 0xFFFFEE44u;
        public const uint COLOUR_STREAK_DIRE   = 0xFF660000u;
        public const uint COLOUR_ARMY_ALLY     = 0xFF7799AAu;  // muted blue-grey: allied contingent
        public const uint COLOUR_TERRAIN_HOME  = 0xFF88BB77u;  // muted green: home ground
        public const uint COLOUR_TERRAIN_AWAY  = 0xFF997755u;  // muted amber: foreign terrain
        public const uint COLOUR_HERO_TENSION  = 0xFFBB7733u;  // orange-amber: alignment conflict
        public const uint COLOUR_HERO_SYNERGY  = 0xFF77AABBu; // cool blue: named hero present

        // Party size brackets (total member count)
        public const int SCALE_SKIRMISH_MAX = 49;
        public const int SCALE_WARBAND_MAX  = 199;
        public const int SCALE_HOST_MAX     = 499;

        // Hero message keys (generic — not per culture)
        public const string KEY_HERO_TENSION_GOOD_IN_EVIL = "lotr_hero_tension_good_in_evil";
        public const string KEY_HERO_TENSION_EVIL_IN_GOOD = "lotr_hero_tension_evil_in_good";
        public const string KEY_HERO_SYNERGY_NAMED        = "lotr_hero_synergy_named";

        public const int DAYS_PER_SEASON = 21;
        public const int DAYS_PER_YEAR   = 84;

        // ── Profiles ──────────────────────────────────────────────────────────
        //  Key format:
        //    Food:    lotr_food_scarce_{id}_{n}   lotr_food_stocked_{id}_{n}   lotr_food_surplus_{id}_{n}
        //    Season:  lotr_season_spring_{id}_{n} lotr_season_summer_{id}_{n}
        //             lotr_season_autumn_{id}_{n} lotr_season_winter_{id}_{n}
        //    Streak:  lotr_streak_peak_{id}_{n}   lotr_streak_dire_{id}_{n}
        //    Prison:  lotr_pri_high_{id}_{n}      lotr_pri_norm_{id}_{n}       lotr_pri_low_{id}_{n}
        //    Combined:lotr_peak_{id}_{n}          lotr_dire_{id}_{n}
        // ─────────────────────────────────────────────────────────────────────

        public static readonly Dictionary<string, CultureProfile> Profiles =
            new Dictionary<string, CultureProfile>(StringComparer.OrdinalIgnoreCase)
        {
            { "gondor", new CultureProfile("gondor", Alignment.Good,
                new FoodProfile("wine", 6f, Season.Autumn,
                    new[] { "lotr_food_scarce_gondor_0",  "lotr_food_scarce_gondor_1",  "lotr_food_scarce_gondor_2"  },
                    new[] { "lotr_food_stocked_gondor_0", "lotr_food_stocked_gondor_1", "lotr_food_stocked_gondor_2" },
                    new[] { "lotr_food_surplus_gondor_0", "lotr_food_surplus_gondor_1", "lotr_food_surplus_gondor_2" }),
                new PrisonerProfile(
                    new[] { "lotr_pri_high_gondor_0", "lotr_pri_high_gondor_1", "lotr_pri_high_gondor_2" },
                    new[] { "lotr_pri_norm_gondor_0", "lotr_pri_norm_gondor_1", "lotr_pri_norm_gondor_2" },
                    new[] { "lotr_pri_low_gondor_0",  "lotr_pri_low_gondor_1",  "lotr_pri_low_gondor_2"  }),
                new CombinedProfile(
                    new[] { "lotr_peak_gondor_0", "lotr_peak_gondor_1", "lotr_peak_gondor_2" },
                    new[] { "lotr_dire_gondor_0", "lotr_dire_gondor_1", "lotr_dire_gondor_2" }),
                new SeasonProfile(
                    new[] { "lotr_season_spring_gondor_0", "lotr_season_spring_gondor_1", "lotr_season_spring_gondor_2" },
                    new[] { "lotr_season_summer_gondor_0", "lotr_season_summer_gondor_1", "lotr_season_summer_gondor_2" },
                    new[] { "lotr_season_autumn_gondor_0", "lotr_season_autumn_gondor_1", "lotr_season_autumn_gondor_2" },
                    new[] { "lotr_season_winter_gondor_0", "lotr_season_winter_gondor_1", "lotr_season_winter_gondor_2" }),
                new StreakProfile(
                    new[] { "lotr_streak_peak_gondor_0", "lotr_streak_peak_gondor_1", "lotr_streak_peak_gondor_2" },
                    new[] { "lotr_streak_dire_gondor_0", "lotr_streak_dire_gondor_1", "lotr_streak_dire_gondor_2" }),
                new TerrainProfile(
                    new[] { TerrainContext.Plain },
                    new[] { "lotr_terrain_home_gondor_0", "lotr_terrain_home_gondor_1", "lotr_terrain_home_gondor_2" },
                    new[] { TerrainContext.Mountain, TerrainContext.Desert },
                    new[] { "lotr_terrain_foreign_gondor_0", "lotr_terrain_foreign_gondor_1", "lotr_terrain_foreign_gondor_2" })) },

            // ROHIRRIM PAIR — vlandia vanilla remap; rohan custom LOTRAOM ID.
            { "vlandia", new CultureProfile("vlandia", Alignment.Good,
                new FoodProfile("grain", 6f, Season.Autumn,
                    new[] { "lotr_food_scarce_vlandia_0",  "lotr_food_scarce_vlandia_1",  "lotr_food_scarce_vlandia_2"  },
                    new[] { "lotr_food_stocked_vlandia_0", "lotr_food_stocked_vlandia_1", "lotr_food_stocked_vlandia_2" },
                    new[] { "lotr_food_surplus_vlandia_0", "lotr_food_surplus_vlandia_1", "lotr_food_surplus_vlandia_2" }),
                new PrisonerProfile(
                    new[] { "lotr_pri_high_vlandia_0", "lotr_pri_high_vlandia_1", "lotr_pri_high_vlandia_2" },
                    new[] { "lotr_pri_norm_vlandia_0", "lotr_pri_norm_vlandia_1", "lotr_pri_norm_vlandia_2" },
                    new[] { "lotr_pri_low_vlandia_0",  "lotr_pri_low_vlandia_1",  "lotr_pri_low_vlandia_2"  }),
                new CombinedProfile(
                    new[] { "lotr_peak_vlandia_0", "lotr_peak_vlandia_1", "lotr_peak_vlandia_2" },
                    new[] { "lotr_dire_vlandia_0", "lotr_dire_vlandia_1", "lotr_dire_vlandia_2" }),
                new SeasonProfile(
                    new[] { "lotr_season_spring_vlandia_0", "lotr_season_spring_vlandia_1", "lotr_season_spring_vlandia_2" },
                    new[] { "lotr_season_summer_vlandia_0", "lotr_season_summer_vlandia_1", "lotr_season_summer_vlandia_2" },
                    new[] { "lotr_season_autumn_vlandia_0", "lotr_season_autumn_vlandia_1", "lotr_season_autumn_vlandia_2" },
                    new[] { "lotr_season_winter_vlandia_0", "lotr_season_winter_vlandia_1", "lotr_season_winter_vlandia_2" }),
                new StreakProfile(
                    new[] { "lotr_streak_peak_vlandia_0", "lotr_streak_peak_vlandia_1", "lotr_streak_peak_vlandia_2" },
                    new[] { "lotr_streak_dire_vlandia_0", "lotr_streak_dire_vlandia_1", "lotr_streak_dire_vlandia_2" }),
                new TerrainProfile(
                    new[] { TerrainContext.Plain, TerrainContext.Steppe },
                    new[] { "lotr_terrain_home_vlandia_0", "lotr_terrain_home_vlandia_1", "lotr_terrain_home_vlandia_2" })) },

            { "sturgia", new CultureProfile("sturgia", Alignment.Good,
                new FoodProfile("fish", 5f, Season.Spring,
                    new[] { "lotr_food_scarce_sturgia_0",  "lotr_food_scarce_sturgia_1",  "lotr_food_scarce_sturgia_2"  },
                    new[] { "lotr_food_stocked_sturgia_0", "lotr_food_stocked_sturgia_1", "lotr_food_stocked_sturgia_2" },
                    new[] { "lotr_food_surplus_sturgia_0", "lotr_food_surplus_sturgia_1", "lotr_food_surplus_sturgia_2" }),
                new PrisonerProfile(
                    new[] { "lotr_pri_high_sturgia_0", "lotr_pri_high_sturgia_1", "lotr_pri_high_sturgia_2" },
                    new[] { "lotr_pri_norm_sturgia_0", "lotr_pri_norm_sturgia_1", "lotr_pri_norm_sturgia_2" },
                    new[] { "lotr_pri_low_sturgia_0",  "lotr_pri_low_sturgia_1",  "lotr_pri_low_sturgia_2"  }),
                new CombinedProfile(
                    new[] { "lotr_peak_sturgia_0", "lotr_peak_sturgia_1", "lotr_peak_sturgia_2" },
                    new[] { "lotr_dire_sturgia_0", "lotr_dire_sturgia_1", "lotr_dire_sturgia_2" }),
                new SeasonProfile(
                    new[] { "lotr_season_spring_sturgia_0", "lotr_season_spring_sturgia_1", "lotr_season_spring_sturgia_2" },
                    new[] { "lotr_season_summer_sturgia_0", "lotr_season_summer_sturgia_1", "lotr_season_summer_sturgia_2" },
                    new[] { "lotr_season_autumn_sturgia_0", "lotr_season_autumn_sturgia_1", "lotr_season_autumn_sturgia_2" },
                    new[] { "lotr_season_winter_sturgia_0", "lotr_season_winter_sturgia_1", "lotr_season_winter_sturgia_2" }),
                new StreakProfile(
                    new[] { "lotr_streak_peak_sturgia_0", "lotr_streak_peak_sturgia_1", "lotr_streak_peak_sturgia_2" },
                    new[] { "lotr_streak_dire_sturgia_0", "lotr_streak_dire_sturgia_1", "lotr_streak_dire_sturgia_2" }),
                new TerrainProfile(
                    new[] { TerrainContext.Plain },
                    new[] { "lotr_terrain_home_sturgia_0", "lotr_terrain_home_sturgia_1", "lotr_terrain_home_sturgia_2" },
                    new[] { TerrainContext.Desert, TerrainContext.Mountain },
                    new[] { "lotr_terrain_foreign_sturgia_0", "lotr_terrain_foreign_sturgia_1", "lotr_terrain_foreign_sturgia_2" })) },

            { "rivendell", new CultureProfile("rivendell", Alignment.Good,
                new FoodProfile("lembas", 7f, Season.Spring,
                    new[] { "lotr_food_scarce_rivendell_0",  "lotr_food_scarce_rivendell_1",  "lotr_food_scarce_rivendell_2"  },
                    new[] { "lotr_food_stocked_rivendell_0", "lotr_food_stocked_rivendell_1", "lotr_food_stocked_rivendell_2" },
                    new[] { "lotr_food_surplus_rivendell_0", "lotr_food_surplus_rivendell_1", "lotr_food_surplus_rivendell_2" }),
                new PrisonerProfile(
                    new[] { "lotr_pri_high_rivendell_0", "lotr_pri_high_rivendell_1", "lotr_pri_high_rivendell_2" },
                    new[] { "lotr_pri_norm_rivendell_0", "lotr_pri_norm_rivendell_1", "lotr_pri_norm_rivendell_2" },
                    new[] { "lotr_pri_low_rivendell_0",  "lotr_pri_low_rivendell_1",  "lotr_pri_low_rivendell_2"  }),
                new CombinedProfile(
                    new[] { "lotr_peak_rivendell_0", "lotr_peak_rivendell_1", "lotr_peak_rivendell_2" },
                    new[] { "lotr_dire_rivendell_0", "lotr_dire_rivendell_1", "lotr_dire_rivendell_2" }),
                new SeasonProfile(
                    new[] { "lotr_season_spring_rivendell_0", "lotr_season_spring_rivendell_1", "lotr_season_spring_rivendell_2" },
                    new[] { "lotr_season_summer_rivendell_0", "lotr_season_summer_rivendell_1", "lotr_season_summer_rivendell_2" },
                    new[] { "lotr_season_autumn_rivendell_0", "lotr_season_autumn_rivendell_1", "lotr_season_autumn_rivendell_2" },
                    new[] { "lotr_season_winter_rivendell_0", "lotr_season_winter_rivendell_1", "lotr_season_winter_rivendell_2" }),
                new StreakProfile(
                    new[] { "lotr_streak_peak_rivendell_0", "lotr_streak_peak_rivendell_1", "lotr_streak_peak_rivendell_2" },
                    new[] { "lotr_streak_dire_rivendell_0", "lotr_streak_dire_rivendell_1", "lotr_streak_dire_rivendell_2" }),
                new TerrainProfile(
                    new[] { TerrainContext.Mountain, TerrainContext.Forest },
                    new[] { "lotr_terrain_home_rivendell_0", "lotr_terrain_home_rivendell_1", "lotr_terrain_home_rivendell_2" },
                    new[] { TerrainContext.Desert },
                    new[] { "lotr_terrain_foreign_rivendell_0", "lotr_terrain_foreign_rivendell_1", "lotr_terrain_foreign_rivendell_2" })) },

            { "mirkwood", new CultureProfile("mirkwood", Alignment.Good,
                new FoodProfile("grapes", 5f, Season.Summer,
                    new[] { "lotr_food_scarce_mirkwood_0",  "lotr_food_scarce_mirkwood_1",  "lotr_food_scarce_mirkwood_2"  },
                    new[] { "lotr_food_stocked_mirkwood_0", "lotr_food_stocked_mirkwood_1", "lotr_food_stocked_mirkwood_2" },
                    new[] { "lotr_food_surplus_mirkwood_0", "lotr_food_surplus_mirkwood_1", "lotr_food_surplus_mirkwood_2" }),
                new PrisonerProfile(
                    new[] { "lotr_pri_high_mirkwood_0", "lotr_pri_high_mirkwood_1", "lotr_pri_high_mirkwood_2" },
                    new[] { "lotr_pri_norm_mirkwood_0", "lotr_pri_norm_mirkwood_1", "lotr_pri_norm_mirkwood_2" },
                    new[] { "lotr_pri_low_mirkwood_0",  "lotr_pri_low_mirkwood_1",  "lotr_pri_low_mirkwood_2"  }),
                new CombinedProfile(
                    new[] { "lotr_peak_mirkwood_0", "lotr_peak_mirkwood_1", "lotr_peak_mirkwood_2" },
                    new[] { "lotr_dire_mirkwood_0", "lotr_dire_mirkwood_1", "lotr_dire_mirkwood_2" }),
                new SeasonProfile(
                    new[] { "lotr_season_spring_mirkwood_0", "lotr_season_spring_mirkwood_1", "lotr_season_spring_mirkwood_2" },
                    new[] { "lotr_season_summer_mirkwood_0", "lotr_season_summer_mirkwood_1", "lotr_season_summer_mirkwood_2" },
                    new[] { "lotr_season_autumn_mirkwood_0", "lotr_season_autumn_mirkwood_1", "lotr_season_autumn_mirkwood_2" },
                    new[] { "lotr_season_winter_mirkwood_0", "lotr_season_winter_mirkwood_1", "lotr_season_winter_mirkwood_2" }),
                new StreakProfile(
                    new[] { "lotr_streak_peak_mirkwood_0", "lotr_streak_peak_mirkwood_1", "lotr_streak_peak_mirkwood_2" },
                    new[] { "lotr_streak_dire_mirkwood_0", "lotr_streak_dire_mirkwood_1", "lotr_streak_dire_mirkwood_2" }),
                new TerrainProfile(
                    new[] { TerrainContext.Forest },
                    new[] { "lotr_terrain_home_mirkwood_0", "lotr_terrain_home_mirkwood_1", "lotr_terrain_home_mirkwood_2" },
                    new[] { TerrainContext.Desert, TerrainContext.Mountain },
                    new[] { "lotr_terrain_foreign_mirkwood_0", "lotr_terrain_foreign_mirkwood_1", "lotr_terrain_foreign_mirkwood_2" })) },

            { "lothlorien", new CultureProfile("lothlorien", Alignment.Good,
                new FoodProfile("lembas", 8f, Season.Spring,
                    new[] { "lotr_food_scarce_lothlorien_0",  "lotr_food_scarce_lothlorien_1",  "lotr_food_scarce_lothlorien_2"  },
                    new[] { "lotr_food_stocked_lothlorien_0", "lotr_food_stocked_lothlorien_1", "lotr_food_stocked_lothlorien_2" },
                    new[] { "lotr_food_surplus_lothlorien_0", "lotr_food_surplus_lothlorien_1", "lotr_food_surplus_lothlorien_2" }),
                new PrisonerProfile(
                    new[] { "lotr_pri_high_lothlorien_0", "lotr_pri_high_lothlorien_1", "lotr_pri_high_lothlorien_2" },
                    new[] { "lotr_pri_norm_lothlorien_0", "lotr_pri_norm_lothlorien_1", "lotr_pri_norm_lothlorien_2" },
                    new[] { "lotr_pri_low_lothlorien_0",  "lotr_pri_low_lothlorien_1",  "lotr_pri_low_lothlorien_2"  }),
                new CombinedProfile(
                    new[] { "lotr_peak_lothlorien_0", "lotr_peak_lothlorien_1", "lotr_peak_lothlorien_2" },
                    new[] { "lotr_dire_lothlorien_0", "lotr_dire_lothlorien_1", "lotr_dire_lothlorien_2" }),
                new SeasonProfile(
                    new[] { "lotr_season_spring_lothlorien_0", "lotr_season_spring_lothlorien_1", "lotr_season_spring_lothlorien_2" },
                    new[] { "lotr_season_summer_lothlorien_0", "lotr_season_summer_lothlorien_1", "lotr_season_summer_lothlorien_2" },
                    new[] { "lotr_season_autumn_lothlorien_0", "lotr_season_autumn_lothlorien_1", "lotr_season_autumn_lothlorien_2" },
                    new[] { "lotr_season_winter_lothlorien_0", "lotr_season_winter_lothlorien_1", "lotr_season_winter_lothlorien_2" }),
                new StreakProfile(
                    new[] { "lotr_streak_peak_lothlorien_0", "lotr_streak_peak_lothlorien_1", "lotr_streak_peak_lothlorien_2" },
                    new[] { "lotr_streak_dire_lothlorien_0", "lotr_streak_dire_lothlorien_1", "lotr_streak_dire_lothlorien_2" }),
                new TerrainProfile(
                    new[] { TerrainContext.Forest },
                    new[] { "lotr_terrain_home_lothlorien_0", "lotr_terrain_home_lothlorien_1", "lotr_terrain_home_lothlorien_2" },
                    new[] { TerrainContext.Desert },
                    new[] { "lotr_terrain_foreign_lothlorien_0", "lotr_terrain_foreign_lothlorien_1", "lotr_terrain_foreign_lothlorien_2" })) },

            // TODO: swap "grain" to "cram" if LOTRAOM registers that item ID.
            { "erebor", new CultureProfile("erebor", Alignment.Good,
                new FoodProfile("grain", 6f, Season.Winter,
                    new[] { "lotr_food_scarce_erebor_0",  "lotr_food_scarce_erebor_1",  "lotr_food_scarce_erebor_2"  },
                    new[] { "lotr_food_stocked_erebor_0", "lotr_food_stocked_erebor_1", "lotr_food_stocked_erebor_2" },
                    new[] { "lotr_food_surplus_erebor_0", "lotr_food_surplus_erebor_1", "lotr_food_surplus_erebor_2" }),
                new PrisonerProfile(
                    new[] { "lotr_pri_high_erebor_0", "lotr_pri_high_erebor_1", "lotr_pri_high_erebor_2" },
                    new[] { "lotr_pri_norm_erebor_0", "lotr_pri_norm_erebor_1", "lotr_pri_norm_erebor_2" },
                    new[] { "lotr_pri_low_erebor_0",  "lotr_pri_low_erebor_1",  "lotr_pri_low_erebor_2"  }),
                new CombinedProfile(
                    new[] { "lotr_peak_erebor_0", "lotr_peak_erebor_1", "lotr_peak_erebor_2" },
                    new[] { "lotr_dire_erebor_0", "lotr_dire_erebor_1", "lotr_dire_erebor_2" }),
                new SeasonProfile(
                    new[] { "lotr_season_spring_erebor_0", "lotr_season_spring_erebor_1", "lotr_season_spring_erebor_2" },
                    new[] { "lotr_season_summer_erebor_0", "lotr_season_summer_erebor_1", "lotr_season_summer_erebor_2" },
                    new[] { "lotr_season_autumn_erebor_0", "lotr_season_autumn_erebor_1", "lotr_season_autumn_erebor_2" },
                    new[] { "lotr_season_winter_erebor_0", "lotr_season_winter_erebor_1", "lotr_season_winter_erebor_2" }),
                new StreakProfile(
                    new[] { "lotr_streak_peak_erebor_0", "lotr_streak_peak_erebor_1", "lotr_streak_peak_erebor_2" },
                    new[] { "lotr_streak_dire_erebor_0", "lotr_streak_dire_erebor_1", "lotr_streak_dire_erebor_2" }),
                new TerrainProfile(
                    new[] { TerrainContext.Mountain },
                    new[] { "lotr_terrain_home_erebor_0", "lotr_terrain_home_erebor_1", "lotr_terrain_home_erebor_2" },
                    new[] { TerrainContext.Desert, TerrainContext.Forest },
                    new[] { "lotr_terrain_foreign_erebor_0", "lotr_terrain_foreign_erebor_1", "lotr_terrain_foreign_erebor_2" })) },

            // empire → Dunlendings. Confirm LOTRAOM culture XML mapping.
            { "empire", new CultureProfile("empire", Alignment.Neutral,
                new FoodProfile("vegetables", 4f, Season.Summer,
                    new[] { "lotr_food_scarce_empire_0",  "lotr_food_scarce_empire_1",  "lotr_food_scarce_empire_2"  },
                    new[] { "lotr_food_stocked_empire_0", "lotr_food_stocked_empire_1", "lotr_food_stocked_empire_2" },
                    new[] { "lotr_food_surplus_empire_0", "lotr_food_surplus_empire_1", "lotr_food_surplus_empire_2" }),
                new PrisonerProfile(
                    new[] { "lotr_pri_high_empire_0", "lotr_pri_high_empire_1", "lotr_pri_high_empire_2" },
                    new[] { "lotr_pri_norm_empire_0", "lotr_pri_norm_empire_1", "lotr_pri_norm_empire_2" },
                    new[] { "lotr_pri_low_empire_0",  "lotr_pri_low_empire_1",  "lotr_pri_low_empire_2"  }),
                new CombinedProfile(
                    new[] { "lotr_peak_empire_0", "lotr_peak_empire_1", "lotr_peak_empire_2" },
                    new[] { "lotr_dire_empire_0", "lotr_dire_empire_1", "lotr_dire_empire_2" }),
                new SeasonProfile(
                    new[] { "lotr_season_spring_empire_0", "lotr_season_spring_empire_1", "lotr_season_spring_empire_2" },
                    new[] { "lotr_season_summer_empire_0", "lotr_season_summer_empire_1", "lotr_season_summer_empire_2" },
                    new[] { "lotr_season_autumn_empire_0", "lotr_season_autumn_empire_1", "lotr_season_autumn_empire_2" },
                    new[] { "lotr_season_winter_empire_0", "lotr_season_winter_empire_1", "lotr_season_winter_empire_2" }),
                new StreakProfile(
                    new[] { "lotr_streak_peak_empire_0", "lotr_streak_peak_empire_1", "lotr_streak_peak_empire_2" },
                    new[] { "lotr_streak_dire_empire_0", "lotr_streak_dire_empire_1", "lotr_streak_dire_empire_2" }),
                new TerrainProfile(
                    new[] { TerrainContext.Mountain },
                    new[] { "lotr_terrain_home_empire_0", "lotr_terrain_home_empire_1", "lotr_terrain_home_empire_2" },
                    new[] { TerrainContext.Desert },
                    new[] { "lotr_terrain_foreign_empire_0", "lotr_terrain_foreign_empire_1", "lotr_terrain_foreign_empire_2" })) },

            { "mordor", new CultureProfile("mordor", Alignment.Evil,
                new FoodProfile("meat", 9f, Season.Winter,
                    new[] { "lotr_food_scarce_mordor_0",  "lotr_food_scarce_mordor_1",  "lotr_food_scarce_mordor_2"  },
                    new[] { "lotr_food_stocked_mordor_0", "lotr_food_stocked_mordor_1", "lotr_food_stocked_mordor_2" },
                    new[] { "lotr_food_surplus_mordor_0", "lotr_food_surplus_mordor_1", "lotr_food_surplus_mordor_2" }),
                new PrisonerProfile(
                    new[] { "lotr_pri_high_mordor_0", "lotr_pri_high_mordor_1", "lotr_pri_high_mordor_2" },
                    new[] { "lotr_pri_norm_mordor_0", "lotr_pri_norm_mordor_1", "lotr_pri_norm_mordor_2" },
                    new[] { "lotr_pri_low_mordor_0",  "lotr_pri_low_mordor_1",  "lotr_pri_low_mordor_2"  }),
                new CombinedProfile(
                    new[] { "lotr_peak_mordor_0", "lotr_peak_mordor_1", "lotr_peak_mordor_2" },
                    new[] { "lotr_dire_mordor_0", "lotr_dire_mordor_1", "lotr_dire_mordor_2" }),
                new SeasonProfile(
                    new[] { "lotr_season_spring_mordor_0", "lotr_season_spring_mordor_1", "lotr_season_spring_mordor_2" },
                    new[] { "lotr_season_summer_mordor_0", "lotr_season_summer_mordor_1", "lotr_season_summer_mordor_2" },
                    new[] { "lotr_season_autumn_mordor_0", "lotr_season_autumn_mordor_1", "lotr_season_autumn_mordor_2" },
                    new[] { "lotr_season_winter_mordor_0", "lotr_season_winter_mordor_1", "lotr_season_winter_mordor_2" }),
                new StreakProfile(
                    new[] { "lotr_streak_peak_mordor_0", "lotr_streak_peak_mordor_1", "lotr_streak_peak_mordor_2" },
                    new[] { "lotr_streak_dire_mordor_0", "lotr_streak_dire_mordor_1", "lotr_streak_dire_mordor_2" }),
                new TerrainProfile(
                    new[] { TerrainContext.Mountain },
                    new[] { "lotr_terrain_home_mordor_0", "lotr_terrain_home_mordor_1", "lotr_terrain_home_mordor_2" },
                    new[] { TerrainContext.Forest },
                    new[] { "lotr_terrain_foreign_mordor_0", "lotr_terrain_foreign_mordor_1", "lotr_terrain_foreign_mordor_2" })) },

            { "isengard", new CultureProfile("isengard", Alignment.Evil,
                new FoodProfile("meat", 9f, Season.Winter,
                    new[] { "lotr_food_scarce_isengard_0",  "lotr_food_scarce_isengard_1",  "lotr_food_scarce_isengard_2"  },
                    new[] { "lotr_food_stocked_isengard_0", "lotr_food_stocked_isengard_1", "lotr_food_stocked_isengard_2" },
                    new[] { "lotr_food_surplus_isengard_0", "lotr_food_surplus_isengard_1", "lotr_food_surplus_isengard_2" }),
                new PrisonerProfile(
                    new[] { "lotr_pri_high_isengard_0", "lotr_pri_high_isengard_1", "lotr_pri_high_isengard_2" },
                    new[] { "lotr_pri_norm_isengard_0", "lotr_pri_norm_isengard_1", "lotr_pri_norm_isengard_2" },
                    new[] { "lotr_pri_low_isengard_0",  "lotr_pri_low_isengard_1",  "lotr_pri_low_isengard_2"  }),
                new CombinedProfile(
                    new[] { "lotr_peak_isengard_0", "lotr_peak_isengard_1", "lotr_peak_isengard_2" },
                    new[] { "lotr_dire_isengard_0", "lotr_dire_isengard_1", "lotr_dire_isengard_2" }),
                new SeasonProfile(
                    new[] { "lotr_season_spring_isengard_0", "lotr_season_spring_isengard_1", "lotr_season_spring_isengard_2" },
                    new[] { "lotr_season_summer_isengard_0", "lotr_season_summer_isengard_1", "lotr_season_summer_isengard_2" },
                    new[] { "lotr_season_autumn_isengard_0", "lotr_season_autumn_isengard_1", "lotr_season_autumn_isengard_2" },
                    new[] { "lotr_season_winter_isengard_0", "lotr_season_winter_isengard_1", "lotr_season_winter_isengard_2" }),
                new StreakProfile(
                    new[] { "lotr_streak_peak_isengard_0", "lotr_streak_peak_isengard_1", "lotr_streak_peak_isengard_2" },
                    new[] { "lotr_streak_dire_isengard_0", "lotr_streak_dire_isengard_1", "lotr_streak_dire_isengard_2" }),
                new TerrainProfile(
                    new[] { TerrainContext.Mountain, TerrainContext.Forest },
                    new[] { "lotr_terrain_home_isengard_0", "lotr_terrain_home_isengard_1", "lotr_terrain_home_isengard_2" },
                    new[] { TerrainContext.Desert },
                    new[] { "lotr_terrain_foreign_isengard_0", "lotr_terrain_foreign_isengard_1", "lotr_terrain_foreign_isengard_2" })) },

            { "gundabad", new CultureProfile("gundabad", Alignment.Evil,
                new FoodProfile("meat", 8f, Season.Winter,
                    new[] { "lotr_food_scarce_gundabad_0",  "lotr_food_scarce_gundabad_1",  "lotr_food_scarce_gundabad_2"  },
                    new[] { "lotr_food_stocked_gundabad_0", "lotr_food_stocked_gundabad_1", "lotr_food_stocked_gundabad_2" },
                    new[] { "lotr_food_surplus_gundabad_0", "lotr_food_surplus_gundabad_1", "lotr_food_surplus_gundabad_2" }),
                new PrisonerProfile(
                    new[] { "lotr_pri_high_gundabad_0", "lotr_pri_high_gundabad_1", "lotr_pri_high_gundabad_2" },
                    new[] { "lotr_pri_norm_gundabad_0", "lotr_pri_norm_gundabad_1", "lotr_pri_norm_gundabad_2" },
                    new[] { "lotr_pri_low_gundabad_0",  "lotr_pri_low_gundabad_1",  "lotr_pri_low_gundabad_2"  }),
                new CombinedProfile(
                    new[] { "lotr_peak_gundabad_0", "lotr_peak_gundabad_1", "lotr_peak_gundabad_2" },
                    new[] { "lotr_dire_gundabad_0", "lotr_dire_gundabad_1", "lotr_dire_gundabad_2" }),
                new SeasonProfile(
                    new[] { "lotr_season_spring_gundabad_0", "lotr_season_spring_gundabad_1", "lotr_season_spring_gundabad_2" },
                    new[] { "lotr_season_summer_gundabad_0", "lotr_season_summer_gundabad_1", "lotr_season_summer_gundabad_2" },
                    new[] { "lotr_season_autumn_gundabad_0", "lotr_season_autumn_gundabad_1", "lotr_season_autumn_gundabad_2" },
                    new[] { "lotr_season_winter_gundabad_0", "lotr_season_winter_gundabad_1", "lotr_season_winter_gundabad_2" }),
                new StreakProfile(
                    new[] { "lotr_streak_peak_gundabad_0", "lotr_streak_peak_gundabad_1", "lotr_streak_peak_gundabad_2" },
                    new[] { "lotr_streak_dire_gundabad_0", "lotr_streak_dire_gundabad_1", "lotr_streak_dire_gundabad_2" }),
                new TerrainProfile(
                    new[] { TerrainContext.Mountain, TerrainContext.Snow },
                    new[] { "lotr_terrain_home_gundabad_0", "lotr_terrain_home_gundabad_1", "lotr_terrain_home_gundabad_2" },
                    new[] { TerrainContext.Desert },
                    new[] { "lotr_terrain_foreign_gundabad_0", "lotr_terrain_foreign_gundabad_1", "lotr_terrain_foreign_gundabad_2" })) },

            { "dolguldur", new CultureProfile("dolguldur", Alignment.Evil,
                new FoodProfile("meat", 8f, Season.Autumn,
                    new[] { "lotr_food_scarce_dolguldur_0",  "lotr_food_scarce_dolguldur_1",  "lotr_food_scarce_dolguldur_2"  },
                    new[] { "lotr_food_stocked_dolguldur_0", "lotr_food_stocked_dolguldur_1", "lotr_food_stocked_dolguldur_2" },
                    new[] { "lotr_food_surplus_dolguldur_0", "lotr_food_surplus_dolguldur_1", "lotr_food_surplus_dolguldur_2" }),
                new PrisonerProfile(
                    new[] { "lotr_pri_high_dolguldur_0", "lotr_pri_high_dolguldur_1", "lotr_pri_high_dolguldur_2" },
                    new[] { "lotr_pri_norm_dolguldur_0", "lotr_pri_norm_dolguldur_1", "lotr_pri_norm_dolguldur_2" },
                    new[] { "lotr_pri_low_dolguldur_0",  "lotr_pri_low_dolguldur_1",  "lotr_pri_low_dolguldur_2"  }),
                new CombinedProfile(
                    new[] { "lotr_peak_dolguldur_0", "lotr_peak_dolguldur_1", "lotr_peak_dolguldur_2" },
                    new[] { "lotr_dire_dolguldur_0", "lotr_dire_dolguldur_1", "lotr_dire_dolguldur_2" }),
                new SeasonProfile(
                    new[] { "lotr_season_spring_dolguldur_0", "lotr_season_spring_dolguldur_1", "lotr_season_spring_dolguldur_2" },
                    new[] { "lotr_season_summer_dolguldur_0", "lotr_season_summer_dolguldur_1", "lotr_season_summer_dolguldur_2" },
                    new[] { "lotr_season_autumn_dolguldur_0", "lotr_season_autumn_dolguldur_1", "lotr_season_autumn_dolguldur_2" },
                    new[] { "lotr_season_winter_dolguldur_0", "lotr_season_winter_dolguldur_1", "lotr_season_winter_dolguldur_2" }),
                new StreakProfile(
                    new[] { "lotr_streak_peak_dolguldur_0", "lotr_streak_peak_dolguldur_1", "lotr_streak_peak_dolguldur_2" },
                    new[] { "lotr_streak_dire_dolguldur_0", "lotr_streak_dire_dolguldur_1", "lotr_streak_dire_dolguldur_2" }),
                new TerrainProfile(
                    new[] { TerrainContext.Forest },
                    new[] { "lotr_terrain_home_dolguldur_0", "lotr_terrain_home_dolguldur_1", "lotr_terrain_home_dolguldur_2" },
                    new[] { TerrainContext.Desert, TerrainContext.Snow },
                    new[] { "lotr_terrain_foreign_dolguldur_0", "lotr_terrain_foreign_dolguldur_1", "lotr_terrain_foreign_dolguldur_2" })) },

            { "umbar", new CultureProfile("umbar", Alignment.Evil,
                new FoodProfile("olives", 5f, Season.Summer,
                    new[] { "lotr_food_scarce_umbar_0",  "lotr_food_scarce_umbar_1",  "lotr_food_scarce_umbar_2"  },
                    new[] { "lotr_food_stocked_umbar_0", "lotr_food_stocked_umbar_1", "lotr_food_stocked_umbar_2" },
                    new[] { "lotr_food_surplus_umbar_0", "lotr_food_surplus_umbar_1", "lotr_food_surplus_umbar_2" }),
                new PrisonerProfile(
                    new[] { "lotr_pri_high_umbar_0", "lotr_pri_high_umbar_1", "lotr_pri_high_umbar_2" },
                    new[] { "lotr_pri_norm_umbar_0", "lotr_pri_norm_umbar_1", "lotr_pri_norm_umbar_2" },
                    new[] { "lotr_pri_low_umbar_0",  "lotr_pri_low_umbar_1",  "lotr_pri_low_umbar_2"  }),
                new CombinedProfile(
                    new[] { "lotr_peak_umbar_0", "lotr_peak_umbar_1", "lotr_peak_umbar_2" },
                    new[] { "lotr_dire_umbar_0", "lotr_dire_umbar_1", "lotr_dire_umbar_2" }),
                new SeasonProfile(
                    new[] { "lotr_season_spring_umbar_0", "lotr_season_spring_umbar_1", "lotr_season_spring_umbar_2" },
                    new[] { "lotr_season_summer_umbar_0", "lotr_season_summer_umbar_1", "lotr_season_summer_umbar_2" },
                    new[] { "lotr_season_autumn_umbar_0", "lotr_season_autumn_umbar_1", "lotr_season_autumn_umbar_2" },
                    new[] { "lotr_season_winter_umbar_0", "lotr_season_winter_umbar_1", "lotr_season_winter_umbar_2" }),
                new StreakProfile(
                    new[] { "lotr_streak_peak_umbar_0", "lotr_streak_peak_umbar_1", "lotr_streak_peak_umbar_2" },
                    new[] { "lotr_streak_dire_umbar_0", "lotr_streak_dire_umbar_1", "lotr_streak_dire_umbar_2" }),
                new TerrainProfile(
                    new[] { TerrainContext.Plain },
                    new[] { "lotr_terrain_home_umbar_0", "lotr_terrain_home_umbar_1", "lotr_terrain_home_umbar_2" },
                    new[] { TerrainContext.Mountain, TerrainContext.Snow },
                    new[] { "lotr_terrain_foreign_umbar_0", "lotr_terrain_foreign_umbar_1", "lotr_terrain_foreign_umbar_2" })) },

            // HARADRIM PAIR — aserai vanilla remap; harad custom LOTRAOM ID.
            { "aserai", new CultureProfile("aserai", Alignment.Evil,
                new FoodProfile("fruit", 5f, Season.Summer,
                    new[] { "lotr_food_scarce_aserai_0",  "lotr_food_scarce_aserai_1",  "lotr_food_scarce_aserai_2"  },
                    new[] { "lotr_food_stocked_aserai_0", "lotr_food_stocked_aserai_1", "lotr_food_stocked_aserai_2" },
                    new[] { "lotr_food_surplus_aserai_0", "lotr_food_surplus_aserai_1", "lotr_food_surplus_aserai_2" }),
                new PrisonerProfile(
                    new[] { "lotr_pri_high_aserai_0", "lotr_pri_high_aserai_1", "lotr_pri_high_aserai_2" },
                    new[] { "lotr_pri_norm_aserai_0", "lotr_pri_norm_aserai_1", "lotr_pri_norm_aserai_2" },
                    new[] { "lotr_pri_low_aserai_0",  "lotr_pri_low_aserai_1",  "lotr_pri_low_aserai_2"  }),
                new CombinedProfile(
                    new[] { "lotr_peak_aserai_0", "lotr_peak_aserai_1", "lotr_peak_aserai_2" },
                    new[] { "lotr_dire_aserai_0", "lotr_dire_aserai_1", "lotr_dire_aserai_2" }),
                new SeasonProfile(
                    new[] { "lotr_season_spring_aserai_0", "lotr_season_spring_aserai_1", "lotr_season_spring_aserai_2" },
                    new[] { "lotr_season_summer_aserai_0", "lotr_season_summer_aserai_1", "lotr_season_summer_aserai_2" },
                    new[] { "lotr_season_autumn_aserai_0", "lotr_season_autumn_aserai_1", "lotr_season_autumn_aserai_2" },
                    new[] { "lotr_season_winter_aserai_0", "lotr_season_winter_aserai_1", "lotr_season_winter_aserai_2" }),
                new StreakProfile(
                    new[] { "lotr_streak_peak_aserai_0", "lotr_streak_peak_aserai_1", "lotr_streak_peak_aserai_2" },
                    new[] { "lotr_streak_dire_aserai_0", "lotr_streak_dire_aserai_1", "lotr_streak_dire_aserai_2" }),
                new TerrainProfile(
                    new[] { TerrainContext.Desert, TerrainContext.Steppe },
                    new[] { "lotr_terrain_home_aserai_0", "lotr_terrain_home_aserai_1", "lotr_terrain_home_aserai_2" },
                    new[] { TerrainContext.Mountain, TerrainContext.Forest, TerrainContext.Snow },
                    new[] { "lotr_terrain_foreign_aserai_0", "lotr_terrain_foreign_aserai_1", "lotr_terrain_foreign_aserai_2" })) },

            { "battania", new CultureProfile("battania", Alignment.Evil,
                new FoodProfile("cheese", 4f, Season.Autumn,
                    new[] { "lotr_food_scarce_battania_0",  "lotr_food_scarce_battania_1",  "lotr_food_scarce_battania_2"  },
                    new[] { "lotr_food_stocked_battania_0", "lotr_food_stocked_battania_1", "lotr_food_stocked_battania_2" },
                    new[] { "lotr_food_surplus_battania_0", "lotr_food_surplus_battania_1", "lotr_food_surplus_battania_2" }),
                new PrisonerProfile(
                    new[] { "lotr_pri_high_battania_0", "lotr_pri_high_battania_1", "lotr_pri_high_battania_2" },
                    new[] { "lotr_pri_norm_battania_0", "lotr_pri_norm_battania_1", "lotr_pri_norm_battania_2" },
                    new[] { "lotr_pri_low_battania_0",  "lotr_pri_low_battania_1",  "lotr_pri_low_battania_2"  }),
                new CombinedProfile(
                    new[] { "lotr_peak_battania_0", "lotr_peak_battania_1", "lotr_peak_battania_2" },
                    new[] { "lotr_dire_battania_0", "lotr_dire_battania_1", "lotr_dire_battania_2" }),
                new SeasonProfile(
                    new[] { "lotr_season_spring_battania_0", "lotr_season_spring_battania_1", "lotr_season_spring_battania_2" },
                    new[] { "lotr_season_summer_battania_0", "lotr_season_summer_battania_1", "lotr_season_summer_battania_2" },
                    new[] { "lotr_season_autumn_battania_0", "lotr_season_autumn_battania_1", "lotr_season_autumn_battania_2" },
                    new[] { "lotr_season_winter_battania_0", "lotr_season_winter_battania_1", "lotr_season_winter_battania_2" }),
                new StreakProfile(
                    new[] { "lotr_streak_peak_battania_0", "lotr_streak_peak_battania_1", "lotr_streak_peak_battania_2" },
                    new[] { "lotr_streak_dire_battania_0", "lotr_streak_dire_battania_1", "lotr_streak_dire_battania_2" }),
                new TerrainProfile(
                    new[] { TerrainContext.Steppe, TerrainContext.Plain },
                    new[] { "lotr_terrain_home_battania_0", "lotr_terrain_home_battania_1", "lotr_terrain_home_battania_2" },
                    new[] { TerrainContext.Forest },
                    new[] { "lotr_terrain_foreign_battania_0", "lotr_terrain_foreign_battania_1", "lotr_terrain_foreign_battania_2" })) },

            { "khuzait", new CultureProfile("khuzait", Alignment.Evil,
                new FoodProfile("meat", 6f, Season.Autumn,
                    new[] { "lotr_food_scarce_khuzait_0",  "lotr_food_scarce_khuzait_1",  "lotr_food_scarce_khuzait_2"  },
                    new[] { "lotr_food_stocked_khuzait_0", "lotr_food_stocked_khuzait_1", "lotr_food_stocked_khuzait_2" },
                    new[] { "lotr_food_surplus_khuzait_0", "lotr_food_surplus_khuzait_1", "lotr_food_surplus_khuzait_2" }),
                new PrisonerProfile(
                    new[] { "lotr_pri_high_khuzait_0", "lotr_pri_high_khuzait_1", "lotr_pri_high_khuzait_2" },
                    new[] { "lotr_pri_norm_khuzait_0", "lotr_pri_norm_khuzait_1", "lotr_pri_norm_khuzait_2" },
                    new[] { "lotr_pri_low_khuzait_0",  "lotr_pri_low_khuzait_1",  "lotr_pri_low_khuzait_2"  }),
                new CombinedProfile(
                    new[] { "lotr_peak_khuzait_0", "lotr_peak_khuzait_1", "lotr_peak_khuzait_2" },
                    new[] { "lotr_dire_khuzait_0", "lotr_dire_khuzait_1", "lotr_dire_khuzait_2" }),
                new SeasonProfile(
                    new[] { "lotr_season_spring_khuzait_0", "lotr_season_spring_khuzait_1", "lotr_season_spring_khuzait_2" },
                    new[] { "lotr_season_summer_khuzait_0", "lotr_season_summer_khuzait_1", "lotr_season_summer_khuzait_2" },
                    new[] { "lotr_season_autumn_khuzait_0", "lotr_season_autumn_khuzait_1", "lotr_season_autumn_khuzait_2" },
                    new[] { "lotr_season_winter_khuzait_0", "lotr_season_winter_khuzait_1", "lotr_season_winter_khuzait_2" }),
                new StreakProfile(
                    new[] { "lotr_streak_peak_khuzait_0", "lotr_streak_peak_khuzait_1", "lotr_streak_peak_khuzait_2" },
                    new[] { "lotr_streak_dire_khuzait_0", "lotr_streak_dire_khuzait_1", "lotr_streak_dire_khuzait_2" }),
                new TerrainProfile(
                    new[] { TerrainContext.Steppe, TerrainContext.Plain },
                    new[] { "lotr_terrain_home_khuzait_0", "lotr_terrain_home_khuzait_1", "lotr_terrain_home_khuzait_2" },
                    new[] { TerrainContext.Forest, TerrainContext.Mountain },
                    new[] { "lotr_terrain_foreign_khuzait_0", "lotr_terrain_foreign_khuzait_1", "lotr_terrain_foreign_khuzait_2" })) },

            { "rohan", new CultureProfile("rohan", Alignment.Good,
                new FoodProfile("grain", 6f, Season.Summer,
                    new[] { "lotr_food_scarce_rohan_0",  "lotr_food_scarce_rohan_1",  "lotr_food_scarce_rohan_2"  },
                    new[] { "lotr_food_stocked_rohan_0", "lotr_food_stocked_rohan_1", "lotr_food_stocked_rohan_2" },
                    new[] { "lotr_food_surplus_rohan_0", "lotr_food_surplus_rohan_1", "lotr_food_surplus_rohan_2" }),
                new PrisonerProfile(
                    new[] { "lotr_pri_high_rohan_0", "lotr_pri_high_rohan_1", "lotr_pri_high_rohan_2" },
                    new[] { "lotr_pri_norm_rohan_0", "lotr_pri_norm_rohan_1", "lotr_pri_norm_rohan_2" },
                    new[] { "lotr_pri_low_rohan_0",  "lotr_pri_low_rohan_1",  "lotr_pri_low_rohan_2"  }),
                new CombinedProfile(
                    new[] { "lotr_peak_rohan_0", "lotr_peak_rohan_1", "lotr_peak_rohan_2" },
                    new[] { "lotr_dire_rohan_0", "lotr_dire_rohan_1", "lotr_dire_rohan_2" }),
                new SeasonProfile(
                    new[] { "lotr_season_spring_rohan_0", "lotr_season_spring_rohan_1", "lotr_season_spring_rohan_2" },
                    new[] { "lotr_season_summer_rohan_0", "lotr_season_summer_rohan_1", "lotr_season_summer_rohan_2" },
                    new[] { "lotr_season_autumn_rohan_0", "lotr_season_autumn_rohan_1", "lotr_season_autumn_rohan_2" },
                    new[] { "lotr_season_winter_rohan_0", "lotr_season_winter_rohan_1", "lotr_season_winter_rohan_2" }),
                new StreakProfile(
                    new[] { "lotr_streak_peak_rohan_0", "lotr_streak_peak_rohan_1", "lotr_streak_peak_rohan_2" },
                    new[] { "lotr_streak_dire_rohan_0", "lotr_streak_dire_rohan_1", "lotr_streak_dire_rohan_2" }),
                new TerrainProfile(
                    new[] { TerrainContext.Plain, TerrainContext.Steppe },
                    new[] { "lotr_terrain_home_rohan_0", "lotr_terrain_home_rohan_1", "lotr_terrain_home_rohan_2" })) },

            { "dale", new CultureProfile("dale", Alignment.Good,
                new FoodProfile("fish", 5f, Season.Autumn,
                    new[] { "lotr_food_scarce_dale_0",  "lotr_food_scarce_dale_1",  "lotr_food_scarce_dale_2"  },
                    new[] { "lotr_food_stocked_dale_0", "lotr_food_stocked_dale_1", "lotr_food_stocked_dale_2" },
                    new[] { "lotr_food_surplus_dale_0", "lotr_food_surplus_dale_1", "lotr_food_surplus_dale_2" }),
                new PrisonerProfile(
                    new[] { "lotr_pri_high_dale_0", "lotr_pri_high_dale_1", "lotr_pri_high_dale_2" },
                    new[] { "lotr_pri_norm_dale_0", "lotr_pri_norm_dale_1", "lotr_pri_norm_dale_2" },
                    new[] { "lotr_pri_low_dale_0",  "lotr_pri_low_dale_1",  "lotr_pri_low_dale_2"  }),
                new CombinedProfile(
                    new[] { "lotr_peak_dale_0", "lotr_peak_dale_1", "lotr_peak_dale_2" },
                    new[] { "lotr_dire_dale_0", "lotr_dire_dale_1", "lotr_dire_dale_2" }),
                new SeasonProfile(
                    new[] { "lotr_season_spring_dale_0", "lotr_season_spring_dale_1", "lotr_season_spring_dale_2" },
                    new[] { "lotr_season_summer_dale_0", "lotr_season_summer_dale_1", "lotr_season_summer_dale_2" },
                    new[] { "lotr_season_autumn_dale_0", "lotr_season_autumn_dale_1", "lotr_season_autumn_dale_2" },
                    new[] { "lotr_season_winter_dale_0", "lotr_season_winter_dale_1", "lotr_season_winter_dale_2" }),
                new StreakProfile(
                    new[] { "lotr_streak_peak_dale_0", "lotr_streak_peak_dale_1", "lotr_streak_peak_dale_2" },
                    new[] { "lotr_streak_dire_dale_0", "lotr_streak_dire_dale_1", "lotr_streak_dire_dale_2" }),
                new TerrainProfile(
                    new[] { TerrainContext.Plain },
                    new[] { "lotr_terrain_home_dale_0", "lotr_terrain_home_dale_1", "lotr_terrain_home_dale_2" },
                    new[] { TerrainContext.Desert },
                    new[] { "lotr_terrain_foreign_dale_0", "lotr_terrain_foreign_dale_1", "lotr_terrain_foreign_dale_2" })) },

            // TODO: swap "vegetables" to "pipeweed" if LOTRAOM registers that item ID.
            { "shire", new CultureProfile("shire", Alignment.Good,
                new FoodProfile("vegetables", 5f, Season.Summer,
                    new[] { "lotr_food_scarce_shire_0",  "lotr_food_scarce_shire_1",  "lotr_food_scarce_shire_2"  },
                    new[] { "lotr_food_stocked_shire_0", "lotr_food_stocked_shire_1", "lotr_food_stocked_shire_2" },
                    new[] { "lotr_food_surplus_shire_0", "lotr_food_surplus_shire_1", "lotr_food_surplus_shire_2" }),
                new PrisonerProfile(
                    new[] { "lotr_pri_high_shire_0", "lotr_pri_high_shire_1", "lotr_pri_high_shire_2" },
                    new[] { "lotr_pri_norm_shire_0", "lotr_pri_norm_shire_1", "lotr_pri_norm_shire_2" },
                    new[] { "lotr_pri_low_shire_0",  "lotr_pri_low_shire_1",  "lotr_pri_low_shire_2"  }),
                new CombinedProfile(
                    new[] { "lotr_peak_shire_0", "lotr_peak_shire_1", "lotr_peak_shire_2" },
                    new[] { "lotr_dire_shire_0", "lotr_dire_shire_1", "lotr_dire_shire_2" }),
                new SeasonProfile(
                    new[] { "lotr_season_spring_shire_0", "lotr_season_spring_shire_1", "lotr_season_spring_shire_2" },
                    new[] { "lotr_season_summer_shire_0", "lotr_season_summer_shire_1", "lotr_season_summer_shire_2" },
                    new[] { "lotr_season_autumn_shire_0", "lotr_season_autumn_shire_1", "lotr_season_autumn_shire_2" },
                    new[] { "lotr_season_winter_shire_0", "lotr_season_winter_shire_1", "lotr_season_winter_shire_2" }),
                new StreakProfile(
                    new[] { "lotr_streak_peak_shire_0", "lotr_streak_peak_shire_1", "lotr_streak_peak_shire_2" },
                    new[] { "lotr_streak_dire_shire_0", "lotr_streak_dire_shire_1", "lotr_streak_dire_shire_2" }),
                new TerrainProfile(
                    new[] { TerrainContext.Plain, TerrainContext.Forest },
                    new[] { "lotr_terrain_home_shire_0", "lotr_terrain_home_shire_1", "lotr_terrain_home_shire_2" },
                    new[] { TerrainContext.Mountain, TerrainContext.Desert },
                    new[] { "lotr_terrain_foreign_shire_0", "lotr_terrain_foreign_shire_1", "lotr_terrain_foreign_shire_2" })) },

            { "harad", new CultureProfile("harad", Alignment.Evil,
                new FoodProfile("fruit", 5f, Season.Summer,
                    new[] { "lotr_food_scarce_harad_0",  "lotr_food_scarce_harad_1",  "lotr_food_scarce_harad_2"  },
                    new[] { "lotr_food_stocked_harad_0", "lotr_food_stocked_harad_1", "lotr_food_stocked_harad_2" },
                    new[] { "lotr_food_surplus_harad_0", "lotr_food_surplus_harad_1", "lotr_food_surplus_harad_2" }),
                new PrisonerProfile(
                    new[] { "lotr_pri_high_harad_0", "lotr_pri_high_harad_1", "lotr_pri_high_harad_2" },
                    new[] { "lotr_pri_norm_harad_0", "lotr_pri_norm_harad_1", "lotr_pri_norm_harad_2" },
                    new[] { "lotr_pri_low_harad_0",  "lotr_pri_low_harad_1",  "lotr_pri_low_harad_2"  }),
                new CombinedProfile(
                    new[] { "lotr_peak_harad_0", "lotr_peak_harad_1", "lotr_peak_harad_2" },
                    new[] { "lotr_dire_harad_0", "lotr_dire_harad_1", "lotr_dire_harad_2" }),
                new SeasonProfile(
                    new[] { "lotr_season_spring_harad_0", "lotr_season_spring_harad_1", "lotr_season_spring_harad_2" },
                    new[] { "lotr_season_summer_harad_0", "lotr_season_summer_harad_1", "lotr_season_summer_harad_2" },
                    new[] { "lotr_season_autumn_harad_0", "lotr_season_autumn_harad_1", "lotr_season_autumn_harad_2" },
                    new[] { "lotr_season_winter_harad_0", "lotr_season_winter_harad_1", "lotr_season_winter_harad_2" }),
                new StreakProfile(
                    new[] { "lotr_streak_peak_harad_0", "lotr_streak_peak_harad_1", "lotr_streak_peak_harad_2" },
                    new[] { "lotr_streak_dire_harad_0", "lotr_streak_dire_harad_1", "lotr_streak_dire_harad_2" }),
                new TerrainProfile(
                    new[] { TerrainContext.Desert },
                    new[] { "lotr_terrain_home_harad_0", "lotr_terrain_home_harad_1", "lotr_terrain_home_harad_2" },
                    new[] { TerrainContext.Mountain, TerrainContext.Forest, TerrainContext.Snow },
                    new[] { "lotr_terrain_foreign_harad_0", "lotr_terrain_foreign_harad_1", "lotr_terrain_foreign_harad_2" })) },

            { "dunland", new CultureProfile("dunland", Alignment.Evil,
                new FoodProfile("meat", 5f, Season.Winter,
                    new[] { "lotr_food_scarce_dunland_0",  "lotr_food_scarce_dunland_1",  "lotr_food_scarce_dunland_2"  },
                    new[] { "lotr_food_stocked_dunland_0", "lotr_food_stocked_dunland_1", "lotr_food_stocked_dunland_2" },
                    new[] { "lotr_food_surplus_dunland_0", "lotr_food_surplus_dunland_1", "lotr_food_surplus_dunland_2" }),
                new PrisonerProfile(
                    new[] { "lotr_pri_high_dunland_0", "lotr_pri_high_dunland_1", "lotr_pri_high_dunland_2" },
                    new[] { "lotr_pri_norm_dunland_0", "lotr_pri_norm_dunland_1", "lotr_pri_norm_dunland_2" },
                    new[] { "lotr_pri_low_dunland_0",  "lotr_pri_low_dunland_1",  "lotr_pri_low_dunland_2"  }),
                new CombinedProfile(
                    new[] { "lotr_peak_dunland_0", "lotr_peak_dunland_1", "lotr_peak_dunland_2" },
                    new[] { "lotr_dire_dunland_0", "lotr_dire_dunland_1", "lotr_dire_dunland_2" }),
                new SeasonProfile(
                    new[] { "lotr_season_spring_dunland_0", "lotr_season_spring_dunland_1", "lotr_season_spring_dunland_2" },
                    new[] { "lotr_season_summer_dunland_0", "lotr_season_summer_dunland_1", "lotr_season_summer_dunland_2" },
                    new[] { "lotr_season_autumn_dunland_0", "lotr_season_autumn_dunland_1", "lotr_season_autumn_dunland_2" },
                    new[] { "lotr_season_winter_dunland_0", "lotr_season_winter_dunland_1", "lotr_season_winter_dunland_2" }),
                new StreakProfile(
                    new[] { "lotr_streak_peak_dunland_0", "lotr_streak_peak_dunland_1", "lotr_streak_peak_dunland_2" },
                    new[] { "lotr_streak_dire_dunland_0", "lotr_streak_dire_dunland_1", "lotr_streak_dire_dunland_2" }),
                new TerrainProfile(
                    new[] { TerrainContext.Mountain },
                    new[] { "lotr_terrain_home_dunland_0", "lotr_terrain_home_dunland_1", "lotr_terrain_home_dunland_2" },
                    new[] { TerrainContext.Desert },
                    new[] { "lotr_terrain_foreign_dunland_0", "lotr_terrain_foreign_dunland_1", "lotr_terrain_foreign_dunland_2" })) },
        };

        public static (int lowCeiling, int highFloor) GetScaledPrisonerThresholds(float elapsedDays)
        {
            if (elapsedDays < 100f) return (1, 5);
            if (elapsedDays < 400f) return (2, 8);
            return (3, 12);
        }

        public static float GetPrisonerMoraleDelta(
            Alignment alignment, PrisonerState state, out string? tooltipKey)
        {
            tooltipKey = null;
            switch (alignment)
            {
                case Alignment.Evil:
                    if (state == PrisonerState.High)  { tooltipKey = KEY_PRI_BUFF;   return EVIL_HIGH_BONUS;   }
                    if (state == PrisonerState.Normal) { tooltipKey = KEY_PRI_NORMAL; return EVIL_NORMAL_BONUS; }
                    if (state == PrisonerState.Low)   { tooltipKey = KEY_PRI_DEB;    return EVIL_LOW_PENALTY;  }
                    break;
                case Alignment.Good:
                    if (state == PrisonerState.Low)   { tooltipKey = KEY_PRI_BUFF;   return GOOD_LOW_BONUS;    }
                    if (state == PrisonerState.High)  { tooltipKey = KEY_PRI_DEB;    return GOOD_HIGH_PENALTY; }
                    break;
            }
            return 0f;
        }
    }
}

// ── FactionExtrasData ─────────────────────────────────────────────────────────
// Separate namespace block so CultureProfile constructors are untouched.
// Key format:  lotr_victory_{id}_{n}
//              lotr_political_{ruler|vassal|independent}_{id}_{n}
//              lotr_renown_{unknown|noticed|renowned|legendary}_{id}_{n}
//              lotr_wounded_{id}_{n}
namespace LOTRAOM.FactionBuff.Data
{
    using System.Collections.Generic;
    public static class FactionExtrasData
    {
        public static readonly Dictionary<string, FactionExtras> Extras =
            new Dictionary<string, FactionExtras>(StringComparer.OrdinalIgnoreCase)
        {
            { "gondor", new FactionExtras(
                new VictoryProfile(new[] { "lotr_victory_gondor_0", "lotr_victory_gondor_1", "lotr_victory_gondor_2" }),
                new PoliticalProfile(
                    new[] { "lotr_political_ruler_gondor_0", "lotr_political_ruler_gondor_1", "lotr_political_ruler_gondor_2" },
                    new[] { "lotr_political_vassal_gondor_0", "lotr_political_vassal_gondor_1", "lotr_political_vassal_gondor_2" },
                    new[] { "lotr_political_independent_gondor_0", "lotr_political_independent_gondor_1", "lotr_political_independent_gondor_2" }),
                new RenownProfile(
                    new[] { "lotr_renown_unknown_gondor_0", "lotr_renown_unknown_gondor_1", "lotr_renown_unknown_gondor_2" },
                    new[] { "lotr_renown_noticed_gondor_0", "lotr_renown_noticed_gondor_1", "lotr_renown_noticed_gondor_2" },
                    new[] { "lotr_renown_renowned_gondor_0", "lotr_renown_renowned_gondor_1", "lotr_renown_renowned_gondor_2" },
                    new[] { "lotr_renown_legendary_gondor_0", "lotr_renown_legendary_gondor_1", "lotr_renown_legendary_gondor_2" }),
                new WoundProfile(new[] { "lotr_wounded_gondor_0", "lotr_wounded_gondor_1", "lotr_wounded_gondor_2" })) },

            { "vlandia", new FactionExtras(
                new VictoryProfile(new[] { "lotr_victory_vlandia_0", "lotr_victory_vlandia_1", "lotr_victory_vlandia_2" }),
                new PoliticalProfile(
                    new[] { "lotr_political_ruler_vlandia_0", "lotr_political_ruler_vlandia_1", "lotr_political_ruler_vlandia_2" },
                    new[] { "lotr_political_vassal_vlandia_0", "lotr_political_vassal_vlandia_1", "lotr_political_vassal_vlandia_2" },
                    new[] { "lotr_political_independent_vlandia_0", "lotr_political_independent_vlandia_1", "lotr_political_independent_vlandia_2" }),
                new RenownProfile(
                    new[] { "lotr_renown_unknown_vlandia_0", "lotr_renown_unknown_vlandia_1", "lotr_renown_unknown_vlandia_2" },
                    new[] { "lotr_renown_noticed_vlandia_0", "lotr_renown_noticed_vlandia_1", "lotr_renown_noticed_vlandia_2" },
                    new[] { "lotr_renown_renowned_vlandia_0", "lotr_renown_renowned_vlandia_1", "lotr_renown_renowned_vlandia_2" },
                    new[] { "lotr_renown_legendary_vlandia_0", "lotr_renown_legendary_vlandia_1", "lotr_renown_legendary_vlandia_2" }),
                new WoundProfile(new[] { "lotr_wounded_vlandia_0", "lotr_wounded_vlandia_1", "lotr_wounded_vlandia_2" })) },

            { "sturgia", new FactionExtras(
                new VictoryProfile(new[] { "lotr_victory_sturgia_0", "lotr_victory_sturgia_1", "lotr_victory_sturgia_2" }),
                new PoliticalProfile(
                    new[] { "lotr_political_ruler_sturgia_0", "lotr_political_ruler_sturgia_1", "lotr_political_ruler_sturgia_2" },
                    new[] { "lotr_political_vassal_sturgia_0", "lotr_political_vassal_sturgia_1", "lotr_political_vassal_sturgia_2" },
                    new[] { "lotr_political_independent_sturgia_0", "lotr_political_independent_sturgia_1", "lotr_political_independent_sturgia_2" }),
                new RenownProfile(
                    new[] { "lotr_renown_unknown_sturgia_0", "lotr_renown_unknown_sturgia_1", "lotr_renown_unknown_sturgia_2" },
                    new[] { "lotr_renown_noticed_sturgia_0", "lotr_renown_noticed_sturgia_1", "lotr_renown_noticed_sturgia_2" },
                    new[] { "lotr_renown_renowned_sturgia_0", "lotr_renown_renowned_sturgia_1", "lotr_renown_renowned_sturgia_2" },
                    new[] { "lotr_renown_legendary_sturgia_0", "lotr_renown_legendary_sturgia_1", "lotr_renown_legendary_sturgia_2" }),
                new WoundProfile(new[] { "lotr_wounded_sturgia_0", "lotr_wounded_sturgia_1", "lotr_wounded_sturgia_2" })) },

            { "rivendell", new FactionExtras(
                new VictoryProfile(new[] { "lotr_victory_rivendell_0", "lotr_victory_rivendell_1", "lotr_victory_rivendell_2" }),
                new PoliticalProfile(
                    new[] { "lotr_political_ruler_rivendell_0", "lotr_political_ruler_rivendell_1", "lotr_political_ruler_rivendell_2" },
                    new[] { "lotr_political_vassal_rivendell_0", "lotr_political_vassal_rivendell_1", "lotr_political_vassal_rivendell_2" },
                    new[] { "lotr_political_independent_rivendell_0", "lotr_political_independent_rivendell_1", "lotr_political_independent_rivendell_2" }),
                new RenownProfile(
                    new[] { "lotr_renown_unknown_rivendell_0", "lotr_renown_unknown_rivendell_1", "lotr_renown_unknown_rivendell_2" },
                    new[] { "lotr_renown_noticed_rivendell_0", "lotr_renown_noticed_rivendell_1", "lotr_renown_noticed_rivendell_2" },
                    new[] { "lotr_renown_renowned_rivendell_0", "lotr_renown_renowned_rivendell_1", "lotr_renown_renowned_rivendell_2" },
                    new[] { "lotr_renown_legendary_rivendell_0", "lotr_renown_legendary_rivendell_1", "lotr_renown_legendary_rivendell_2" }),
                new WoundProfile(new[] { "lotr_wounded_rivendell_0", "lotr_wounded_rivendell_1", "lotr_wounded_rivendell_2" })) },

            { "mirkwood", new FactionExtras(
                new VictoryProfile(new[] { "lotr_victory_mirkwood_0", "lotr_victory_mirkwood_1", "lotr_victory_mirkwood_2" }),
                new PoliticalProfile(
                    new[] { "lotr_political_ruler_mirkwood_0", "lotr_political_ruler_mirkwood_1", "lotr_political_ruler_mirkwood_2" },
                    new[] { "lotr_political_vassal_mirkwood_0", "lotr_political_vassal_mirkwood_1", "lotr_political_vassal_mirkwood_2" },
                    new[] { "lotr_political_independent_mirkwood_0", "lotr_political_independent_mirkwood_1", "lotr_political_independent_mirkwood_2" }),
                new RenownProfile(
                    new[] { "lotr_renown_unknown_mirkwood_0", "lotr_renown_unknown_mirkwood_1", "lotr_renown_unknown_mirkwood_2" },
                    new[] { "lotr_renown_noticed_mirkwood_0", "lotr_renown_noticed_mirkwood_1", "lotr_renown_noticed_mirkwood_2" },
                    new[] { "lotr_renown_renowned_mirkwood_0", "lotr_renown_renowned_mirkwood_1", "lotr_renown_renowned_mirkwood_2" },
                    new[] { "lotr_renown_legendary_mirkwood_0", "lotr_renown_legendary_mirkwood_1", "lotr_renown_legendary_mirkwood_2" }),
                new WoundProfile(new[] { "lotr_wounded_mirkwood_0", "lotr_wounded_mirkwood_1", "lotr_wounded_mirkwood_2" })) },

            { "lothlorien", new FactionExtras(
                new VictoryProfile(new[] { "lotr_victory_lothlorien_0", "lotr_victory_lothlorien_1", "lotr_victory_lothlorien_2" }),
                new PoliticalProfile(
                    new[] { "lotr_political_ruler_lothlorien_0", "lotr_political_ruler_lothlorien_1", "lotr_political_ruler_lothlorien_2" },
                    new[] { "lotr_political_vassal_lothlorien_0", "lotr_political_vassal_lothlorien_1", "lotr_political_vassal_lothlorien_2" },
                    new[] { "lotr_political_independent_lothlorien_0", "lotr_political_independent_lothlorien_1", "lotr_political_independent_lothlorien_2" }),
                new RenownProfile(
                    new[] { "lotr_renown_unknown_lothlorien_0", "lotr_renown_unknown_lothlorien_1", "lotr_renown_unknown_lothlorien_2" },
                    new[] { "lotr_renown_noticed_lothlorien_0", "lotr_renown_noticed_lothlorien_1", "lotr_renown_noticed_lothlorien_2" },
                    new[] { "lotr_renown_renowned_lothlorien_0", "lotr_renown_renowned_lothlorien_1", "lotr_renown_renowned_lothlorien_2" },
                    new[] { "lotr_renown_legendary_lothlorien_0", "lotr_renown_legendary_lothlorien_1", "lotr_renown_legendary_lothlorien_2" }),
                new WoundProfile(new[] { "lotr_wounded_lothlorien_0", "lotr_wounded_lothlorien_1", "lotr_wounded_lothlorien_2" })) },

            { "erebor", new FactionExtras(
                new VictoryProfile(new[] { "lotr_victory_erebor_0", "lotr_victory_erebor_1", "lotr_victory_erebor_2" }),
                new PoliticalProfile(
                    new[] { "lotr_political_ruler_erebor_0", "lotr_political_ruler_erebor_1", "lotr_political_ruler_erebor_2" },
                    new[] { "lotr_political_vassal_erebor_0", "lotr_political_vassal_erebor_1", "lotr_political_vassal_erebor_2" },
                    new[] { "lotr_political_independent_erebor_0", "lotr_political_independent_erebor_1", "lotr_political_independent_erebor_2" }),
                new RenownProfile(
                    new[] { "lotr_renown_unknown_erebor_0", "lotr_renown_unknown_erebor_1", "lotr_renown_unknown_erebor_2" },
                    new[] { "lotr_renown_noticed_erebor_0", "lotr_renown_noticed_erebor_1", "lotr_renown_noticed_erebor_2" },
                    new[] { "lotr_renown_renowned_erebor_0", "lotr_renown_renowned_erebor_1", "lotr_renown_renowned_erebor_2" },
                    new[] { "lotr_renown_legendary_erebor_0", "lotr_renown_legendary_erebor_1", "lotr_renown_legendary_erebor_2" }),
                new WoundProfile(new[] { "lotr_wounded_erebor_0", "lotr_wounded_erebor_1", "lotr_wounded_erebor_2" })) },

            { "empire", new FactionExtras(
                new VictoryProfile(new[] { "lotr_victory_empire_0", "lotr_victory_empire_1", "lotr_victory_empire_2" }),
                new PoliticalProfile(
                    new[] { "lotr_political_ruler_empire_0", "lotr_political_ruler_empire_1", "lotr_political_ruler_empire_2" },
                    new[] { "lotr_political_vassal_empire_0", "lotr_political_vassal_empire_1", "lotr_political_vassal_empire_2" },
                    new[] { "lotr_political_independent_empire_0", "lotr_political_independent_empire_1", "lotr_political_independent_empire_2" }),
                new RenownProfile(
                    new[] { "lotr_renown_unknown_empire_0", "lotr_renown_unknown_empire_1", "lotr_renown_unknown_empire_2" },
                    new[] { "lotr_renown_noticed_empire_0", "lotr_renown_noticed_empire_1", "lotr_renown_noticed_empire_2" },
                    new[] { "lotr_renown_renowned_empire_0", "lotr_renown_renowned_empire_1", "lotr_renown_renowned_empire_2" },
                    new[] { "lotr_renown_legendary_empire_0", "lotr_renown_legendary_empire_1", "lotr_renown_legendary_empire_2" }),
                new WoundProfile(new[] { "lotr_wounded_empire_0", "lotr_wounded_empire_1", "lotr_wounded_empire_2" })) },

            { "mordor", new FactionExtras(
                new VictoryProfile(new[] { "lotr_victory_mordor_0", "lotr_victory_mordor_1", "lotr_victory_mordor_2" }),
                new PoliticalProfile(
                    new[] { "lotr_political_ruler_mordor_0", "lotr_political_ruler_mordor_1", "lotr_political_ruler_mordor_2" },
                    new[] { "lotr_political_vassal_mordor_0", "lotr_political_vassal_mordor_1", "lotr_political_vassal_mordor_2" },
                    new[] { "lotr_political_independent_mordor_0", "lotr_political_independent_mordor_1", "lotr_political_independent_mordor_2" }),
                new RenownProfile(
                    new[] { "lotr_renown_unknown_mordor_0", "lotr_renown_unknown_mordor_1", "lotr_renown_unknown_mordor_2" },
                    new[] { "lotr_renown_noticed_mordor_0", "lotr_renown_noticed_mordor_1", "lotr_renown_noticed_mordor_2" },
                    new[] { "lotr_renown_renowned_mordor_0", "lotr_renown_renowned_mordor_1", "lotr_renown_renowned_mordor_2" },
                    new[] { "lotr_renown_legendary_mordor_0", "lotr_renown_legendary_mordor_1", "lotr_renown_legendary_mordor_2" }),
                new WoundProfile(new[] { "lotr_wounded_mordor_0", "lotr_wounded_mordor_1", "lotr_wounded_mordor_2" })) },

            { "isengard", new FactionExtras(
                new VictoryProfile(new[] { "lotr_victory_isengard_0", "lotr_victory_isengard_1", "lotr_victory_isengard_2" }),
                new PoliticalProfile(
                    new[] { "lotr_political_ruler_isengard_0", "lotr_political_ruler_isengard_1", "lotr_political_ruler_isengard_2" },
                    new[] { "lotr_political_vassal_isengard_0", "lotr_political_vassal_isengard_1", "lotr_political_vassal_isengard_2" },
                    new[] { "lotr_political_independent_isengard_0", "lotr_political_independent_isengard_1", "lotr_political_independent_isengard_2" }),
                new RenownProfile(
                    new[] { "lotr_renown_unknown_isengard_0", "lotr_renown_unknown_isengard_1", "lotr_renown_unknown_isengard_2" },
                    new[] { "lotr_renown_noticed_isengard_0", "lotr_renown_noticed_isengard_1", "lotr_renown_noticed_isengard_2" },
                    new[] { "lotr_renown_renowned_isengard_0", "lotr_renown_renowned_isengard_1", "lotr_renown_renowned_isengard_2" },
                    new[] { "lotr_renown_legendary_isengard_0", "lotr_renown_legendary_isengard_1", "lotr_renown_legendary_isengard_2" }),
                new WoundProfile(new[] { "lotr_wounded_isengard_0", "lotr_wounded_isengard_1", "lotr_wounded_isengard_2" })) },

            { "gundabad", new FactionExtras(
                new VictoryProfile(new[] { "lotr_victory_gundabad_0", "lotr_victory_gundabad_1", "lotr_victory_gundabad_2" }),
                new PoliticalProfile(
                    new[] { "lotr_political_ruler_gundabad_0", "lotr_political_ruler_gundabad_1", "lotr_political_ruler_gundabad_2" },
                    new[] { "lotr_political_vassal_gundabad_0", "lotr_political_vassal_gundabad_1", "lotr_political_vassal_gundabad_2" },
                    new[] { "lotr_political_independent_gundabad_0", "lotr_political_independent_gundabad_1", "lotr_political_independent_gundabad_2" }),
                new RenownProfile(
                    new[] { "lotr_renown_unknown_gundabad_0", "lotr_renown_unknown_gundabad_1", "lotr_renown_unknown_gundabad_2" },
                    new[] { "lotr_renown_noticed_gundabad_0", "lotr_renown_noticed_gundabad_1", "lotr_renown_noticed_gundabad_2" },
                    new[] { "lotr_renown_renowned_gundabad_0", "lotr_renown_renowned_gundabad_1", "lotr_renown_renowned_gundabad_2" },
                    new[] { "lotr_renown_legendary_gundabad_0", "lotr_renown_legendary_gundabad_1", "lotr_renown_legendary_gundabad_2" }),
                new WoundProfile(new[] { "lotr_wounded_gundabad_0", "lotr_wounded_gundabad_1", "lotr_wounded_gundabad_2" })) },

            { "dolguldur", new FactionExtras(
                new VictoryProfile(new[] { "lotr_victory_dolguldur_0", "lotr_victory_dolguldur_1", "lotr_victory_dolguldur_2" }),
                new PoliticalProfile(
                    new[] { "lotr_political_ruler_dolguldur_0", "lotr_political_ruler_dolguldur_1", "lotr_political_ruler_dolguldur_2" },
                    new[] { "lotr_political_vassal_dolguldur_0", "lotr_political_vassal_dolguldur_1", "lotr_political_vassal_dolguldur_2" },
                    new[] { "lotr_political_independent_dolguldur_0", "lotr_political_independent_dolguldur_1", "lotr_political_independent_dolguldur_2" }),
                new RenownProfile(
                    new[] { "lotr_renown_unknown_dolguldur_0", "lotr_renown_unknown_dolguldur_1", "lotr_renown_unknown_dolguldur_2" },
                    new[] { "lotr_renown_noticed_dolguldur_0", "lotr_renown_noticed_dolguldur_1", "lotr_renown_noticed_dolguldur_2" },
                    new[] { "lotr_renown_renowned_dolguldur_0", "lotr_renown_renowned_dolguldur_1", "lotr_renown_renowned_dolguldur_2" },
                    new[] { "lotr_renown_legendary_dolguldur_0", "lotr_renown_legendary_dolguldur_1", "lotr_renown_legendary_dolguldur_2" }),
                new WoundProfile(new[] { "lotr_wounded_dolguldur_0", "lotr_wounded_dolguldur_1", "lotr_wounded_dolguldur_2" })) },

            { "umbar", new FactionExtras(
                new VictoryProfile(new[] { "lotr_victory_umbar_0", "lotr_victory_umbar_1", "lotr_victory_umbar_2" }),
                new PoliticalProfile(
                    new[] { "lotr_political_ruler_umbar_0", "lotr_political_ruler_umbar_1", "lotr_political_ruler_umbar_2" },
                    new[] { "lotr_political_vassal_umbar_0", "lotr_political_vassal_umbar_1", "lotr_political_vassal_umbar_2" },
                    new[] { "lotr_political_independent_umbar_0", "lotr_political_independent_umbar_1", "lotr_political_independent_umbar_2" }),
                new RenownProfile(
                    new[] { "lotr_renown_unknown_umbar_0", "lotr_renown_unknown_umbar_1", "lotr_renown_unknown_umbar_2" },
                    new[] { "lotr_renown_noticed_umbar_0", "lotr_renown_noticed_umbar_1", "lotr_renown_noticed_umbar_2" },
                    new[] { "lotr_renown_renowned_umbar_0", "lotr_renown_renowned_umbar_1", "lotr_renown_renowned_umbar_2" },
                    new[] { "lotr_renown_legendary_umbar_0", "lotr_renown_legendary_umbar_1", "lotr_renown_legendary_umbar_2" }),
                new WoundProfile(new[] { "lotr_wounded_umbar_0", "lotr_wounded_umbar_1", "lotr_wounded_umbar_2" })) },

            { "aserai", new FactionExtras(
                new VictoryProfile(new[] { "lotr_victory_aserai_0", "lotr_victory_aserai_1", "lotr_victory_aserai_2" }),
                new PoliticalProfile(
                    new[] { "lotr_political_ruler_aserai_0", "lotr_political_ruler_aserai_1", "lotr_political_ruler_aserai_2" },
                    new[] { "lotr_political_vassal_aserai_0", "lotr_political_vassal_aserai_1", "lotr_political_vassal_aserai_2" },
                    new[] { "lotr_political_independent_aserai_0", "lotr_political_independent_aserai_1", "lotr_political_independent_aserai_2" }),
                new RenownProfile(
                    new[] { "lotr_renown_unknown_aserai_0", "lotr_renown_unknown_aserai_1", "lotr_renown_unknown_aserai_2" },
                    new[] { "lotr_renown_noticed_aserai_0", "lotr_renown_noticed_aserai_1", "lotr_renown_noticed_aserai_2" },
                    new[] { "lotr_renown_renowned_aserai_0", "lotr_renown_renowned_aserai_1", "lotr_renown_renowned_aserai_2" },
                    new[] { "lotr_renown_legendary_aserai_0", "lotr_renown_legendary_aserai_1", "lotr_renown_legendary_aserai_2" }),
                new WoundProfile(new[] { "lotr_wounded_aserai_0", "lotr_wounded_aserai_1", "lotr_wounded_aserai_2" })) },

            { "battania", new FactionExtras(
                new VictoryProfile(new[] { "lotr_victory_battania_0", "lotr_victory_battania_1", "lotr_victory_battania_2" }),
                new PoliticalProfile(
                    new[] { "lotr_political_ruler_battania_0", "lotr_political_ruler_battania_1", "lotr_political_ruler_battania_2" },
                    new[] { "lotr_political_vassal_battania_0", "lotr_political_vassal_battania_1", "lotr_political_vassal_battania_2" },
                    new[] { "lotr_political_independent_battania_0", "lotr_political_independent_battania_1", "lotr_political_independent_battania_2" }),
                new RenownProfile(
                    new[] { "lotr_renown_unknown_battania_0", "lotr_renown_unknown_battania_1", "lotr_renown_unknown_battania_2" },
                    new[] { "lotr_renown_noticed_battania_0", "lotr_renown_noticed_battania_1", "lotr_renown_noticed_battania_2" },
                    new[] { "lotr_renown_renowned_battania_0", "lotr_renown_renowned_battania_1", "lotr_renown_renowned_battania_2" },
                    new[] { "lotr_renown_legendary_battania_0", "lotr_renown_legendary_battania_1", "lotr_renown_legendary_battania_2" }),
                new WoundProfile(new[] { "lotr_wounded_battania_0", "lotr_wounded_battania_1", "lotr_wounded_battania_2" })) },

            { "khuzait", new FactionExtras(
                new VictoryProfile(new[] { "lotr_victory_khuzait_0", "lotr_victory_khuzait_1", "lotr_victory_khuzait_2" }),
                new PoliticalProfile(
                    new[] { "lotr_political_ruler_khuzait_0", "lotr_political_ruler_khuzait_1", "lotr_political_ruler_khuzait_2" },
                    new[] { "lotr_political_vassal_khuzait_0", "lotr_political_vassal_khuzait_1", "lotr_political_vassal_khuzait_2" },
                    new[] { "lotr_political_independent_khuzait_0", "lotr_political_independent_khuzait_1", "lotr_political_independent_khuzait_2" }),
                new RenownProfile(
                    new[] { "lotr_renown_unknown_khuzait_0", "lotr_renown_unknown_khuzait_1", "lotr_renown_unknown_khuzait_2" },
                    new[] { "lotr_renown_noticed_khuzait_0", "lotr_renown_noticed_khuzait_1", "lotr_renown_noticed_khuzait_2" },
                    new[] { "lotr_renown_renowned_khuzait_0", "lotr_renown_renowned_khuzait_1", "lotr_renown_renowned_khuzait_2" },
                    new[] { "lotr_renown_legendary_khuzait_0", "lotr_renown_legendary_khuzait_1", "lotr_renown_legendary_khuzait_2" }),
                new WoundProfile(new[] { "lotr_wounded_khuzait_0", "lotr_wounded_khuzait_1", "lotr_wounded_khuzait_2" })) },

            { "rohan", new FactionExtras(
                new VictoryProfile(new[] { "lotr_victory_rohan_0", "lotr_victory_rohan_1", "lotr_victory_rohan_2" }),
                new PoliticalProfile(
                    new[] { "lotr_political_ruler_rohan_0", "lotr_political_ruler_rohan_1", "lotr_political_ruler_rohan_2" },
                    new[] { "lotr_political_vassal_rohan_0", "lotr_political_vassal_rohan_1", "lotr_political_vassal_rohan_2" },
                    new[] { "lotr_political_independent_rohan_0", "lotr_political_independent_rohan_1", "lotr_political_independent_rohan_2" }),
                new RenownProfile(
                    new[] { "lotr_renown_unknown_rohan_0", "lotr_renown_unknown_rohan_1", "lotr_renown_unknown_rohan_2" },
                    new[] { "lotr_renown_noticed_rohan_0", "lotr_renown_noticed_rohan_1", "lotr_renown_noticed_rohan_2" },
                    new[] { "lotr_renown_renowned_rohan_0", "lotr_renown_renowned_rohan_1", "lotr_renown_renowned_rohan_2" },
                    new[] { "lotr_renown_legendary_rohan_0", "lotr_renown_legendary_rohan_1", "lotr_renown_legendary_rohan_2" }),
                new WoundProfile(new[] { "lotr_wounded_rohan_0", "lotr_wounded_rohan_1", "lotr_wounded_rohan_2" })) },

            { "dale", new FactionExtras(
                new VictoryProfile(new[] { "lotr_victory_dale_0", "lotr_victory_dale_1", "lotr_victory_dale_2" }),
                new PoliticalProfile(
                    new[] { "lotr_political_ruler_dale_0", "lotr_political_ruler_dale_1", "lotr_political_ruler_dale_2" },
                    new[] { "lotr_political_vassal_dale_0", "lotr_political_vassal_dale_1", "lotr_political_vassal_dale_2" },
                    new[] { "lotr_political_independent_dale_0", "lotr_political_independent_dale_1", "lotr_political_independent_dale_2" }),
                new RenownProfile(
                    new[] { "lotr_renown_unknown_dale_0", "lotr_renown_unknown_dale_1", "lotr_renown_unknown_dale_2" },
                    new[] { "lotr_renown_noticed_dale_0", "lotr_renown_noticed_dale_1", "lotr_renown_noticed_dale_2" },
                    new[] { "lotr_renown_renowned_dale_0", "lotr_renown_renowned_dale_1", "lotr_renown_renowned_dale_2" },
                    new[] { "lotr_renown_legendary_dale_0", "lotr_renown_legendary_dale_1", "lotr_renown_legendary_dale_2" }),
                new WoundProfile(new[] { "lotr_wounded_dale_0", "lotr_wounded_dale_1", "lotr_wounded_dale_2" })) },

            { "shire", new FactionExtras(
                new VictoryProfile(new[] { "lotr_victory_shire_0", "lotr_victory_shire_1", "lotr_victory_shire_2" }),
                new PoliticalProfile(
                    new[] { "lotr_political_ruler_shire_0", "lotr_political_ruler_shire_1", "lotr_political_ruler_shire_2" },
                    new[] { "lotr_political_vassal_shire_0", "lotr_political_vassal_shire_1", "lotr_political_vassal_shire_2" },
                    new[] { "lotr_political_independent_shire_0", "lotr_political_independent_shire_1", "lotr_political_independent_shire_2" }),
                new RenownProfile(
                    new[] { "lotr_renown_unknown_shire_0", "lotr_renown_unknown_shire_1", "lotr_renown_unknown_shire_2" },
                    new[] { "lotr_renown_noticed_shire_0", "lotr_renown_noticed_shire_1", "lotr_renown_noticed_shire_2" },
                    new[] { "lotr_renown_renowned_shire_0", "lotr_renown_renowned_shire_1", "lotr_renown_renowned_shire_2" },
                    new[] { "lotr_renown_legendary_shire_0", "lotr_renown_legendary_shire_1", "lotr_renown_legendary_shire_2" }),
                new WoundProfile(new[] { "lotr_wounded_shire_0", "lotr_wounded_shire_1", "lotr_wounded_shire_2" })) },

            { "harad", new FactionExtras(
                new VictoryProfile(new[] { "lotr_victory_harad_0", "lotr_victory_harad_1", "lotr_victory_harad_2" }),
                new PoliticalProfile(
                    new[] { "lotr_political_ruler_harad_0", "lotr_political_ruler_harad_1", "lotr_political_ruler_harad_2" },
                    new[] { "lotr_political_vassal_harad_0", "lotr_political_vassal_harad_1", "lotr_political_vassal_harad_2" },
                    new[] { "lotr_political_independent_harad_0", "lotr_political_independent_harad_1", "lotr_political_independent_harad_2" }),
                new RenownProfile(
                    new[] { "lotr_renown_unknown_harad_0", "lotr_renown_unknown_harad_1", "lotr_renown_unknown_harad_2" },
                    new[] { "lotr_renown_noticed_harad_0", "lotr_renown_noticed_harad_1", "lotr_renown_noticed_harad_2" },
                    new[] { "lotr_renown_renowned_harad_0", "lotr_renown_renowned_harad_1", "lotr_renown_renowned_harad_2" },
                    new[] { "lotr_renown_legendary_harad_0", "lotr_renown_legendary_harad_1", "lotr_renown_legendary_harad_2" }),
                new WoundProfile(new[] { "lotr_wounded_harad_0", "lotr_wounded_harad_1", "lotr_wounded_harad_2" })) },

            { "dunland", new FactionExtras(
                new VictoryProfile(new[] { "lotr_victory_dunland_0", "lotr_victory_dunland_1", "lotr_victory_dunland_2" }),
                new PoliticalProfile(
                    new[] { "lotr_political_ruler_dunland_0", "lotr_political_ruler_dunland_1", "lotr_political_ruler_dunland_2" },
                    new[] { "lotr_political_vassal_dunland_0", "lotr_political_vassal_dunland_1", "lotr_political_vassal_dunland_2" },
                    new[] { "lotr_political_independent_dunland_0", "lotr_political_independent_dunland_1", "lotr_political_independent_dunland_2" }),
                new RenownProfile(
                    new[] { "lotr_renown_unknown_dunland_0", "lotr_renown_unknown_dunland_1", "lotr_renown_unknown_dunland_2" },
                    new[] { "lotr_renown_noticed_dunland_0", "lotr_renown_noticed_dunland_1", "lotr_renown_noticed_dunland_2" },
                    new[] { "lotr_renown_renowned_dunland_0", "lotr_renown_renowned_dunland_1", "lotr_renown_renowned_dunland_2" },
                    new[] { "lotr_renown_legendary_dunland_0", "lotr_renown_legendary_dunland_1", "lotr_renown_legendary_dunland_2" }),
                new WoundProfile(new[] { "lotr_wounded_dunland_0", "lotr_wounded_dunland_1", "lotr_wounded_dunland_2" })) },
        };

        // Renown tier thresholds
        public const float RENOWN_NOTICED   =  50f;
        public const float RENOWN_RENOWNED  = 200f;
        public const float RENOWN_LEGENDARY = 500f;

        // Colours for new systems
        public const uint COLOUR_VICTORY   = 0xFFCCAA33u;
        public const uint COLOUR_POLITICAL = 0xFF8899CCu;
        public const uint COLOUR_RENOWN    = 0xFFBBAA55u;
        public const uint COLOUR_WOUNDED   = 0xFF994433u;
    }
}
