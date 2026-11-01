using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Library;

namespace TAOM_LordsLedger
{
    /// <summary>
    /// Pure static data reading. No mutable state. Never modifies settlement data.
    /// All TaleWorlds API calls that may fail on edge-case settlement types are
    /// guarded with null-checks or try/catch returning safe zero defaults.
    /// </summary>
    public static class LedgerDataReader
    {
        // ── TAOM / vanilla culture → display adjective ───────────────────
        private static readonly Dictionary<string, string> _adjectives =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // TAOM Free Peoples
            ["gondor"]     = "Gondorian",
            ["rohan"]      = "Rohirrim",
            ["erebor"]     = "Dwarven",
            ["rivendell"]  = "Noldor",
            ["lothlorien"] = "Galadhrim",
            ["mirkwood"]   = "Silvan",
            // TAOM Shadow
            ["mordor"]     = "Mordor",
            ["isengard"]   = "Isengard",
            ["gundabad"]   = "Gundabad",
            ["dolguldur"]  = "Shadow",
            ["umbar"]      = "Corsair",
            ["harad"]      = "Haradrim",
            ["dunland"]    = "Dunlending",
            ["rhun"]       = "Easterling",
            // Vanilla Bannerlord
            ["empire"]     = "Imperial",
            ["sturgia"]    = "Sturgian",
            ["vlandia"]    = "Vlandian",
            ["battania"]   = "Battanian",
            ["aserai"]     = "Aserai",
            ["khuzait"]    = "Khuzait",
        };

        // Cultures that use a stronger noun for castles
        private static readonly Dictionary<string, string> _castleNouns =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["erebor"]     = "Stronghold",
            ["mordor"]     = "Fortress",
            ["isengard"]   = "Fortress",
            ["gundabad"]   = "Fortress",
            ["dolguldur"]  = "Citadel",
        };

        // ── Public entry point ────────────────────────────────────────────

        /// <summary>
        /// Builds a complete FiefInfo snapshot for <paramref name="s"/>.
        /// </summary>
        public static FiefInfo Read(
            Settlement s,
            LedgerSettings settings,
            Dictionary<string, float> loyaltyHistory)
        {
            string cultureId = GetCultureId(s);

            var info = new FiefInfo
            {
                Name     = s.Name?.ToString() ?? s.StringId,
                IsVassal = IsVassal(s),
            };

            if (s.IsTown)
            {
                info.TypeLabel = MakeTypeLabel(cultureId, "Town");
                ReadTownOrCastle(s, ref info, settings, loyaltyHistory);
            }
            else if (s.IsCastle)
            {
                string noun    = _castleNouns.TryGetValue(cultureId, out var cn) ? cn : "Castle";
                info.TypeLabel = MakeTypeLabel(cultureId, noun);
                ReadTownOrCastle(s, ref info, settings, loyaltyHistory);
            }
            else if (s.IsVillage)
            {
                info.IsVillage  = true;
                info.TypeLabel  = MakeTypeLabel(cultureId, "Village");
                info.Prosperity = s.Village?.Hearth ?? 0f;

                var vs = s.Village?.VillageState;
                info.IsRaidedOrLooted =
                    vs == Village.VillageStates.BeingRaided ||
                    vs == Village.VillageStates.Looted;
            }

            info.HasWarning = ComputeWarning(ref info, settings);
            info.RiskScore  = ComputeRisk(ref info, settings);
            ApplyTriage(ref info, settings);
            return info;
        }

        // ── Town / castle reading ─────────────────────────────────────────

        private static void ReadTownOrCastle(
            Settlement s,
            ref FiefInfo info,
            LedgerSettings settings,
            Dictionary<string, float> loyaltyHistory)
        {
            var town = s.Town;
            if (town == null) return;

            info.Prosperity   = town.Prosperity;
            info.Loyalty      = town.Loyalty;
            info.Security     = town.Security;
            info.FoodChange   = town.FoodChange;
            info.IsUnderSiege = s.IsUnderSiege;
            info.WallLevel    = town.GetWallLevel();
            info.GovernorName = town.Governor?.Name?.ToString() ?? "\u2014"; // em dash

            var garrison = town.GarrisonParty;
            info.GarrisonCount     = garrison?.MemberRoster?.TotalManCount ?? 0;
            info.MilitiaCount      = (int)s.Militia;
            info.BoundVillageCount = s.BoundVillages?.Count ?? 0;

            // Loyalty trend
            info.LoyaltyDelta = loyaltyHistory.TryGetValue(s.StringId, out float prev)
                ? info.Loyalty - prev
                : float.NaN;

            // Construction and prisoners
            info.BuildingInProgress = TryGetCurrentBuilding(town);
            info.NotablePrisoners   = TryGetHeroPrisoners(s);

            // Danger: below garrison threshold AND a hostile army is within danger radius
            info.IsDanger = info.GarrisonCount > 0
                         && info.GarrisonCount < settings.LowGarrison
                         && HasNearbyHostileArmy(s, settings.DangerRadius);
        }

        // ── Construction ─────────────────────────────────────────────────

        private static string TryGetCurrentBuilding(Town town)
        {
            try
            {
                // Town.CurrentBuilding is available in BL 1.2.x
                var b = town.CurrentBuilding;
                return b?.BuildingType?.Name?.ToString();
            }
            catch { return null; }
        }

        // ── Prisoners ────────────────────────────────────────────────────

        private static List<string> TryGetHeroPrisoners(Settlement s)
        {
            try
            {
                var roster = s.Party?.PrisonRoster;
                if (roster == null) return null;

                List<string> names = null;
                foreach (var element in roster.GetTroopRoster())
                {
                    if (!element.Character.IsHero) continue;
                    var hero = element.Character.HeroObject;
                    if (hero == null || hero == Hero.MainHero) continue;
                    if (names == null) names = new List<string>(4);
                    names.Add(hero.Name?.ToString() ?? "?");
                }
                return names;
            }
            catch { return null; }
        }

        // ── Nearby hostile army ───────────────────────────────────────────

        private static bool HasNearbyHostileArmy(Settlement s, float radius)
        {
            try
            {
                var playerFaction = Hero.MainHero?.MapFaction;
                if (playerFaction == null) return false;

                Vec2 gatePos = s.GatePosition.ToVec2();
                float radiusSq = radius * radius;

                foreach (var party in MobileParty.All)
                {
                    if (party == null || party.IsMainParty) continue;

                    // Only consider armies or parties large enough to be a threat
                    if (party.Army == null && party.MemberRoster.TotalManCount < 80) continue;

                    var fac = party.MapFaction;
                    if (fac == null) continue;

                    bool atWar;
                    try
                    {
                        atWar = FactionManager.IsAtWarAgainstFaction(playerFaction, fac);
                    }
                    catch
                    {
                        continue;
                    }

                    if (!atWar) continue;

                    if ((party.GetPosition2D - gatePos).LengthSquared < radiusSq)
                        return true;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        // ── Warning & risk ────────────────────────────────────────────────

        private static bool ComputeWarning(ref FiefInfo f, LedgerSettings cfg)
        {
            if (f.IsVillage)
                return f.IsRaidedOrLooted;

            return f.IsUnderSiege
                || f.IsDanger
                || f.IsRaidedOrLooted
                || (f.Loyalty  > 0f && f.Loyalty  < cfg.LowLoyalty)
                || (f.Security > 0f && f.Security < cfg.LowSecurity)
                || f.FoodChange < cfg.LowFood
                || f.GarrisonCount < cfg.LowGarrison;
        }

        private static int ComputeRisk(ref FiefInfo f, LedgerSettings cfg)
        {
            if (f.IsVillage)    return f.IsRaidedOrLooted ? 60 : 0;
            if (f.IsUnderSiege) return 100;
            if (f.IsDanger)     return 90;

            float score = 0f;

            // Loyalty: 0–35 pts — how far below (threshold+20) the settlement sits
            if (f.Loyalty > 0f)
            {
                float ceil = cfg.LowLoyalty + 20f;
                score += Math.Max(0f, (ceil - f.Loyalty) / ceil * 35f);
            }

            // Security: 0–20 pts
            if (f.Security > 0f)
            {
                float ceil = cfg.LowSecurity + 20f;
                score += Math.Max(0f, (ceil - f.Security) / ceil * 20f);
            }

            // Food deficit: 0–25 pts
            if (f.FoodChange < 0f)
                score += Math.Min(25f, (-f.FoodChange / 8f) * 25f);

            // Weak garrison: 0–20 pts — scales from 2× threshold down to 1
            if (f.GarrisonCount < cfg.LowGarrison * 2)
            {
                float ceil = cfg.LowGarrison * 2f;
                score += Math.Max(0f, (ceil - f.GarrisonCount) / ceil * 20f);
            }

            return Math.Min(100, (int)score);
        }

        private static void ApplyTriage(ref FiefInfo f, LedgerSettings cfg)
        {
            f.TrendLabel = BuildTrendLabel(f.LoyaltyDelta);
            f.ReinforcementNeed = f.IsVillage ? 0 : Math.Max(0, cfg.LowGarrison - f.GarrisonCount);
            f.PrimaryIssue = BuildPrimaryIssue(ref f, cfg);
            f.ActionHint = BuildActionHint(ref f, cfg);
            f.GovernanceScore = ComputeGovernanceScore(ref f);
        }

        private static int ComputeGovernanceScore(ref FiefInfo f)
        {
            if (f.IsVillage)
                return f.IsRaidedOrLooted ? 25 : 80;

            int score = 100 - f.RiskScore;
            if (f.IsUnderSiege) score = 0;
            else if (f.IsDanger) score = Math.Min(score, 20);

            if (!float.IsNaN(f.LoyaltyDelta))
            {
                if (f.LoyaltyDelta <= -2f) score -= 8;
                else if (f.LoyaltyDelta >= 2f) score += 4;
            }

            if (!string.IsNullOrEmpty(f.BuildingInProgress))
                score += 2;

            if (score < 0) return 0;
            if (score > 100) return 100;
            return score;
        }

        private static string BuildPrimaryIssue(ref FiefInfo f, LedgerSettings cfg)
        {
            if (f.IsUnderSiege) return "Under siege";
            if (f.IsDanger) return "Enemy army nearby";
            if (f.IsRaidedOrLooted) return f.IsVillage ? "Village raided" : "Raided";
            if (f.IsVillage) return "Stable village";
            if (f.Loyalty > 0f && f.Loyalty < cfg.LowLoyalty) return "Low loyalty";
            if (f.FoodChange < cfg.LowFood) return "Food shortage";
            if (f.Security > 0f && f.Security < cfg.LowSecurity) return "Low security";
            if (f.GarrisonCount < cfg.LowGarrison) return "Weak garrison";
            if (!float.IsNaN(f.LoyaltyDelta) && f.LoyaltyDelta <= -2f) return "Loyalty falling";
            return "Stable";
        }

        private static string BuildActionHint(ref FiefInfo f, LedgerSettings cfg)
        {
            if (f.IsUnderSiege) return "Break the siege";
            if (f.IsDanger) return "Reinforce and intercept";
            if (f.IsRaidedOrLooted) return f.IsVillage ? "Clear raiders and recover hearths" : "Secure the countryside";
            if (f.IsVillage) return "Protect bound routes";
            if (f.Loyalty > 0f && f.Loyalty < cfg.LowLoyalty) return "Stabilize loyalty";
            if (f.FoodChange < cfg.LowFood) return "Bring food or reduce drain";
            if (f.Security > 0f && f.Security < cfg.LowSecurity) return "Hunt threats and assign troops";
            if (f.GarrisonCount < cfg.LowGarrison) return "Add " + Math.Max(0, cfg.LowGarrison - f.GarrisonCount) + " troops";
            if (!float.IsNaN(f.LoyaltyDelta) && f.LoyaltyDelta <= -2f) return "Watch loyalty trend";
            if (string.IsNullOrEmpty(f.GovernorName) || f.GovernorName == "\u2014") return "Consider a governor";
            return "Hold course";
        }

        private static string BuildTrendLabel(float delta)
        {
            if (float.IsNaN(delta)) return "trend new";
            if (delta >= 2f) return "trend rising";
            if (delta <= -2f) return "trend falling";
            return "trend steady";
        }

        // ── Ownership (called by CampaignBehavior to filter Settlement.All) ──

        public static bool IsPlayerClanOwned(Settlement s)
        {
            if (s == null) return false;
            try
            {
                if (Hero.MainHero?.Clan == null) return false;
                if (s.IsVillage)
                    return s.Village?.Bound?.OwnerClan == Hero.MainHero.Clan;
                return s.OwnerClan == Hero.MainHero.Clan;
            }
            catch { return false; }
        }

        public static bool IsKingdomOwned(Settlement s)
        {
            if (s == null) return false;
            try
            {
                var kingdom = Clan.PlayerClan?.Kingdom;
                if (kingdom == null) return false;
                var owner = s.IsVillage ? s.Village?.Bound?.OwnerClan : s.OwnerClan;
                return owner?.Kingdom == kingdom;
            }
            catch { return false; }
        }

        // ── Helpers ───────────────────────────────────────────────────────

        private static string GetCultureId(Settlement s)
        {
            try   { return s.Culture?.StringId?.ToLowerInvariant() ?? string.Empty; }
            catch { return string.Empty; }
        }

        private static string MakeTypeLabel(string cultureId, string typeNoun)
        {
            if (string.IsNullOrEmpty(cultureId)) return typeNoun;
            return _adjectives.TryGetValue(cultureId, out var adj)
                ? $"{adj} {typeNoun}"
                : typeNoun;
        }

        private static bool IsVassal(Settlement s)
        {
            try
            {
                var owner = s.IsVillage
                    ? s.Village?.Bound?.OwnerClan
                    : s.OwnerClan;
                return owner != null
                    && owner != Clan.PlayerClan
                    && owner.Kingdom == Clan.PlayerClan?.Kingdom;
            }
            catch { return false; }
        }
    }
}
