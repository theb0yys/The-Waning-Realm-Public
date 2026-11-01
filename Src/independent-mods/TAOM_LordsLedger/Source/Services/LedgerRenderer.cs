using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using TaleWorlds.CampaignSystem.Settlements;

namespace TAOM_LordsLedger
{
    /// <summary>
    /// Builds the full Ledger report string from Settlement.All.
    /// Owns the reusable buffers so no allocations occur on repeated refreshes.
    /// All Settlement iteration and DataReader calls happen here.
    /// </summary>
    public sealed class LedgerRenderer
    {
        // ── Reusable buffers ──────────────────────────────────────────────
        private readonly StringBuilder  _sb       = new StringBuilder(8192);
        private readonly List<FiefInfo> _warned   = new List<FiefInfo>(16);
        private readonly List<FiefInfo> _clean    = new List<FiefInfo>(64);
        private readonly List<string>   _warnMsgs = new List<string>(6);

        // ── Entry point ───────────────────────────────────────────────────

        public string Build(LedgerSettings s, Dictionary<string, float> loyaltyHistory)
        {
            _sb.Clear();
            _warned.Clear();
            _clean.Clear();

            // ── Gather ───────────────────────────────────────────────────
            foreach (var settlement in Settlement.All)
            {
                if (settlement == null) continue;
                if (!PassOwnership(settlement, s)) continue;
                if (!PassTypeFilter(settlement, s)) continue;

                var info = LedgerDataReader.Read(settlement, s, loyaltyHistory);

                if (s.WarningsOnly && !info.HasWarning) continue;

                if (info.HasWarning)
                    _warned.Add(info);
                else
                    _clean.Add(info);
            }

            // ── Sort each group ───────────────────────────────────────────
            SortGroup(_warned, s);
            SortGroup(_clean,  s);

            // ── Header ────────────────────────────────────────────────────
            _sb.AppendLine("=== Lord's Ledger ===");
            _sb.AppendLine(
                $"Sort: {s.SortLabel()}  |  " +
                $"Filter: {s.FilterLabel()}  |  " +
                $"Thresholds: {s.PresetLabel()}  |  " +
                $"Warnings: {s.WarningsLabel()}  |  " +
                $"View: {s.ViewLabel()}");

            // ── Aggregate summary ─────────────────────────────────────────
            int total = _warned.Count + _clean.Count;
            if (total > 0)
            {
                int garrison = 0, militia = 0;
                float prosperity = 0f;
                float loyalty = 0f, security = 0f;
                int governed = 0;
                int sieges = 0, raids = 0, danger = 0;
                int readinessTotal = 0, critical = 0, reinforceNeed = 0;
                int rising = 0, falling = 0;
                FiefInfo worst = default(FiefInfo);
                bool hasWorst = false;

                var all = _warned;
                for (int pass = 0; pass < 2; pass++, all = _clean)
                {
                    for (int i = 0; i < all.Count; i++)
                    {
                        var f = all[i];
                        garrison    += f.GarrisonCount;
                        militia     += f.MilitiaCount;
                        prosperity  += f.Prosperity;
                        if (f.IsUnderSiege)      sieges++;
                        if (f.IsRaidedOrLooted)  raids++;
                        if (f.IsDanger)          danger++;
                        readinessTotal += f.GovernanceScore;
                        if (f.GovernanceScore < 50) critical++;
                        reinforceNeed += f.ReinforcementNeed;
                        if (f.TrendLabel == "trend rising") rising++;
                        else if (f.TrendLabel == "trend falling") falling++;
                        if (!f.IsVillage)
                        {
                            loyalty += f.Loyalty;
                            security += f.Security;
                            governed++;
                        }
                        if (!hasWorst || f.RiskScore > worst.RiskScore)
                        {
                            worst = f;
                            hasWorst = true;
                        }
                    }
                }

                _sb.AppendLine();
                _sb.AppendLine(
                    $"Fiefs: {total}  |  " +
                    $"Garrison: {garrison}  |  " +
                    $"Militia: {militia}  |  " +
                    $"Prosperity: {prosperity:0}");

                if (governed > 0 && hasWorst)
                {
                    _sb.AppendLine(
                        $"Governance: Loyalty avg {loyalty / governed:0}  |  " +
                        $"Security avg {security / governed:0}  |  " +
                        $"Highest risk: {worst.Name} ({worst.RiskScore}{RiskBadge(worst.RiskScore)})");
                    _sb.AppendLine(
                        $"Readiness: avg {readinessTotal / total}/100  |  " +
                        $"Critical: {critical}  |  " +
                        $"Reinforcements needed: {reinforceNeed}  |  " +
                        $"Trends: +{rising} / -{falling}");
                    _sb.AppendLine(
                        $"Priority: {worst.Name} - {worst.PrimaryIssue} -> {worst.ActionHint}");
                }

                // Warning summary line — show total first, then breakdown as context.
                // Previously "2 siege | 4 with warnings" double-counted the sieges.
                // Now: "4 need attention  (2 siege  |  1 raided  |  1 in danger)"
                if (_warned.Count > 0)
                {
                    _warnMsgs.Clear();
                    if (sieges > 0)  _warnMsgs.Add($"{sieges} siege");
                    if (raids > 0)   _warnMsgs.Add($"{raids} raided");
                    if (danger > 0)  _warnMsgs.Add($"{danger} in danger");

                    string breakdown = _warnMsgs.Count > 0
                        ? $"  ({string.Join("  |  ", _warnMsgs)})"
                        : string.Empty;
                    _sb.AppendLine($"  ⚠  {_warned.Count} need attention{breakdown}");
                }
            }

            _sb.AppendLine("────────────────────────────────────────");

            // ── No results ───────────────────────────────────────────────
            if (total == 0)
            {
                _sb.AppendLine(s.WarningsOnly
                    ? "No warnings — all fiefs are in good order."
                    : "You hold no fiefs matching this filter.");
                return _sb.ToString();
            }

            // ── Warned fiefs (pinned first) ───────────────────────────────
            if (_warned.Count > 0)
            {
                if (!s.WarningsOnly)
                    _sb.AppendLine("[ ⚠  Needs Attention ]");
                for (int i = 0; i < _warned.Count; i++)
                    AppendFief(_warned[i], s.CompactView, s);
            }

            // ── Clean fiefs ───────────────────────────────────────────────
            if (_clean.Count > 0)
            {
                if (_warned.Count > 0)
                    _sb.AppendLine("[ All Clear ]");
                for (int i = 0; i < _clean.Count; i++)
                    AppendFief(_clean[i], s.CompactView, s);
            }

            return _sb.ToString();
        }

        public LedgerBoardSnapshot BuildSnapshot(LedgerSettings s, Dictionary<string, float> loyaltyHistory)
        {
            _warned.Clear();
            _clean.Clear();

            foreach (var settlement in Settlement.All)
            {
                if (settlement == null) continue;
                if (!PassOwnership(settlement, s)) continue;
                if (!PassTypeFilter(settlement, s)) continue;

                var info = LedgerDataReader.Read(settlement, s, loyaltyHistory);

                if (s.WarningsOnly && !info.HasWarning) continue;

                if (info.HasWarning)
                    _warned.Add(info);
                else
                    _clean.Add(info);
            }

            SortGroup(_warned, s);
            SortGroup(_clean, s);

            var snapshot = new LedgerBoardSnapshot
            {
                TitleText = "Lord's Ledger",
                SignalText = "Governance board for fief risk, readiness, and reinforcement needs.",
                ScopeText = "Scope: " + s.ScopeLabel() + " | Filter: " + s.FilterLabel() + " | Sort: " + s.SortLabel(),
                MemoryText = "Thresholds: " + s.PresetLabel() + " | Warnings: " + s.WarningsLabel() + " | View: " + s.ViewLabel(),
                SortButtonText = "Sort: " + s.SortLabel(),
                FilterButtonText = "Filter: " + s.FilterLabel(),
                WarningsButtonText = "Warnings: " + s.WarningsLabel(),
                PresetButtonText = "Thresholds: " + s.PresetLabel(),
                ViewButtonText = "View: " + s.ViewLabel(),
                ScopeButtonText = "Scope: " + s.ScopeLabel()
            };

            int total = _warned.Count + _clean.Count;
            if (total == 0)
            {
                snapshot.VisibleText = "Visible fiefs: 0";
                snapshot.EmptyText = s.WarningsOnly
                    ? "No warnings. Your fiefs are currently in good order."
                    : "No fiefs match the current scope and filter.";
                snapshot.SummaryLineOne = "0 fiefs tracked";
                snapshot.SummaryLineTwo = "No active governance records";
                snapshot.SummaryLineThree = "Adjust scope or acquire fiefs to populate the board.";
                snapshot.PriorityText = "No priority target.";
                return snapshot;
            }

            int garrison = 0;
            int militia = 0;
            int sieges = 0;
            int raids = 0;
            int danger = 0;
            int critical = 0;
            int reinforceNeed = 0;
            int readinessTotal = 0;
            int towns = 0;
            int castles = 0;
            int villages = 0;
            FiefInfo worst = default(FiefInfo);
            bool hasWorst = false;

            var all = _warned;
            for (int pass = 0; pass < 2; pass++, all = _clean)
            {
                for (int i = 0; i < all.Count; i++)
                {
                    var f = all[i];
                    if (f.IsVillage) villages++;
                    else if (f.TypeLabel.IndexOf("Castle", StringComparison.OrdinalIgnoreCase) >= 0 ||
                             f.TypeLabel.IndexOf("Stronghold", StringComparison.OrdinalIgnoreCase) >= 0 ||
                             f.TypeLabel.IndexOf("Fortress", StringComparison.OrdinalIgnoreCase) >= 0 ||
                             f.TypeLabel.IndexOf("Citadel", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        castles++;
                    }
                    else
                    {
                        towns++;
                    }

                    garrison += f.GarrisonCount;
                    militia += f.MilitiaCount;
                    readinessTotal += f.GovernanceScore;
                    reinforceNeed += f.ReinforcementNeed;
                    if (f.IsUnderSiege) sieges++;
                    if (f.IsRaidedOrLooted) raids++;
                    if (f.IsDanger) danger++;
                    if (f.GovernanceScore < 50 || f.RiskScore >= 76) critical++;

                    if (!hasWorst || f.RiskScore > worst.RiskScore)
                    {
                        worst = f;
                        hasWorst = true;
                    }
                }
            }

            int readiness = readinessTotal / total;
            int watch = Math.Max(0, _warned.Count - critical);
            int stable = Math.Max(0, total - _warned.Count);
            snapshot.VisibleText = "Visible fiefs: " + total;
            snapshot.SummaryLineOne = total + " fiefs | " + _warned.Count + " warnings | readiness " + readiness + "/100";
            snapshot.SummaryLineTwo = "Towns " + towns + " | Castles " + castles + " | Villages " + villages;
            snapshot.SummaryLineThree = "Sieges " + sieges + " | Raids " + raids + " | Field danger " + danger + " | Critical " + critical;
            snapshot.PriorityText = hasWorst
                ? "Priority: " + worst.Name + " - " + worst.PrimaryIssue + " -> " + worst.ActionHint
                : "No priority target.";
            snapshot.CriticalBandText = "Critical " + critical;
            snapshot.WatchBandText = "Watch " + watch;
            snapshot.StableBandText = "Stable " + stable;
            snapshot.ReinforceBandText = "Need " + reinforceNeed;
            snapshot.FooterText = "Garrison " + garrison + " | Militia " + militia + " | Reinforcement need " + reinforceNeed;

            for (int i = 0; i < _warned.Count; i++)
                snapshot.Fiefs.Add(CreateFiefSnapshot(_warned[i]));
            for (int i = 0; i < _clean.Count; i++)
                snapshot.Fiefs.Add(CreateFiefSnapshot(_clean[i]));

            return snapshot;
        }

        // ── Fief rendering ────────────────────────────────────────────────

        private void AppendFief(FiefInfo f, bool compact, LedgerSettings cfg)
        {
            string status = f.IsUnderSiege     ? " [SIEGE]"
                          : f.IsDanger         ? " [DANGER]"
                          : f.IsRaidedOrLooted ? " [RAIDED]"
                          : string.Empty;

            string vassal = f.IsVassal ? " (vassal)" : string.Empty;
            string risk   = RiskBadge(f.RiskScore);

            if (compact)
            {
                // ── Compact: one line per fief ────────────────────────────
                if (f.IsVillage)
                {
                    _sb.AppendLine(
                        $"{f.Name}  ({f.TypeLabel}){status}{vassal}" +
                        $"  Hearth:{f.Prosperity:0}" +
                        $"  Score:{f.GovernanceScore}" +
                        $"  Action:{f.ActionHint}");
                }
                else
                {
                    string delta = DeltaStr(f.LoyaltyDelta);
                    string warn  = f.HasWarning ? " ⚠" : string.Empty;
                    _sb.AppendLine(
                        $"{f.Name}  ({f.TypeLabel}){status}{vassal}{warn}" +
                        $"  Loy:{f.Loyalty:0}{delta}" +
                        $"  Sec:{f.Security:0}" +
                        $"  Gar:{f.GarrisonCount}" +
                        $"  Food:{f.FoodChange:+0.#;-0.#;0}" +
                        $"  Risk:{f.RiskScore}{risk}" +
                        $"  Score:{f.GovernanceScore}" +
                        $"  Action:{f.ActionHint}");
                }
            }
            else
            {
                // ── Detailed: multi-line ──────────────────────────────────
                _sb.AppendLine($"{f.Name}  ({f.TypeLabel}){status}{vassal}");

                if (f.IsVillage)
                {
                    _sb.AppendLine($"    Hearth: {f.Prosperity:0}");
                    _sb.AppendLine($"    Counsel: {f.PrimaryIssue}  |  Action: {f.ActionHint}  |  Readiness: {f.GovernanceScore}/100");
                }
                else
                {
                    // Line 2: loyalty, security, prosperity, risk
                    _sb.Append($"    Loyalty: {f.Loyalty:0}{DeltaStr(f.LoyaltyDelta)}");
                    if (f.Loyalty > 0f && f.Loyalty < cfg.LowLoyalty)   _sb.Append(" ⚠");
                    _sb.Append($"  |  Security: {f.Security:0}");
                    if (f.Security > 0f && f.Security < cfg.LowSecurity) _sb.Append(" ⚠");
                    _sb.Append($"  |  Prosperity: {f.Prosperity:0}");
                    _sb.AppendLine($"  |  Risk: {f.RiskScore}{risk}");

                    // Line 3: military + food
                    _sb.Append($"    Garrison: {f.GarrisonCount}");
                    if (f.GarrisonCount > 0 && f.GarrisonCount < cfg.LowGarrison) _sb.Append(" ⚠");
                    _sb.Append($"  |  Militia: {f.MilitiaCount}");
                    _sb.Append($"  |  Food: {f.FoodChange:+0.#;-0.#;0}");
                    if (f.FoodChange < cfg.LowFood) _sb.Append(" ⚠");
                    _sb.AppendLine();

                    _sb.AppendLine($"    Counsel: {f.PrimaryIssue}  |  Action: {f.ActionHint}  |  Readiness: {f.GovernanceScore}/100  |  {f.TrendLabel}");

                    // Line 4: governance + extras
                    _sb.Append($"    Governor: {f.GovernorName}");
                    if (f.WallLevel > 0)
                        _sb.Append($"  |  Walls: {f.WallLevel}");
                    if (f.BoundVillageCount > 0)
                        _sb.Append($"  |  Villages: {f.BoundVillageCount}");
                    if (!string.IsNullOrEmpty(f.BuildingInProgress))
                        _sb.Append($"  |  Building: {f.BuildingInProgress}");
                    _sb.AppendLine();

                    // Line 5: prisoners (only if any)
                    if (f.NotablePrisoners != null && f.NotablePrisoners.Count > 0)
                        _sb.AppendLine($"    Prisoners: {string.Join(", ", f.NotablePrisoners)}");
                }

                _sb.AppendLine();
            }
        }

        private static LedgerFiefSnapshot CreateFiefSnapshot(FiefInfo f)
        {
            string status = f.IsUnderSiege ? "SIEGE"
                : f.IsDanger ? "DANGER"
                : f.IsRaidedOrLooted ? "RAIDED"
                : f.HasWarning ? "WATCH"
                : "STABLE";

            string metrics = f.IsVillage
                ? "Hearth " + f.Prosperity.ToString("0")
                : "Loyalty " + f.Loyalty.ToString("0") + DeltaStr(f.LoyaltyDelta) +
                  " | Security " + f.Security.ToString("0") +
                  " | Prosperity " + f.Prosperity.ToString("0") +
                  " | Food " + f.FoodChange.ToString("+0.#;-0.#;0");

            string military = f.IsVillage
                ? "Protect bound routes"
                : "Garrison " + f.GarrisonCount +
                  " | Militia " + f.MilitiaCount +
                  (f.WallLevel > 0 ? " | Walls " + f.WallLevel : string.Empty) +
                  (f.BoundVillageCount > 0 ? " | Villages " + f.BoundVillageCount : string.Empty);

            string construction = string.IsNullOrEmpty(f.BuildingInProgress)
                ? string.Empty
                : "Building: " + f.BuildingInProgress;

            string prisoners = f.NotablePrisoners != null && f.NotablePrisoners.Count > 0
                ? "Prisoners: " + string.Join(", ", f.NotablePrisoners)
                : string.Empty;

            return new LedgerFiefSnapshot
            {
                Name = f.Name,
                TypeText = f.TypeLabel + (f.IsVassal ? " | vassal" : string.Empty),
                StatusText = status,
                RiskText = "Risk " + f.RiskScore + RiskBadge(f.RiskScore),
                ReadinessText = f.GovernanceScore + "/100",
                MetricsLine = metrics,
                MilitaryLine = military,
                ActionLine = "Issue: " + f.PrimaryIssue + " | Action: " + f.ActionHint,
                GovernorLine = f.IsVillage ? string.Empty : "Governor: " + f.GovernorName + " | " + f.TrendLabel,
                ConstructionLine = construction,
                PrisonersLine = prisoners,
                ShowGovernor = !f.IsVillage,
                ShowConstruction = !string.IsNullOrEmpty(construction),
                ShowPrisoners = !string.IsNullOrEmpty(prisoners),
                IsCritical = f.IsUnderSiege || f.IsDanger || f.RiskScore >= 76 || f.GovernanceScore < 35,
                IsWarning = f.HasWarning && !(f.IsUnderSiege || f.IsDanger || f.RiskScore >= 76 || f.GovernanceScore < 35),
                IsStable = !f.HasWarning,
                IsVillage = f.IsVillage
            };
        }

        // ── Sorting ───────────────────────────────────────────────────────

        private static void SortGroup(List<FiefInfo> group, LedgerSettings s)
        {
            switch (s.Sort)
            {
                case SortMode.ByLoyalty:
                    group.Sort((a, b) =>
                    {
                        int c = a.Loyalty.CompareTo(b.Loyalty);
                        return c != 0 ? c : string.Compare(a.Name, b.Name, StringComparison.Ordinal);
                    });
                    break;
                case SortMode.ByProsperity:
                    group.Sort((a, b) =>
                    {
                        int c = b.Prosperity.CompareTo(a.Prosperity);
                        return c != 0 ? c : string.Compare(a.Name, b.Name, StringComparison.Ordinal);
                    });
                    break;
                case SortMode.ByFood:
                    group.Sort((a, b) =>
                    {
                        int c = a.FoodChange.CompareTo(b.FoodChange);
                        return c != 0 ? c : string.Compare(a.Name, b.Name, StringComparison.Ordinal);
                    });
                    break;
                case SortMode.ByGarrison:
                    group.Sort((a, b) =>
                    {
                        int c = a.GarrisonCount.CompareTo(b.GarrisonCount);
                        return c != 0 ? c : string.Compare(a.Name, b.Name, StringComparison.Ordinal);
                    });
                    break;
                case SortMode.ByRisk:
                    group.Sort((a, b) =>
                    {
                        int c = b.RiskScore.CompareTo(a.RiskScore);
                        return c != 0 ? c : string.Compare(a.Name, b.Name, StringComparison.Ordinal);
                    });
                    break;
                default: // ByName
                    group.Sort((a, b) =>
                        string.Compare(a.Name, b.Name, StringComparison.Ordinal));
                    break;
            }
        }

        // ── Filters ───────────────────────────────────────────────────────

        private static bool PassOwnership(Settlement s, LedgerSettings cfg)
        {
            return cfg.IncludeVassals
                ? LedgerDataReader.IsKingdomOwned(s)
                : LedgerDataReader.IsPlayerClanOwned(s);
        }

        private static bool PassTypeFilter(Settlement s, LedgerSettings cfg)
        {
            switch (cfg.Filter)
            {
                case FiefFilter.Towns:    return s.IsTown;
                case FiefFilter.Castles:  return s.IsCastle;
                case FiefFilter.Villages: return s.IsVillage;
                default:                  return s.IsTown || s.IsCastle || s.IsVillage;
            }
        }

        // ── Export to file ────────────────────────────────────────────────

        /// <summary>
        /// Writes the current report to a .txt file in the player's Bannerlord
        /// documents folder. Returns a status message to show in the game feed.
        /// </summary>
        public string ExportToFile(LedgerSettings s, Dictionary<string, float> loyaltyHistory)
        {
            try
            {
                string report = Build(s, loyaltyHistory);

                string dir  = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    "Mount and Blade II Bannerlord");
                Directory.CreateDirectory(dir);

                string path = Path.Combine(dir, "LordsLedger_export.txt");
                File.WriteAllText(path, report, Encoding.UTF8);
                return $"[Ledger] Exported to: {path}";
            }
            catch (Exception ex)
            {
                return $"[Ledger] Export failed: {ex.Message}";
            }
        }

        // ── Formatting helpers ────────────────────────────────────────────

        private static string DeltaStr(float delta)
        {
            if (float.IsNaN(delta) || Math.Abs(delta) < 0.1f) return string.Empty;
            return delta > 0f
                ? $" (+{delta:0.#})"
                : $" ({delta:0.#})";
        }

        private static string RiskBadge(int score)
        {
            if (score >= 76) return "!!!";
            if (score >= 51) return "!!";
            if (score >= 26) return "!";
            return string.Empty;
        }
    }
}
