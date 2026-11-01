using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TAOM_LordsLedger.UI;

namespace TAOM_LordsLedger
{
    /// <summary>
    /// Thin coordinator. Owns the LedgerSettings, LedgerRenderer, and persistent
    /// state (loyalty history). Wires CampaignEvents, the game menu, the hotkey,
    /// the daily background scan, and SyncData persistence.
    /// Data reading is delegated to LedgerDataReader; report building to LedgerRenderer.
    /// </summary>
    public sealed class LedgerCampaignBehavior : CampaignBehaviorBase
    {
        // ── Menu / option IDs ────────────────────────────────────────────
        private const string MENU_ID        = "taom_lords_ledger";
        private const string OPT_SORT       = "taom_ledger_sort";
        private const string OPT_FILTER     = "taom_ledger_filter";
        private const string OPT_WARNINGS   = "taom_ledger_warnings";
        private const string OPT_PRESET     = "taom_ledger_preset";
        private const string OPT_VIEW       = "taom_ledger_view";
        private const string OPT_SCOPE      = "taom_ledger_scope";
        private const string OPT_EXPORT     = "taom_ledger_export";
        private const string OPT_REFRESH    = "taom_ledger_refresh";
        private const string OPT_CLOSE      = "taom_ledger_close";

        private const InputKey HOTKEY = InputKey.L;

        private static readonly string[] StandardRootMenus =
        {
            "town",
            "castle",
            "village",
            "camp",
            "encounter"
        };

        // ── Owned state ──────────────────────────────────────────────────
        private readonly LedgerSettings  _settings = new LedgerSettings();
        private readonly LedgerRenderer  _renderer = new LedgerRenderer();

        // ── Persistent data ───────────────────────────────────────────────
        // Loyalty recorded each daily tick — used to compute per-settlement trend deltas.
        private Dictionary<string, float> _loyaltyHistory = new Dictionary<string, float>();

        // ── Non-persistent: daily scan state ──────────────────────────────
        // Tracks which settlements were in a warning state last tick so we can
        // fire a feed message only when a settlement newly crosses a threshold.
        // Not saved — a batch of "new warning" messages on first load after
        // restarting is acceptable (they correctly describe current state).
        private Dictionary<string, bool> _prevWarnings = new Dictionary<string, bool>();
        private double _lastGovernancePulseDay = -999.0;

        // ── Color palette ─────────────────────────────────────────────────
        private static readonly Color DangerColor  = new Color(0.90f, 0.25f, 0.20f);
        private static readonly Color WarningColor = new Color(0.90f, 0.65f, 0.20f);
        private static readonly Color InfoColor    = new Color(0.70f, 0.70f, 0.70f);

        // ── CampaignBehaviorBase ──────────────────────────────────────────

        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
            CampaignEvents.OnNewGameCreatedEvent.AddNonSerializedListener(this, OnNewGame);
            CampaignEvents.OnGameLoadedEvent.AddNonSerializedListener(this, OnGameLoaded);
            CampaignEvents.TickEvent.AddNonSerializedListener(this, OnTick);
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
        }

        public override void SyncData(IDataStore dataStore)
        {
            // Persist settings as ints/bools — enums are not directly serializable.
            int sortInt   = (int)_settings.Sort;
            int filterInt = (int)_settings.Filter;
            int presetInt = (int)_settings.Preset;
            bool warnOnly = _settings.WarningsOnly;
            bool compact  = _settings.CompactView;
            bool vassals  = _settings.IncludeVassals;
            bool pulse    = _settings.ShowGovernancePulse;
            int pulseDays = _settings.GovernancePulseIntervalDays;

            dataStore.SyncData("ledger_sort",     ref sortInt);
            dataStore.SyncData("ledger_filter",   ref filterInt);
            dataStore.SyncData("ledger_preset",   ref presetInt);
            dataStore.SyncData("ledger_warnonly", ref warnOnly);
            dataStore.SyncData("ledger_compact",  ref compact);
            dataStore.SyncData("ledger_vassals",  ref vassals);
            dataStore.SyncData("ledger_pulse",    ref pulse);
            dataStore.SyncData("ledger_pulse_days", ref pulseDays);
            dataStore.SyncData("ledger_last_pulse_day", ref _lastGovernancePulseDay);
            dataStore.SyncData("ledger_loyalty",  ref _loyaltyHistory);

            _settings.Sort           = (SortMode)sortInt;
            _settings.Filter         = (FiefFilter)filterInt;
            _settings.Preset         = (ThresholdPreset)presetInt;
            _settings.WarningsOnly   = warnOnly;
            _settings.CompactView    = compact;
            _settings.IncludeVassals = vassals;
            _settings.ShowGovernancePulse = pulse;
            _settings.GovernancePulseIntervalDays = Math.Max(1, pulseDays);
        }

        // ── Lifecycle ─────────────────────────────────────────────────────

        private void OnNewGame(CampaignGameStarter _)
        {
            _loyaltyHistory.Clear();
            _prevWarnings.Clear();
            SnapshotLoyalty();
            SnapshotWarnings();
        }

        private void OnGameLoaded(CampaignGameStarter _)
        {
            // Populate _prevWarnings from current state so the first DailyTick
            // after loading only fires messages for NEW threshold crossings,
            // not everything that was already in warning before saving.
            _prevWarnings.Clear();
            SnapshotWarnings();
        }

        // ── Hotkey ────────────────────────────────────────────────────────

        private void OnTick(float dt)
        {
            if (Campaign.Current == null || !Campaign.Current.GameStarted) return;
            if (Campaign.Current.CurrentMenuContext != null) return;

            try   { if (!Input.IsKeyPressed(HOTKEY)) return; }
            catch { return; }

            OpenLedgerOrFallback();
        }

        // ── Daily background scan ─────────────────────────────────────────

        private void OnDailyTick()
        {
            ScanForNewWarnings();
            TryShowGovernancePulse();
            SnapshotLoyalty();
        }

        private void TryShowGovernancePulse()
        {
            if (!_settings.ShowGovernancePulse) return;
            if (Hero.MainHero?.Clan == null) return;

            double today = Math.Floor(CampaignTime.Now.ToDays);
            int interval = Math.Max(1, _settings.GovernancePulseIntervalDays);
            if (today < _lastGovernancePulseDay + interval) return;

            int total = 0, warnings = 0, readinessTotal = 0, reinforceNeed = 0;
            FiefInfo worst = default(FiefInfo);
            bool hasWorst = false;

            foreach (var s in Settlement.All)
            {
                if (s == null) continue;
                if (!LedgerDataReader.IsPlayerClanOwned(s) &&
                    !(_settings.IncludeVassals && LedgerDataReader.IsKingdomOwned(s)))
                    continue;

                var info = LedgerDataReader.Read(s, _settings, _loyaltyHistory);
                total++;
                if (info.HasWarning) warnings++;
                readinessTotal += info.GovernanceScore;
                reinforceNeed += info.ReinforcementNeed;

                if (!hasWorst || info.RiskScore > worst.RiskScore)
                {
                    worst = info;
                    hasWorst = true;
                }
            }

            if (total <= 0 || !hasWorst) return;

            _lastGovernancePulseDay = today;
            int readiness = readinessTotal / total;
            Color color = readiness < 50 || warnings > 0 ? WarningColor : InfoColor;

            InformationManager.DisplayMessage(new InformationMessage(
                "[Ledger Pulse] " + total + " fiefs | readiness " + readiness + "/100" +
                " | " + warnings + " need attention" +
                " | reinforce need " + reinforceNeed +
                " | priority " + worst.Name + ": " + worst.ActionHint +
                " (Press L)",
                color));
        }

        private void ScanForNewWarnings()
        {
            if (Hero.MainHero?.Clan == null) return;

            foreach (var s in Settlement.All)
            {
                if (s == null) continue;
                if (!LedgerDataReader.IsPlayerClanOwned(s) &&
                    !(_settings.IncludeVassals && LedgerDataReader.IsKingdomOwned(s)))
                    continue;

                var info = LedgerDataReader.Read(s, _settings, _loyaltyHistory);
                bool wasWarning = _prevWarnings.TryGetValue(s.StringId, out bool prev) && prev;
                bool isWarning  = info.HasWarning;

                if (isWarning && !wasWarning)
                    FireWarningMessage(info, s.StringId);

                _prevWarnings[s.StringId] = isWarning;
            }
        }

        private void FireWarningMessage(FiefInfo f, string id)
        {
            string reason;
            Color  color;

            if (f.IsUnderSiege)
            {
                reason = "is under siege!";
                color  = DangerColor;
            }
            else if (f.IsDanger)
            {
                reason = $"garrison thin ({f.GarrisonCount}) with enemy army nearby!";
                color  = DangerColor;
            }
            else if (f.IsRaidedOrLooted)
            {
                reason = "is being raided!";
                color  = DangerColor;
            }
            else if (f.Loyalty > 0f && f.Loyalty < _settings.LowLoyalty)
            {
                reason = $"loyalty has fallen to {f.Loyalty:0}";
                color  = WarningColor;
            }
            else if (f.FoodChange < _settings.LowFood)
            {
                reason = $"food shortage ({f.FoodChange:+0.#;-0.#;0}/day)";
                color  = WarningColor;
            }
            else if (f.GarrisonCount < _settings.LowGarrison)
            {
                reason = $"garrison is weak ({f.GarrisonCount} troops)";
                color  = WarningColor;
            }
            else if (f.Security > 0f && f.Security < _settings.LowSecurity)
            {
                reason = $"security has dropped to {f.Security:0}";
                color  = WarningColor;
            }
            else
            {
                reason = "needs attention";
                color  = WarningColor;
            }

            InformationManager.DisplayMessage(new InformationMessage(
                $"[Ledger] {f.Name}: {reason}  Action: {f.ActionHint}.  (Press L to open)", color));
        }

        // ── Loyalty snapshot ──────────────────────────────────────────────

        private void SnapshotLoyalty()
        {
            foreach (var s in Settlement.All)
            {
                if (s == null || s.IsVillage) continue;
                var town = s.Town;
                if (town == null) continue;
                try { _loyaltyHistory[s.StringId] = town.Loyalty; }
                catch { }
            }
        }

        private void SnapshotWarnings()
        {
            if (Hero.MainHero?.Clan == null) return;
            foreach (var s in Settlement.All)
            {
                if (s == null) continue;
                if (!LedgerDataReader.IsPlayerClanOwned(s) &&
                    !LedgerDataReader.IsKingdomOwned(s)) continue;
                try
                {
                    var info = LedgerDataReader.Read(s, _settings, _loyaltyHistory);
                    _prevWarnings[s.StringId] = info.HasWarning;
                }
                catch { }
            }
        }

        // ── Report ────────────────────────────────────────────────────────

        private void RefreshReport()
        {
            string report = _renderer.Build(_settings, _loyaltyHistory);
            MBTextManager.SetTextVariable("LEDGER_REPORT", new TextObject(report), false);
            SetMenuOptionLabels();
        }

        private void OpenLedgerOrFallback()
        {
            RefreshReport();

            if (!TryOpenLedgerBoard())
                GameMenu.ActivateGameMenu(MENU_ID);
        }

        public bool TryOpenLedgerBoard()
        {
            try
            {
                return LedgerBoardScreen.TryOpen(this);
            }
            catch (Exception ex)
            {
                InformationManager.DisplayMessage(
                    new InformationMessage("[Lord's Ledger] Board failed: " + ex.Message, WarningColor));
                return false;
            }
        }

        public LedgerBoardSnapshot BuildBoardSnapshot()
        {
            return _renderer.BuildSnapshot(_settings, _loyaltyHistory);
        }

        public void CycleSort()
        {
            _settings.CycleSort();
            RefreshReport();
        }

        public void CycleFilter()
        {
            _settings.CycleFilter();
            RefreshReport();
        }

        public void ToggleWarningsOnly()
        {
            _settings.WarningsOnly = !_settings.WarningsOnly;
            RefreshReport();
        }

        public void CyclePreset()
        {
            _settings.CyclePreset();
            RefreshReport();
        }

        public void ToggleCompactView()
        {
            _settings.CompactView = !_settings.CompactView;
            RefreshReport();
        }

        public void ToggleScope()
        {
            _settings.IncludeVassals = !_settings.IncludeVassals;
            RefreshReport();
        }

        public string ExportLedger()
        {
            string msg = _renderer.ExportToFile(_settings, _loyaltyHistory);
            RefreshReport();
            return msg;
        }

        public void RefreshLedger()
        {
            RefreshReport();
        }

        private void SetMenuOptionLabels()
        {
            MBTextManager.SetTextVariable("LEDGER_SORT_LBL",    _settings.SortLabel());
            MBTextManager.SetTextVariable("LEDGER_FILTER_LBL",  _settings.FilterLabel());
            MBTextManager.SetTextVariable("LEDGER_WARN_LBL",    _settings.WarningsLabel());
            MBTextManager.SetTextVariable("LEDGER_PRESET_LBL",  _settings.PresetLabel());
            MBTextManager.SetTextVariable("LEDGER_VIEW_LBL",    _settings.ViewLabel());
            MBTextManager.SetTextVariable("LEDGER_SCOPE_LBL",   _settings.ScopeLabel());
        }

        // ── Session / menu setup ──────────────────────────────────────────

        private void OnSessionLaunched(CampaignGameStarter starter)
        {
            starter.AddGameMenu(
                MENU_ID,
                "{=TAOM_LEDGER_REPORT}{LEDGER_REPORT}",
                args => RefreshReport(),
                GameMenu.MenuOverlayType.None,
                GameMenu.MenuFlags.None,
                null);

            AddOption(starter, OPT_SORT,
                "{=TAOM_SORT}Sort: {LEDGER_SORT_LBL}",
                GameMenuOption.LeaveType.Submenu, 0,
                CycleSort);

            AddOption(starter, OPT_FILTER,
                "{=TAOM_FILTER}Filter: {LEDGER_FILTER_LBL}",
                GameMenuOption.LeaveType.Trade, 1,
                CycleFilter);

            AddOption(starter, OPT_WARNINGS,
                "{=TAOM_WARN}Warnings only: {LEDGER_WARN_LBL}",
                GameMenuOption.LeaveType.DefendAction, 2,
                ToggleWarningsOnly);

            AddOption(starter, OPT_PRESET,
                "{=TAOM_PRESET}Thresholds: {LEDGER_PRESET_LBL}",
                GameMenuOption.LeaveType.Manage, 3,
                CyclePreset);

            AddOption(starter, OPT_VIEW,
                "{=TAOM_VIEW}View: {LEDGER_VIEW_LBL}",
                GameMenuOption.LeaveType.Continue, 4,
                ToggleCompactView);

            AddOption(starter, OPT_SCOPE,
                "{=TAOM_SCOPE}Scope: {LEDGER_SCOPE_LBL}",
                GameMenuOption.LeaveType.Ransom, 5,
                ToggleScope);

            AddOption(starter, OPT_EXPORT,
                "{=TAOM_EXPORT}Export to file",
                GameMenuOption.LeaveType.Submenu, 6,
                () =>
                {
                    string msg = ExportLedger();
                    InformationManager.DisplayMessage(new InformationMessage(msg, InfoColor));
                });

            AddOption(starter, OPT_REFRESH,
                "{=TAOM_REFRESH}Refresh",
                GameMenuOption.LeaveType.Continue, 7,
                RefreshLedger);

            starter.AddGameMenuOption(
                MENU_ID, OPT_CLOSE,
                "{=TAOM_CLOSE}Close the Ledger",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Leave;
                    return true;
                },
                args => GameMenu.ExitToLast(),
                isLeave: true, index: 8);

            AddRootMenuAccess(starter);
        }

        // ── Menu helper ───────────────────────────────────────────────────

        private void AddOption(
            CampaignGameStarter starter,
            string id, string text,
            GameMenuOption.LeaveType leaveType, int index,
            Action onConsequence)
        {
            starter.AddGameMenuOption(
                MENU_ID, id, text,
                args =>
                {
                    args.optionLeaveType = leaveType;
                    SetMenuOptionLabels();
                    return true;
                },
                args => onConsequence(),
                isLeave: false, index: index);
        }

        private void AddRootMenuAccess(CampaignGameStarter starter)
        {
            for (int i = 0; i < StandardRootMenus.Length; i++)
                TryAddRootMenuOption(starter, StandardRootMenus[i]);
        }

        private void TryAddRootMenuOption(CampaignGameStarter starter, string menuId)
        {
            try
            {
                starter.AddGameMenuOption(
                    menuId,
                    "taom_lords_ledger_open_" + menuId,
                    "{=TAOM_LEDGER_OPEN}Open the Lord's Ledger",
                    args =>
                    {
                        args.optionLeaveType = GameMenuOption.LeaveType.Submenu;
                        return true;
                    },
                    args => OpenLedgerOrFallback(),
                    isLeave: false,
                    index: 996);
            }
            catch
            {
                // Some menus may not exist in every campaign state.
            }
        }
    }
}
