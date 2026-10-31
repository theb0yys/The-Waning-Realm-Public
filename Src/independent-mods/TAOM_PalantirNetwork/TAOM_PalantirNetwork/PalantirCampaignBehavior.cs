using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TAOM_PalantirNetwork.UI;

namespace TAOM_PalantirNetwork
{
    public sealed class PalantirCampaignBehavior : CampaignBehaviorBase
    {
        // ── Menu / option IDs ────────────────────────────────────────────────
        private const string MENU_ID             = "taom_palantir_menu";
        private const string OPT_TOGGLE_HOSTILES = "taom_palantir_toggle_hostiles";
        private const string OPT_CYCLE_CATEGORY  = "taom_palantir_cycle_category";
        private const string OPT_CYCLE_SORT      = "taom_palantir_cycle_sort";
        private const string OPT_TOGGLE_HISTORY  = "taom_palantir_toggle_history";
        private const string OPT_REFRESH         = "taom_palantir_refresh";
        private const string OPT_CLOSE           = "taom_palantir_close";

        private const string SAVE_KEY_SIGHTINGS     = "taom_palantir_sightings";
        private const string SAVE_KEY_HOSTILES_ONLY = "taom_palantir_hostiles_only";
        private const string SAVE_KEY_HISTORY_DETAIL = "taom_palantir_history_detail";
        private const string SAVE_KEY_CATEGORY      = "taom_palantir_category";
        private const string SAVE_KEY_SORT          = "taom_palantir_sort";
        private const string SAVE_KEY_LAST_PULSE    = "taom_palantir_last_pulse_day";

        private static readonly string[] StandardRootMenus =
        {
            "town",
            "castle",
            "village",
            "camp",
            "encounter"
        };

        // ── Save-game state ──────────────────────────────────────────────────
        private readonly Dictionary<string, Sighting> _sightingsByPartyId
            = new Dictionary<string, Sighting>();
        private List<KeyValuePair<string, Sighting>> _serializedSightings;

        private bool _hostilesOnly;
        private bool _hostilesOnlyInitialized;
        private bool _showHistoryDetails = true;
        private bool _showHistoryDetailsInitialized;
        private double _lastThreatPulseDay = -999.0;

        private PartyCategory _category = PartyCategory.All;
        private SortMode      _sortMode = SortMode.MostRecent;

        // ── Reusable allocations (perf) ──────────────────────────────────────
        private readonly StringBuilder _reportBuilder = new StringBuilder(2048);
        private readonly List<string>  _staleKeys     = new List<string>(32);
        private readonly List<Sighting> _filteredCache = new List<Sighting>(64);

        // ── Enums ────────────────────────────────────────────────────────────
        public enum PartyCategory
        {
            All      = 0,
            Armies   = 1,
            Caravans = 2,
            Bandits  = 3,
            Other    = 4,
            Threats  = 5
        }

        public enum SortMode
        {
            MostRecent    = 0,
            Nearest       = 1,
            Strongest     = 2,
            MostDangerous = 3,
            MostWatched   = 4,
            PeakDanger    = 5
        }

        public enum PartyKind
        {
            Other   = 0,
            Caravan = 1,
            Army    = 2,
            Bandit  = 3
        }

        // ── Sighting data ────────────────────────────────────────────────────
        public struct Sighting
        {
            public CampaignTime SeenAt;
            public float X;
            public float Y;
            public string PartyName;
            public string FactionName;
            public float Strength;
            public int TroopCount;
            public int Kind;           // PartyKind as int for serialization
            public bool IsHostileAtTime;
            public bool WasHostilePreviously;
            public int SeenCount;
            public double FirstSeenDay;
            public double PreviousSeenDay;
            public float PeakStrength;
            public int PeakTroopCount;
            public int LastDangerScore;
            public int PeakDangerScore;
            public float LastDistance;
        }

        // ── Helpers ──────────────────────────────────────────────────────────
        private static PalantirMcmSettings Cfg => PalantirMcmSettings.Instance;

        private bool HostilesOnly
        {
            get
            {
                if (!_hostilesOnlyInitialized)
                {
                    _hostilesOnly = Cfg?.HostilesOnly ?? true;
                    _hostilesOnlyInitialized = true;
                }
                return _hostilesOnly;
            }
            set
            {
                _hostilesOnly = value;
                _hostilesOnlyInitialized = true;
            }
        }

        private int  IntelDays   => Cfg?.IntelDays   ?? 5;
        private int  MaxResults  => Cfg?.MaxResults  ?? 25;
        private bool ModEnabled  => Cfg?.Enabled     ?? true;

        private bool ShowHistoryDetails
        {
            get
            {
                if (!_showHistoryDetailsInitialized)
                {
                    _showHistoryDetails = Cfg?.ShowHistoryDetails ?? true;
                    _showHistoryDetailsInitialized = true;
                }
                return _showHistoryDetails;
            }
            set
            {
                _showHistoryDetails = value;
                _showHistoryDetailsInitialized = true;
            }
        }

        private InputKey Hotkey
        {
            get
            {
                if (Cfg == null) return InputKey.P;
                // Parse the MCM string to an InputKey; fall back to P.
                if (Enum.TryParse(Cfg.HotkeyName, true, out InputKey key))
                    return key;
                return InputKey.P;
            }
        }

        // ── CampaignBehaviorBase ─────────────────────────────────────────────
        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
            CampaignEvents.PartyVisibilityChangedEvent.AddNonSerializedListener(this, OnPartyVisibilityChanged);
            CampaignEvents.TickEvent.AddNonSerializedListener(this, OnTick);
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
        }

        public override void SyncData(IDataStore dataStore)
        {
            try
            {
                dataStore.SyncData(SAVE_KEY_SIGHTINGS,     ref _serializedSightings);
                dataStore.SyncData(SAVE_KEY_HOSTILES_ONLY, ref _hostilesOnly);
                dataStore.SyncData(SAVE_KEY_HISTORY_DETAIL, ref _showHistoryDetails);
                dataStore.SyncData(SAVE_KEY_LAST_PULSE,    ref _lastThreatPulseDay);

                int cat  = (int)_category;
                int sort = (int)_sortMode;
                dataStore.SyncData(SAVE_KEY_CATEGORY, ref cat);
                dataStore.SyncData(SAVE_KEY_SORT,     ref sort);

                if (dataStore.IsLoading)
                {
                    _hostilesOnlyInitialized = true;
                    _showHistoryDetailsInitialized = true;
                    _category = Enum.IsDefined(typeof(PartyCategory), cat)
                        ? (PartyCategory)cat : PartyCategory.All;
                    _sortMode = Enum.IsDefined(typeof(SortMode), sort)
                        ? (SortMode)sort : SortMode.MostRecent;

                    _sightingsByPartyId.Clear();
                    if (_serializedSightings != null)
                        foreach (var kv in _serializedSightings)
                            if (!string.IsNullOrEmpty(kv.Key))
                                _sightingsByPartyId[kv.Key] = NormalizeLoadedSighting(kv.Value);
                }
                else if (dataStore.IsSaving)
                {
                    _serializedSightings = _sightingsByPartyId.ToList();
                }
            }
            catch (Exception ex)
            {
                LogDebug($"SyncData error: {ex.Message}");
            }
        }

        // ── Session setup ────────────────────────────────────────────────────
        private void OnSessionLaunched(CampaignGameStarter starter)
        {
            if (!ModEnabled) return;

            try
            {
                AddMenuAndOptions(starter);
                AddRootMenuAccess(starter);
            }
            catch (Exception ex)
            {
                LogDebug($"OnSessionLaunched error: {ex.Message}");
            }
        }

        private void AddMenuAndOptions(CampaignGameStarter starter)
        {
            starter.AddGameMenu(
                MENU_ID,
                "{=TAOM_PAL_MENU}{PAL_REPORT}",
                args => RefreshReport(),
                GameMenu.MenuOverlayType.None,
                GameMenu.MenuFlags.None,
                null
            );

            // Option 0 – toggle hostile filter
            starter.AddGameMenuOption(
                MENU_ID, OPT_TOGGLE_HOSTILES,
                "{=TAOM_PAL_TOGGLE_HOSTILES}Hostiles: {PAL_HOSTILES_STATE}",
                args => { UpdateMenuTextVariables(); args.optionLeaveType = GameMenuOption.LeaveType.DefendAction; return true; },
                args => ToggleHostileFilter(),
                isLeave: false, index: 0
            );

            // Option 1 – cycle party category
            starter.AddGameMenuOption(
                MENU_ID, OPT_CYCLE_CATEGORY,
                "{=TAOM_PAL_CYCLE_CATEGORY}Category: {PAL_CATEGORY_STATE}",
                args => { UpdateMenuTextVariables(); args.optionLeaveType = GameMenuOption.LeaveType.Trade; return true; },
                args => CycleCategory(),
                isLeave: false, index: 1
            );

            // Option 2 – cycle sort mode
            starter.AddGameMenuOption(
                MENU_ID, OPT_CYCLE_SORT,
                "{=TAOM_PAL_CYCLE_SORT}Sort: {PAL_SORT_STATE}",
                args => { UpdateMenuTextVariables(); args.optionLeaveType = GameMenuOption.LeaveType.Submenu; return true; },
                args => CycleSortMode(),
                isLeave: false, index: 2
            );

            // Option 3 – refresh / re-prune
            starter.AddGameMenuOption(
                MENU_ID, OPT_TOGGLE_HISTORY,
                "{=TAOM_PAL_TOGGLE_HISTORY}History: {PAL_HISTORY_STATE}",
                args => { UpdateMenuTextVariables(); args.optionLeaveType = GameMenuOption.LeaveType.Submenu; return true; },
                args => ToggleHistoryDetails(),
                isLeave: false, index: 3
            );

            // Option 4 – refresh / re-prune
            starter.AddGameMenuOption(
                MENU_ID, OPT_REFRESH,
                "{=TAOM_PAL_REFRESH}Refresh visions",
                args => { args.optionLeaveType = GameMenuOption.LeaveType.Ransom; return true; },
                args => RefreshIntel(),
                isLeave: false, index: 4
            );

            // Option 5 – close
            starter.AddGameMenuOption(
                MENU_ID, OPT_CLOSE,
                "{=TAOM_PAL_CLOSE}Close the Palantir",
                args => { args.optionLeaveType = GameMenuOption.LeaveType.Leave; return true; },
                args => GameMenu.ExitToLast(),
                isLeave: true, index: 5
            );
        }

        private void AddRootMenuAccess(CampaignGameStarter starter)
        {
            if (!(Cfg?.EnableRootMenuAccess ?? true))
                return;

            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string id in StandardRootMenus)
                TryAddRootMenuOption(starter, id, seen);

            string extra = Cfg?.AdditionalRootMenuIdsCsv;
            if (string.IsNullOrWhiteSpace(extra))
                return;

            string[] ids = extra.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < ids.Length; i++)
                TryAddRootMenuOption(starter, ids[i].Trim(), seen);
        }

        private void TryAddRootMenuOption(CampaignGameStarter starter, string menuId, HashSet<string> seen)
        {
            if (string.IsNullOrWhiteSpace(menuId) || !seen.Add(menuId))
                return;

            try
            {
                string optionId = "taom_palantir_open_" + SanitizeMenuId(menuId);
                starter.AddGameMenuOption(
                    menuId,
                    optionId,
                    "{=TAOM_PAL_ROOT_OPEN}Consult the Palantir",
                    args => { args.optionLeaveType = GameMenuOption.LeaveType.Submenu; return ModEnabled; },
                    args =>
                    {
                        OpenThreatBoardOrFallback();
                    },
                    isLeave: false,
                    index: 995);
            }
            catch (Exception ex)
            {
                if (Cfg?.DebugShowMenuIds == true)
                    LogDebug("Menu hook skipped for '" + menuId + "': " + ex.Message);
            }
        }

        // ── Per-tick hotkey handling ──────────────────────────────────────────
        private void OnTick(float dt)
        {
            if (!ModEnabled) return;

            var campaign = Campaign.Current;
            if (campaign == null || !campaign.GameStarted) return;
            if (campaign.CurrentMenuContext != null) return;

            try
            {
                if (!Input.IsKeyPressed(Hotkey)) return;
            }
            catch
            {
                // Guard against InputSystem issues with custom key bindings.
                return;
            }

            OpenThreatBoardOrFallback();
        }

        private void OnDailyTick()
        {
            if (!ModEnabled) return;
            if (!(Cfg?.ShowThreatPulse ?? true)) return;

            double today = Math.Floor(CampaignTime.Now.ToDays);
            int interval = Math.Max(1, Cfg?.ThreatPulseIntervalDays ?? 3);
            if (today < _lastThreatPulseDay + interval) return;

            CleanupExpiredSightings();
            ThreatSummary summary = BuildThreatSummary(GetPlayerPosition());
            int threshold = Math.Max(20, Cfg?.ThreatPulseThreshold ?? 55);
            if (summary.HighestDangerScore < threshold) return;

            _lastThreatPulseDay = today;
            InformationManager.DisplayMessage(new InformationMessage(
                "[Palantir Pulse] danger " + summary.HighestDangerScore + "/100" +
                " | " + summary.HostileCount + " hostile" +
                " | critical " + summary.CriticalContacts +
                " | focus " + summary.MostDangerousName +
                " | nearest " + summary.NearestHostileName + " (" + summary.NearestHostileDistance.ToString("0.0") + ")" +
                " | strongest " + summary.StrongestHostileName + " (~" + summary.StrongestHostileStrength.ToString("0") + ")" +
                " | " + summary.Advice,
                summary.HighestDangerScore >= 75 ? Colors.Red : Colors.Yellow));
        }

        // ── Sighting recording ───────────────────────────────────────────────
        private void OnPartyVisibilityChanged(PartyBase party)
        {
            if (!ModEnabled) return;
            if (party == null || !party.IsActive || !party.IsVisible || !party.IsMobile) return;

            var mp = party.MobileParty;
            if (mp == null || !mp.IsActive || mp.IsMainParty) return;

            var id = mp.StringId;
            if (string.IsNullOrEmpty(id)) return;

            try
            {
                Vec2 position = party.Position.ToVec2();
                Vec2 playerPos = GetPlayerPosition();
                double today = CampaignTime.Now.ToDays;
                Sighting previous;
                bool hasPrevious = _sightingsByPartyId.TryGetValue(id, out previous);
                int previousCount = hasPrevious && previous.SeenCount > 0 ? previous.SeenCount : (hasPrevious ? 1 : 0);
                float strength = mp.GetTotalLandStrengthWithFollowers();
                int troops = mp.MemberRoster?.TotalManCount ?? 0;

                var sighting = new Sighting
                {
                    SeenAt          = CampaignTime.Now,
                    X               = position.x,
                    Y               = position.y,
                    PartyName       = mp.Name?.ToString() ?? "Unknown party",
                    FactionName     = mp.MapFaction?.Name?.ToString() ?? "Unknown faction",
                    Strength        = strength,
                    TroopCount      = troops,
                    Kind            = (int)Classify(mp),
                    IsHostileAtTime = IsHostileToPlayer(mp),
                    WasHostilePreviously = hasPrevious && previous.IsHostileAtTime,
                    SeenCount = previousCount + 1,
                    FirstSeenDay = hasPrevious
                        ? (previous.FirstSeenDay > 0.0 ? previous.FirstSeenDay : previous.SeenAt.ToDays)
                        : today,
                    PreviousSeenDay = hasPrevious ? previous.SeenAt.ToDays : -1.0,
                    PeakStrength = hasPrevious ? Math.Max(previous.PeakStrength > 0f ? previous.PeakStrength : previous.Strength, strength) : strength,
                    PeakTroopCount = hasPrevious ? Math.Max(previous.PeakTroopCount > 0 ? previous.PeakTroopCount : previous.TroopCount, troops) : troops,
                    PeakDangerScore = hasPrevious ? previous.PeakDangerScore : 0,
                    LastDistance = 0f,
                    LastDangerScore = 0
                };

                sighting.LastDistance = Distance(playerPos, sighting);
                sighting.LastDangerScore = ThreatScore(sighting, playerPos);
                sighting.PeakDangerScore = Math.Max(sighting.PeakDangerScore, sighting.LastDangerScore);

                _sightingsByPartyId[id] = sighting;

                if (Cfg?.DebugShowSightings == true)
                    LogDebug($"Sighting: {sighting.PartyName} ({KindLabel(sighting.Kind)}) " +
                             $"at ({sighting.X:0.0}, {sighting.Y:0.0}), hostile={sighting.IsHostileAtTime}, danger={sighting.LastDangerScore}");
            }
            catch (Exception ex)
            {
                LogDebug($"OnPartyVisibilityChanged error for '{id}': {ex.Message}");
            }
        }

        // ── Pruning (optimized: no LINQ allocs) ─────────────────────────────
        private void CleanupExpiredSightings()
        {
            var cutoff = CampaignTime.Now - CampaignTime.Days(IntelDays);

            _staleKeys.Clear();
            foreach (var kv in _sightingsByPartyId)
                if (kv.Value.SeenAt < cutoff)
                    _staleKeys.Add(kv.Key);

            for (int i = 0; i < _staleKeys.Count; i++)
                _sightingsByPartyId.Remove(_staleKeys[i]);
        }

        // ── Report building ──────────────────────────────────────────────────
        private void RefreshReport()
        {
            UpdateMenuTextVariables();
            MBTextManager.SetTextVariable("PAL_REPORT", new TextObject(BuildReport()), false);
        }

        private void OpenThreatBoardOrFallback()
        {
            CleanupExpiredSightings();
            RefreshReport();

            if (!TryOpenThreatBoard())
                ActivateGameMenuSafe(MENU_ID);
        }

        public bool TryOpenThreatBoard()
        {
            try
            {
                return PalantirScreen.TryOpen(this);
            }
            catch (Exception ex)
            {
                LogDebug("Threat board failed: " + ex.Message);
                return false;
            }
        }

        public void ToggleHostileFilter()
        {
            HostilesOnly = !HostilesOnly;
            CleanupExpiredSightings();
            RefreshReport();
        }

        public void CycleCategory()
        {
            int count = Enum.GetValues(typeof(PartyCategory)).Length;
            _category = (PartyCategory)(((int)_category + 1) % count);
            CleanupExpiredSightings();
            RefreshReport();
        }

        public void CycleSortMode()
        {
            int count = Enum.GetValues(typeof(SortMode)).Length;
            _sortMode = (SortMode)(((int)_sortMode + 1) % count);
            RefreshReport();
        }

        public void ToggleHistoryDetails()
        {
            ShowHistoryDetails = !ShowHistoryDetails;
            RefreshReport();
        }

        public void RefreshIntel()
        {
            CleanupExpiredSightings();
            RefreshReport();
        }

        private void UpdateMenuTextVariables()
        {
            MBTextManager.SetTextVariable("PAL_HOSTILES_STATE", HostilesOnly ? "only" : "all", false);
            MBTextManager.SetTextVariable("PAL_CATEGORY_STATE", FormatCategoryLabel(_category), false);
            MBTextManager.SetTextVariable("PAL_SORT_STATE", SortLabel(_sortMode), false);
            MBTextManager.SetTextVariable("PAL_HISTORY_STATE", ShowHistoryDetails ? "detailed" : "compact", false);
        }

        private string BuildReport()
        {
            _reportBuilder.Clear();
            var sb = _reportBuilder;

            Vec2 playerPos = GetPlayerPosition();
            ThreatSummary summary = BuildThreatSummary(playerPos);

            sb.AppendLine("=== Palantir Network ===");
            sb.AppendLine("Recent sightings ranked by threat, memory, and movement.");
            sb.AppendLine();

            var hotkey = Hotkey;
            sb.AppendLine("Signal: " + BuildPostureLine(summary));
            sb.AppendLine($"Hotkey: {hotkey}  |  Hostiles: {HostileFilterLabel()}  |  " +
                          $"Category: {FormatCategoryLabel(_category)}  |  Sort: {SortLabel(_sortMode)}");
            sb.AppendLine($"Memory: {IntelDays} day{(IntelDays != 1 ? "s" : "")}  |  " +
                          $"Tracked: {_sightingsByPartyId.Count}  |  History: {(ShowHistoryDetails ? "detailed" : "compact")}");
            sb.AppendLine();
            AppendNetworkSummary(sb, summary);
            sb.AppendLine("----------------------------------------");

            if (_sightingsByPartyId.Count == 0)
            {
                sb.AppendLine("No sightings recorded yet.");
                sb.AppendLine("Scout visible parties to seed the network, then reopen this report.");
                return sb.ToString();
            }

            // ── Single-pass filter into reusable list ────────────────────────
            _filteredCache.Clear();
            foreach (var kv in _sightingsByPartyId)
                if (PassFilters(kv.Value))
                    _filteredCache.Add(kv.Value);

            if (_filteredCache.Count == 0)
            {
                sb.AppendLine("No visions match your filters.");
                sb.AppendLine("Loosen hostiles/category filters or wait for new scout sightings.");
                return sb.ToString();
            }

            SortFilteredContacts(playerPos);

            int totalFiltered = _filteredCache.Count;
            int showing = Math.Min(totalFiltered, MaxResults);

            sb.AppendLine($"Visible contacts: {showing} of {totalFiltered}");
            sb.AppendLine("----------------------------------------");

            for (int i = 0; i < showing; i++)
            {
                var e = _filteredCache[i];
                AppendSightingEntry(sb, e, playerPos);
            }

            if (totalFiltered > MaxResults)
                sb.AppendLine($"... and {totalFiltered - MaxResults} more (increase Max Results in MCM).");

            return sb.ToString();
        }

        public PalantirBoardSnapshot BuildBoardSnapshot()
        {
            CleanupExpiredSightings();

            Vec2 playerPos = GetPlayerPosition();
            ThreatSummary summary = BuildThreatSummary(playerPos);
            int critical;
            int watch;
            int hostile;
            int info;
            BuildDangerBandCounts(playerPos, out critical, out watch, out hostile, out info);

            var snapshot = new PalantirBoardSnapshot
            {
                TitleText = "Palantir Network",
                SignalText = BuildPostureLine(summary),
                FilterText = HostileFilterLabel() + " | " + FormatCategoryLabel(_category) + " | " + SortLabel(_sortMode),
                MemoryText = IntelDays + " day" + (IntelDays != 1 ? "s" : "") + " memory | " +
                             _sightingsByPartyId.Count + " tracked | " +
                             (ShowHistoryDetails ? "detailed history" : "compact history"),
                HostilesButtonText = "Hostiles: " + (HostilesOnly ? "Only" : "All"),
                CategoryButtonText = "Category: " + FormatCategoryLabel(_category),
                SortButtonText = "Sort: " + SortLabel(_sortMode),
                HistoryButtonText = "History: " + (ShowHistoryDetails ? "Detailed" : "Compact"),
                SummaryLineOne = summary.HostileCount + " hostile | " +
                                 summary.ArmyCount + " armies | " +
                                 summary.BanditCount + " bandit parties",
                SummaryLineTwo = summary.TotalObservations + " observations | " +
                                 summary.FreshContacts + " fresh | " +
                                 summary.CriticalContacts + " critical",
                CriticalBandText = "Critical " + critical,
                WatchBandText = "Watch " + watch,
                HostileBandText = "Hostile " + hostile,
                InfoBandText = "Info " + info
            };

            if (summary.HostileCount > 0)
            {
                snapshot.SummaryLineThree =
                    "Danger " + summary.HighestDangerScore + "/100 | " +
                    summary.DangerBand + " | " +
                    summary.MostDangerousName;
                snapshot.PriorityText =
                    "Nearest: " + summary.NearestHostileName + " (" +
                    summary.NearestHostileDistance.ToString("0.0") + ") | " +
                    "Strongest: " + summary.StrongestHostileName + " (~" +
                    summary.StrongestHostileStrength.ToString("0") + ")";
            }
            else
            {
                snapshot.SummaryLineThree = "No hostile contacts in memory";
                snapshot.PriorityText = "Scout visible parties to seed the network.";
            }

            _filteredCache.Clear();
            foreach (var kv in _sightingsByPartyId)
                if (PassFilters(kv.Value))
                    _filteredCache.Add(kv.Value);

            SortFilteredContacts(playerPos);

            int totalFiltered = _filteredCache.Count;
            int showing = Math.Min(totalFiltered, MaxResults);
            snapshot.VisibleText = "Visible contacts: " + showing + " of " + totalFiltered;

            if (_sightingsByPartyId.Count == 0)
            {
                snapshot.EmptyText = "No sightings recorded yet.";
                return snapshot;
            }

            if (totalFiltered == 0)
            {
                snapshot.EmptyText = "No contacts match the current filters.";
                return snapshot;
            }

            for (int i = 0; i < showing; i++)
                snapshot.Contacts.Add(CreateContactSnapshot(_filteredCache[i], playerPos));

            if (totalFiltered > MaxResults)
                snapshot.FooterText = "... and " + (totalFiltered - MaxResults) + " more hidden by Max Results.";

            return snapshot;
        }

        private void BuildDangerBandCounts(
            Vec2 playerPos,
            out int critical,
            out int watch,
            out int hostile,
            out int info)
        {
            critical = 0;
            watch = 0;
            hostile = 0;
            info = 0;

            foreach (var kv in _sightingsByPartyId)
            {
                var sighting = kv.Value;
                if (!sighting.IsHostileAtTime)
                {
                    info++;
                    continue;
                }

                int danger = ThreatScore(sighting, playerPos);
                if (danger >= 75)
                    critical++;
                else if (danger >= 50)
                    watch++;
                else
                    hostile++;
            }
        }

        private void SortFilteredContacts(Vec2 playerPos)
        {
            switch (_sortMode)
            {
                case SortMode.Nearest:
                    _filteredCache.Sort((a, b) =>
                        DistanceSq(playerPos, a).CompareTo(DistanceSq(playerPos, b)));
                    break;
                case SortMode.Strongest:
                    _filteredCache.Sort((a, b) => b.Strength.CompareTo(a.Strength));
                    break;
                case SortMode.MostDangerous:
                    _filteredCache.Sort((a, b) =>
                        ThreatScore(b, playerPos).CompareTo(ThreatScore(a, playerPos)));
                    break;
                case SortMode.MostWatched:
                    _filteredCache.Sort((a, b) =>
                        ObservationCount(b).CompareTo(ObservationCount(a)));
                    break;
                case SortMode.PeakDanger:
                    _filteredCache.Sort((a, b) =>
                        PeakDanger(b, playerPos).CompareTo(PeakDanger(a, playerPos)));
                    break;
                default:
                    _filteredCache.Sort((a, b) => b.SeenAt.CompareTo(a.SeenAt));
                    break;
            }
        }

        private PalantirContactSnapshot CreateContactSnapshot(Sighting e, Vec2 playerPos)
        {
            double hoursAgo = (CampaignTime.Now - e.SeenAt).ToHours;
            string age = hoursAgo < 24.0
                ? hoursAgo.ToString("0.0") + "h ago"
                : (hoursAgo / 24.0).ToString("0.0") + "d ago";

            float dist = Distance(playerPos, e);
            int danger = ThreatScore(e, playerPos);
            string near = FindNearestSettlement(e.X, e.Y);

            var contact = new PalantirContactSnapshot
            {
                Name = e.PartyName,
                Severity = ContactSeverityLabel(e, danger).Trim('[', ']'),
                Kind = KindLabel(e.Kind),
                FactionLine = e.FactionName + " | " + e.TroopCount + " troops | strength ~" + e.Strength.ToString("0"),
                PositionLine = "Seen " + age + " | " + dist.ToString("0.0") + " away " + CompassDirection(playerPos, e) + " | observed " + ObservationCount(e) + "x",
                CounselLine = ThreatAdvice(e, dist, danger),
                DangerText = danger + "/100",
                LocationLine = string.IsNullOrEmpty(near) ? string.Empty : "Near " + near,
                HistoryLine = ShowHistoryDetails
                    ? "First " + FormatAgeFromDay(e.FirstSeenDay) +
                      " | peak str ~" + PeakStrength(e).ToString("0") +
                      " | peak danger " + PeakDanger(e, playerPos) +
                      "/100 | trend " + BuildTrendLabel(e, danger)
                    : string.Empty,
                ShowLocation = !string.IsNullOrEmpty(near),
                ShowHistory = ShowHistoryDetails,
                IsCritical = e.IsHostileAtTime && danger >= 75,
                IsWatch = e.IsHostileAtTime && danger >= 50 && danger < 75,
                IsHostile = e.IsHostileAtTime && danger < 50,
                IsInfo = !e.IsHostileAtTime
            };

            return contact;
        }

        private void AppendSightingEntry(StringBuilder sb, Sighting e, Vec2 playerPos)
        {
            double hoursAgo = (CampaignTime.Now - e.SeenAt).ToHours;
            string age = hoursAgo < 24.0
                ? $"{hoursAgo:0.0}h ago"
                : $"{hoursAgo / 24.0:0.0}d ago";

            float dist  = Distance(playerPos, e);
            string dir  = CompassDirection(playerPos, e);
            string near = FindNearestSettlement(e.X, e.Y);

            int danger = ThreatScore(e, playerPos);
            sb.AppendLine($"{ContactSeverityLabel(e, danger)} {e.PartyName}  ({KindLabel(e.Kind)})");
            sb.AppendLine($"   {e.FactionName}  |  {e.TroopCount} troops  |  strength ~{e.Strength:0}  |  danger {danger}/100");
            sb.AppendLine($"   Seen {age}  |  {dist:0.0} away {dir}  |  observed {ObservationCount(e)}x");
            sb.AppendLine($"   Counsel: {ThreatAdvice(e, dist, danger)}");

            if (!string.IsNullOrEmpty(near))
                sb.AppendLine($"   Near {near}");

            if (ShowHistoryDetails)
            {
                sb.AppendLine(
                    "   History: first " + FormatAgeFromDay(e.FirstSeenDay) +
                    " | peak str ~" + PeakStrength(e).ToString("0") +
                    " | peak danger " + PeakDanger(e, playerPos) +
                    "/100 | trend " + BuildTrendLabel(e, danger));
            }

            sb.AppendLine();
        }

        private static void AppendNetworkSummary(StringBuilder sb, ThreatSummary summary)
        {
            sb.AppendLine("Network summary");
            sb.AppendLine($"Contacts: {summary.HostileCount} hostile  |  {summary.ArmyCount} armies  |  {summary.BanditCount} bandit parties");
            sb.AppendLine($"History: {summary.TotalObservations} observations  |  {summary.FreshContacts} fresh contacts  |  {summary.CriticalContacts} critical");
            if (summary.HostileCount > 0)
            {
                sb.AppendLine(
                    $"Threat: strength ~{summary.HostileStrength:0}  |  nearest {summary.NearestHostileName} ({summary.NearestHostileDistance:0.0})  |  strongest {summary.StrongestHostileName} (~{summary.StrongestHostileStrength:0})");
                sb.AppendLine(
                    $"Danger: {summary.HighestDangerScore}/100  |  {summary.DangerBand}  |  {summary.MostDangerousName}  |  {summary.Advice}");
            }
            else
            {
                sb.AppendLine("Threat: no hostile contacts in memory");
            }
        }

        private ThreatSummary BuildThreatSummary(Vec2 playerPos)
        {
            ThreatSummary summary = new ThreatSummary();
            summary.NearestHostileDistance = float.MaxValue;
            summary.NearestHostileName = "none";
            summary.StrongestHostileName = "none";
            summary.MostDangerousName = "none";
            summary.DangerBand = "quiet";
            summary.Advice = "keep watch";

            foreach (var kv in _sightingsByPartyId)
            {
                var sighting = kv.Value;
                var kind = (PartyKind)sighting.Kind;
                if (kind == PartyKind.Army) summary.ArmyCount++;
                if (kind == PartyKind.Bandit) summary.BanditCount++;

                summary.TotalObservations += ObservationCount(sighting);
                if ((CampaignTime.Now - sighting.SeenAt).ToHours <= 24.0)
                    summary.FreshContacts++;

                if (!sighting.IsHostileAtTime) continue;

                summary.HostileCount++;
                summary.HostileStrength += sighting.Strength;

                float dist = Distance(playerPos, sighting);
                int danger = ThreatScore(sighting, playerPos);
                if (danger >= 75)
                    summary.CriticalContacts++;

                if (danger > summary.HighestDangerScore)
                {
                    summary.HighestDangerScore = danger;
                    summary.MostDangerousName = sighting.PartyName;
                    summary.Advice = ThreatAdvice(sighting, dist, danger);
                    summary.DangerBand = DangerBand(danger);
                }

                if (dist < summary.NearestHostileDistance)
                {
                    summary.NearestHostileDistance = dist;
                    summary.NearestHostileName = sighting.PartyName;
                }

                if (sighting.Strength > summary.StrongestHostileStrength)
                {
                    summary.StrongestHostileStrength = sighting.Strength;
                    summary.StrongestHostileName = sighting.PartyName;
                }
            }

            if (summary.HostileCount <= 0)
                summary.NearestHostileDistance = 0f;

            return summary;
        }

        // ── Filter helpers ───────────────────────────────────────────────────
        private bool PassFilters(Sighting e)
        {
            if (HostilesOnly && !e.IsHostileAtTime) return false;

            var kind = (PartyKind)e.Kind;

            // Per-type MCM overrides
            switch (kind)
            {
                case PartyKind.Caravan: if (!(Cfg?.ShowCaravans ?? true)) return false; break;
                case PartyKind.Army:    if (!(Cfg?.ShowArmies   ?? true)) return false; break;
                case PartyKind.Bandit:  if (!(Cfg?.ShowBandits  ?? true)) return false; break;
            }

            switch (_category)
            {
                case PartyCategory.All:      return true;
                case PartyCategory.Armies:   return kind == PartyKind.Army;
                case PartyCategory.Caravans: return kind == PartyKind.Caravan;
                case PartyCategory.Bandits:  return kind == PartyKind.Bandit;
                case PartyCategory.Other:    return kind == PartyKind.Other;
                case PartyCategory.Threats:  return ThreatScore(e, GetPlayerPosition()) >= Math.Max(40, Cfg?.ThreatPulseThreshold ?? 55);
                default:                     return true;
            }
        }

        // ── Classification helpers ────────────────────────────────────────────
        private static PartyKind Classify(MobileParty p)
        {
            if (p.Army != null) return PartyKind.Army;
            if (p.IsCaravan)    return PartyKind.Caravan;
            if (p.IsBandit)     return PartyKind.Bandit;
            return PartyKind.Other;
        }

        private static string KindLabel(int kind)
        {
            switch ((PartyKind)kind)
            {
                case PartyKind.Caravan: return "Caravan";
                case PartyKind.Army:    return "Army";
                case PartyKind.Bandit:  return "Bandits";
                default:                return "Party";
            }
        }

        private string HostileFilterLabel()
        {
            return HostilesOnly ? "hostiles only" : "all contacts";
        }

        private static string FormatCategoryLabel(PartyCategory category)
        {
            switch (category)
            {
                case PartyCategory.Armies: return "Armies";
                case PartyCategory.Caravans: return "Caravans";
                case PartyCategory.Bandits: return "Bandits";
                case PartyCategory.Other: return "Other";
                case PartyCategory.Threats: return "Threats";
                default: return "All";
            }
        }

        private static string SortLabel(SortMode mode)
        {
            switch (mode)
            {
                case SortMode.MostRecent: return "Recent";
                case SortMode.Nearest:    return "Nearest";
                case SortMode.Strongest:  return "Strongest";
                case SortMode.MostDangerous: return "Danger";
                case SortMode.MostWatched: return "Watched";
                case SortMode.PeakDanger:  return "Peak danger";
                default:                  return mode.ToString();
            }
        }

        private static string BuildPostureLine(ThreatSummary summary)
        {
            if (summary.HostileCount <= 0)
                return "quiet - no hostile contacts in memory";

            return summary.DangerBand + " - " +
                   summary.MostDangerousName + " at " +
                   summary.HighestDangerScore + "/100; " +
                   summary.Advice;
        }

        private static string ContactSeverityLabel(Sighting e, int danger)
        {
            if (!e.IsHostileAtTime)
                return "[INFO]";
            if (danger >= 75)
                return "[CRITICAL]";
            if (danger >= 50)
                return "[WATCH]";
            return "[HOSTILE]";
        }

        private static int ThreatScore(Sighting e, Vec2 playerPos)
        {
            if (!e.IsHostileAtTime) return 0;

            int score = 10;
            score += Math.Min(28, (int)(e.Strength / 35f));
            score += Math.Min(16, e.TroopCount / 18);

            PartyKind kind = (PartyKind)e.Kind;
            if (kind == PartyKind.Army) score += 22;
            else if (kind == PartyKind.Bandit) score += 10;

            float dist = Distance(playerPos, e);
            if (dist <= 10f) score += 20;
            else if (dist <= 25f) score += 14;
            else if (dist <= 50f) score += 8;

            float playerStrength = GetPlayerStrength();
            if (playerStrength > 1f)
            {
                float ratio = e.Strength / playerStrength;
                if (ratio >= 2.0f) score += 18;
                else if (ratio >= 1.25f) score += 12;
                else if (ratio >= 0.75f) score += 6;
            }

            double hoursAgo = (CampaignTime.Now - e.SeenAt).ToHours;
            if (hoursAgo <= 6.0) score += 8;
            else if (hoursAgo <= 24.0) score += 4;

            int seenCount = ObservationCount(e);
            if (seenCount >= 2)
                score += Math.Min(10, seenCount * 2);

            if (e.WasHostilePreviously)
                score += 4;

            if (e.PeakDangerScore >= 75)
                score += 4;

            return score > 100 ? 100 : score;
        }

        private static string DangerBand(int score)
        {
            if (score >= 80) return "critical";
            if (score >= 60) return "severe";
            if (score >= 40) return "watch";
            return "low";
        }

        private static string ThreatAdvice(Sighting e, float distance, int danger)
        {
            if (!e.IsHostileAtTime) return "observe only";
            if (danger >= 80) return "avoid or summon an army";
            if ((PartyKind)e.Kind == PartyKind.Army) return "track army movement";
            if (distance <= 15f) return "prepare interception";
            if (e.TroopCount >= 120) return "scout before engagement";
            if ((PartyKind)e.Kind == PartyKind.Bandit) return "clear before it multiplies";
            return "mark and monitor";
        }

        private struct ThreatSummary
        {
            public int HostileCount;
            public int ArmyCount;
            public int BanditCount;
            public float HostileStrength;
            public float NearestHostileDistance;
            public string NearestHostileName;
            public float StrongestHostileStrength;
            public string StrongestHostileName;
            public int HighestDangerScore;
            public string MostDangerousName;
            public string DangerBand;
            public string Advice;
            public int TotalObservations;
            public int FreshContacts;
            public int CriticalContacts;
        }

        private static bool IsHostileToPlayer(MobileParty p)
        {
            try
            {
                var playerFaction = Hero.MainHero?.MapFaction;
                if (playerFaction == null || p.MapFaction == null) return false;
                return FactionManager.IsAtWarAgainstFaction(playerFaction, p.MapFaction);
            }
            catch { return false; }
        }

        private static Sighting NormalizeLoadedSighting(Sighting e)
        {
            if (e.SeenCount <= 0)
                e.SeenCount = 1;
            if (e.FirstSeenDay <= 0.0)
                e.FirstSeenDay = e.SeenAt.ToDays;
            if (e.PreviousSeenDay == 0.0)
                e.PreviousSeenDay = -1.0;
            if (e.PeakStrength <= 0f)
                e.PeakStrength = e.Strength;
            if (e.PeakTroopCount <= 0)
                e.PeakTroopCount = e.TroopCount;
            if (e.PeakDangerScore < e.LastDangerScore)
                e.PeakDangerScore = e.LastDangerScore;

            return e;
        }

        private static int ObservationCount(Sighting e)
        {
            return e.SeenCount > 0 ? e.SeenCount : 1;
        }

        private static float PeakStrength(Sighting e)
        {
            return e.PeakStrength > 0f ? e.PeakStrength : e.Strength;
        }

        private static int PeakDanger(Sighting e, Vec2 playerPos)
        {
            int current = ThreatScore(e, playerPos);
            return Math.Max(e.PeakDangerScore, current);
        }

        private static string BuildTrendLabel(Sighting e, int currentDanger)
        {
            int previousDanger = e.LastDangerScore;
            if (ObservationCount(e) <= 1 || previousDanger <= 0)
                return "new contact";

            int delta = currentDanger - previousDanger;
            if (delta >= 10)
                return "worsening";
            if (delta <= -10)
                return "fading";
            if (e.WasHostilePreviously && e.IsHostileAtTime)
                return "persistent hostile";
            return "steady";
        }

        private static string FormatAgeFromDay(double day)
        {
            if (day <= 0.0)
                return "unknown";

            double ageDays = CampaignTime.Now.ToDays - day;
            if (ageDays < 0.0)
                ageDays = 0.0;
            if (ageDays < 1.0)
                return (ageDays * 24.0).ToString("0.0") + "h ago";
            return ageDays.ToString("0.0") + "d ago";
        }

        private static float GetPlayerStrength()
        {
            try
            {
                var main = MobileParty.MainParty;
                if (main != null)
                    return main.GetTotalLandStrengthWithFollowers();
            }
            catch { }

            return 0f;
        }

        // ── Spatial helpers ──────────────────────────────────────────────────
        private static Vec2 GetPlayerPosition()
        {
            try
            {
                var main = MobileParty.MainParty;
                if (main != null)
                    return main.GetPosition2D;
            }
            catch { }
            return Vec2.Zero;
        }

        private static float DistanceSq(Vec2 playerPos, Sighting e)
        {
            float dx = playerPos.x - e.X;
            float dy = playerPos.y - e.Y;
            return dx * dx + dy * dy;
        }

        private static float Distance(Vec2 playerPos, Sighting e)
        {
            return (float)Math.Sqrt(DistanceSq(playerPos, e));
        }

        /// <summary>
        /// Returns an 8-point compass direction string (N, NE, E, etc.)
        /// from the player toward the sighting.
        /// </summary>
        private static string CompassDirection(Vec2 playerPos, Sighting e)
        {
            float dx = e.X - playerPos.x;
            float dy = e.Y - playerPos.y;
            if (dx == 0f && dy == 0f) return "";

            // atan2 gives angle from positive X axis; Bannerlord Y is "north".
            double angle = Math.Atan2(dx, dy) * (180.0 / Math.PI);
            if (angle < 0) angle += 360.0;

            // 8 sectors of 45° each, offset by 22.5°
            int sector = (int)((angle + 22.5) / 45.0) % 8;
            switch (sector)
            {
                case 0: return "(N)";
                case 1: return "(NE)";
                case 2: return "(E)";
                case 3: return "(SE)";
                case 4: return "(S)";
                case 5: return "(SW)";
                case 6: return "(W)";
                case 7: return "(NW)";
                default: return "";
            }
        }

        /// <summary>
        /// Finds the closest settlement to the given world coordinates.
        /// Returns the settlement name, or null if none found.
        /// </summary>
        private static string FindNearestSettlement(float x, float y)
        {
            try
            {
                Settlement nearest = null;
                float bestDist = float.MaxValue;

                foreach (var s in Settlement.All)
                {
                    if (s == null || s.IsHideout) continue;
                    Vec2 position = s.GetPosition2D;
                    float dx = position.x - x;
                    float dy = position.y - y;
                    float d = dx * dx + dy * dy;
                    if (d < bestDist)
                    {
                        bestDist = d;
                        nearest = s;
                    }
                }

                return nearest?.Name?.ToString();
            }
            catch
            {
                return null;
            }
        }

        // ── Logging helper ───────────────────────────────────────────────────
        private static void LogDebug(string message)
        {
            try
            {
                InformationManager.DisplayMessage(
                    new InformationMessage($"[Palantir] {message}", Colors.Gray));
            }
            catch { }
        }

        private static string SanitizeMenuId(string menuId)
        {
            if (string.IsNullOrEmpty(menuId))
                return "unknown";

            var sb = new StringBuilder(menuId.Length);
            for (int i = 0; i < menuId.Length; i++)
            {
                char c = menuId[i];
                if ((c >= 'a' && c <= 'z') ||
                    (c >= 'A' && c <= 'Z') ||
                    (c >= '0' && c <= '9') ||
                    c == '_')
                {
                    sb.Append(c);
                }
                else
                {
                    sb.Append('_');
                }
            }
            return sb.ToString();
        }

        // ── Menu activation (reflection shim) ────────────────────────────────
        /// <summary>
        /// Opens a GameMenu from the campaign map.
        /// Uses reflection to stay compatible across Bannerlord minor versions.
        /// </summary>
        private static void ActivateGameMenuSafe(string menuId)
        {
            // Attempt 1: static GameMenu.ActivateGameMenu(string)
            try
            {
                var m = typeof(GameMenu).GetMethod(
                    "ActivateGameMenu",
                    BindingFlags.Public | BindingFlags.Static,
                    null, new[] { typeof(string) }, null);
                if (m != null) { m.Invoke(null, new object[] { menuId }); return; }
            }
            catch (Exception ex)
            {
                LogDebug($"ActivateGameMenu static failed: {ex.Message}");
            }

            // Attempt 2: Campaign.Current.GameMenuManager.ActivateGameMenu(string)
            try
            {
                var campaign = Campaign.Current;
                if (campaign == null) return;

                var mgr = campaign.GetType()
                    .GetProperty("GameMenuManager", BindingFlags.Public | BindingFlags.Instance)
                    ?.GetValue(campaign);
                if (mgr == null)
                {
                    LogDebug("GameMenuManager property not found");
                    return;
                }

                mgr.GetType()
                    .GetMethod("ActivateGameMenu",
                        BindingFlags.Public | BindingFlags.Instance,
                        null, new[] { typeof(string) }, null)
                    ?.Invoke(mgr, new object[] { menuId });
            }
            catch (Exception ex)
            {
                LogDebug($"ActivateGameMenu instance failed: {ex.Message}");
            }
        }
    }
}
