using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Library;
using TAOM_CheatPanel.Core;

namespace TAOM_CheatPanel.UI.Tabs
{
    public sealed class TeleportTabVM : ViewModel
    {
        private MBBindingList<SettlementEntryVM> _visibleSettlements;
        private List<SettlementEntryVM>          _allSettlements;
        private string _searchText  = "";
        private string _statusText  = "";
        private string _filterMode  = "All"; // "All", "Town", "Castle", "Village"

        public TeleportTabVM()
        {
            _allSettlements     = new List<SettlementEntryVM>();
            _visibleSettlements = new MBBindingList<SettlementEntryVM>();
            LoadSettlements();
        }

        // ── Bound properties ──────────────────────────────────────────────

        [DataSourceProperty]
        public MBBindingList<SettlementEntryVM> VisibleSettlements
        {
            get { return _visibleSettlements; }
            set { if (_visibleSettlements == value) return; _visibleSettlements = value; OnPropertyChangedWithValue(value, nameof(VisibleSettlements)); }
        }

        [DataSourceProperty]
        public string SearchText
        {
            get { return _searchText; }
            set
            {
                if (_searchText == value) return;
                _searchText = value;
                OnPropertyChangedWithValue(value, nameof(SearchText));
                RebuildVisible();
            }
        }

        [DataSourceProperty]
        public string StatusText
        {
            get { return _statusText; }
            set { if (_statusText == value) return; _statusText = value; OnPropertyChangedWithValue(value, nameof(StatusText)); }
        }

        [DataSourceProperty]
        public string FilterMode
        {
            get { return _filterMode; }
            set { if (_filterMode == value) return; _filterMode = value; OnPropertyChangedWithValue(value, nameof(FilterMode)); RebuildVisible(); }
        }

        // ── Commands — filter shortcuts ───────────────────────────────────

        public void ExecuteFilterAll()     => FilterMode = "All";
        public void ExecuteFilterTowns()   => FilterMode = "Town";
        public void ExecuteFilterCastles() => FilterMode = "Castle";
        public void ExecuteFilterVillages()=> FilterMode = "Village";

        // ── Internal ──────────────────────────────────────────────────────

        private void LoadSettlements()
        {
            _allSettlements.Clear();
            try
            {
                var settlements = Settlement.All
                    .Where(s => s != null && (s.IsTown || s.IsCastle || s.IsVillage))
                    .OrderBy(s => s.IsTown   ? 0 : s.IsCastle ? 1 : 2)
                    .ThenBy(s  => s.Name?.ToString() ?? "")
                    .ToList();

                foreach (var s in settlements)
                {
                    _allSettlements.Add(new SettlementEntryVM(
                        settlementId:   s.StringId,
                        name:           s.Name?.ToString() ?? s.StringId,
                        cultureName:    s.Culture?.Name?.ToString() ?? "Unknown",
                        settlementType: s.IsTown ? "Town" : s.IsCastle ? "Castle" : "Village",
                        onTeleport:     OnTeleportRequested
                    ));
                }

                RebuildVisible();
            }
            catch (Exception ex)
            {
                StatusText = "Error loading settlements.";
                TAOM_CheatPanel.Runtime.Log.ErrorOnce("teleport.load", ex);
            }
        }

        private void RebuildVisible()
        {
            _visibleSettlements.Clear();
            var q = _allSettlements.AsEnumerable();

            if (_filterMode != "All")
                q = q.Where(e => e.SettlementType == _filterMode);

            if (!string.IsNullOrWhiteSpace(_searchText))
                q = q.Where(e => e.Name.IndexOf(_searchText, StringComparison.OrdinalIgnoreCase) >= 0
                               || e.CultureName.IndexOf(_searchText, StringComparison.OrdinalIgnoreCase) >= 0);

            foreach (var e in q)
                _visibleSettlements.Add(e);
        }

        private void OnTeleportRequested(string settlementId, string settlementName)
        {
            TAOM_CheatPanel.UI.CheatPanelHost.RequestConfirmation(
                $"Teleport your party to {settlementName}?",
                () =>
                {
                    try
                    {
                        var settlement = Settlement.Find(settlementId);
                        if (settlement == null)
                        { StatusText = $"Settlement '{settlementId}' not found."; return; }

                        var party = MobileParty.MainParty;
                        if (party == null)
                        { StatusText = "Main party not found."; return; }

                        // If player is currently inside a settlement, leave it first.
                        if (Hero.MainHero.CurrentSettlement != null)
                            LeaveSettlementAction.ApplyForParty(party);

                        Bannerlord115Compat.SetPosition2D(party, settlement.GatePosition);

                        var msg = $"Teleported to {settlementName}.";
                        StatusText = msg;
                        TAOM_CheatPanel.Runtime.RecentActionsLog.Record(msg);
                        TAOM_CheatPanel.Runtime.Log.Info("[Teleport] " + msg);
                    }
                    catch (Exception ex)
                    {
                        StatusText = "Teleport failed: " + ex.Message;
                        TAOM_CheatPanel.Runtime.Log.ErrorOnce("teleport.execute." + settlementId, ex);
                    }
                }
            );
        }
    }

    public sealed class SettlementEntryVM : ViewModel
    {
        private readonly string _settlementId;
        private readonly Action<string, string> _onTeleport;

        public SettlementEntryVM(string settlementId, string name, string cultureName,
                                  string settlementType, Action<string, string> onTeleport)
        {
            _settlementId   = settlementId;
            _onTeleport     = onTeleport;
            Name            = name;
            CultureName     = cultureName;
            SettlementType  = settlementType;
        }

        [DataSourceProperty] public string Name           { get; }
        [DataSourceProperty] public string CultureName    { get; }
        [DataSourceProperty] public string SettlementType { get; }

        public void ExecuteTeleport() => _onTeleport?.Invoke(_settlementId, Name);
    }
}
