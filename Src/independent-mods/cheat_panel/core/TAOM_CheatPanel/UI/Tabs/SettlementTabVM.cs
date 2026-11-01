using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Library;
using TAOM_CheatPanel.Core;

namespace TAOM_CheatPanel.UI.Tabs
{
    /// <summary>
    /// Gauntlet UI tab for managing the settlement the player is currently inside.
    /// Bound to: taom_utilityhub_settlement_tab (XML movie expected).
    ///
    /// Provides the same settlement tools as HubMenuBehavior's settlement submenu,
    /// but in the tabbed screen UI with text-field input instead of preset menus.
    ///
    /// NOTE: This tab requires a corresponding Gauntlet XML prefab to display.
    /// Register it in SubModule.cs as:
    ///   "taom.utilityhub.settlement" → SettlementTabVM, sortOrder 45
    /// </summary>
    public sealed class SettlementTabVM : ViewModel
    {
        private string _settlementInfo  = "";
        private string _prosperityInput = "1000";
        private string _loyaltyInput    = "75";
        private string _securityInput   = "75";
        private string _garrisonInput   = "50";
        private string _statusText      = "";

        public SettlementTabVM()
        {
            RefreshInfo();
        }

        // ── Bound properties ──────────────────────────────────────────────

        /// <summary>Read-only stats block shown at the top of the tab.</summary>
        [DataSourceProperty]
        public string SettlementInfo
        {
            get { return _settlementInfo; }
            set { if (_settlementInfo == value) return; _settlementInfo = value; OnPropertyChangedWithValue(value, nameof(SettlementInfo)); }
        }

        [DataSourceProperty]
        public string ProsperityInput
        {
            get { return _prosperityInput; }
            set { if (_prosperityInput == value) return; _prosperityInput = value; OnPropertyChangedWithValue(value, nameof(ProsperityInput)); }
        }

        [DataSourceProperty]
        public string LoyaltyInput
        {
            get { return _loyaltyInput; }
            set { if (_loyaltyInput == value) return; _loyaltyInput = value; OnPropertyChangedWithValue(value, nameof(LoyaltyInput)); }
        }

        [DataSourceProperty]
        public string SecurityInput
        {
            get { return _securityInput; }
            set { if (_securityInput == value) return; _securityInput = value; OnPropertyChangedWithValue(value, nameof(SecurityInput)); }
        }

        [DataSourceProperty]
        public string GarrisonInput
        {
            get { return _garrisonInput; }
            set { if (_garrisonInput == value) return; _garrisonInput = value; OnPropertyChangedWithValue(value, nameof(GarrisonInput)); }
        }

        [DataSourceProperty]
        public string StatusText
        {
            get { return _statusText; }
            set { if (_statusText == value) return; _statusText = value; OnPropertyChangedWithValue(value, nameof(StatusText)); }
        }

        // ── Commands ──────────────────────────────────────────────────────

        public void ExecuteRefresh() => RefreshInfo();

        public void ExecuteSetProsperity()
        {
            var sett = RequireTown(); if (sett == null) return;
            if (!ParsePositiveInt(_prosperityInput, "prosperity", out int v)) return;

            Confirm("Set prosperity of " + sett.Name + " to " + v.ToString("N0") + "?", () =>
            {
                try
                {
                    sett.Town.Prosperity = v;
                    Done("Prosperity of " + sett.Name + " set to " + v.ToString("N0") + ".");
                }
                catch (Exception ex) { HandleError("settlement.prosperity", ex); }
            });
        }

        public void ExecuteSetLoyalty()
        {
            var sett = RequireTown(); if (sett == null) return;
            if (!ParsePositiveInt(_loyaltyInput, "loyalty", out int v)) return;

            Confirm("Set loyalty of " + sett.Name + " to " + v + "?", () =>
            {
                try
                {
                    sett.Town.Loyalty = v;
                    Done("Loyalty of " + sett.Name + " set to " + v + ".");
                }
                catch (Exception ex) { HandleError("settlement.loyalty", ex); }
            });
        }

        public void ExecuteSetSecurity()
        {
            var sett = RequireTown(); if (sett == null) return;
            if (!ParsePositiveInt(_securityInput, "security", out int v)) return;

            Confirm("Set security of " + sett.Name + " to " + v + "?", () =>
            {
                try
                {
                    sett.Town.Security = v;
                    Done("Security of " + sett.Name + " set to " + v + ".");
                }
                catch (Exception ex) { HandleError("settlement.security", ex); }
            });
        }

        public void ExecuteFillFood()
        {
            var sett = RequireTown(); if (sett == null) return;

            Confirm("Fill food stocks in " + sett.Name + "?", () =>
            {
                try
                {
                    sett.Town.FoodStocks = sett.Town.FoodStocksUpperLimit();
                    Done("Food stocks in " + sett.Name + " filled to maximum.");
                }
                catch (Exception ex) { HandleError("settlement.food", ex); }
            });
        }

        public void ExecuteReinforceGarrison()
        {
            var sett = RequireTown(); if (sett == null) return;
            if (!ParsePositiveInt(_garrisonInput, "garrison count", out int v)) return;

            var elite = sett.Culture?.EliteBasicTroop;
            var basic = sett.Culture?.BasicTroop;
            string cultureId = sett.Culture != null ? sett.Culture.StringId : null;
            var troop = TroopEquipmentSafety.IsSpawnSafe(elite)
                ? elite
                : TroopEquipmentSafety.IsSpawnSafe(basic)
                    ? basic
                    : TroopEquipmentSafety.ResolveBestArmoredTroop(4, cultureId, TroopRolePreference.Infantry)
                        ?? TroopEquipmentSafety.ResolveBestArmoredTroop(4, null, TroopRolePreference.Infantry);
            if (troop == null) { StatusText = "No troop found for culture of " + sett.Name + "."; return; }

            Confirm("Add " + v + "x " + troop.Name + " to garrison of " + sett.Name + "?", () =>
            {
                try
                {
                    sett.Town.GarrisonParty.AddElementToMemberRoster(troop, v);
                    Done("Added " + v + "x " + troop.Name + " to garrison of " + sett.Name + ".");
                }
                catch (Exception ex) { HandleError("settlement.garrison", ex); }
            });
        }

        public void ExecuteBoostMilitia()
        {
            var sett = Settlement.CurrentSettlement;
            if (sett == null) { StatusText = "Not inside a settlement."; return; }

            Confirm("Add 200 militia to " + sett.Name + "?", () =>
            {
                try
                {
                    sett.Militia = Math.Min(sett.Militia + 200f, 2000f);
                    Done("Militia in " + sett.Name + " boosted to " + (int)sett.Militia + ".");
                }
                catch (Exception ex) { HandleError("settlement.militia", ex); }
            });
        }

        public void ExecuteGiveToClan()
        {
            var sett = RequireTown(); if (sett == null) return;
            var hero = Hero.MainHero;
            if (hero?.Clan == null) { StatusText = "You don't have a clan."; return; }

            Confirm("Transfer " + sett.Name + " to " + hero.Clan.Name + "?", () =>
            {
                try
                {
                    ChangeOwnerOfSettlementAction.ApplyByDefault(hero, sett);
                    Done("Transferred " + sett.Name + " to " + hero.Clan.Name + ".");
                }
                catch (Exception ex) { HandleError("settlement.give", ex); }
            });
        }

        // Quick-preset shortcuts (no input field needed — wired to buttons in XML)
        public void ExecutePresetProsperity1000()  { ProsperityInput = "1000";  ExecuteSetProsperity(); }
        public void ExecutePresetProsperity5000()  { ProsperityInput = "5000";  ExecuteSetProsperity(); }
        public void ExecutePresetProsperity10000() { ProsperityInput = "10000"; ExecuteSetProsperity(); }
        public void ExecutePresetLoyalty50()       { LoyaltyInput    = "50";    ExecuteSetLoyalty(); }
        public void ExecutePresetLoyalty75()       { LoyaltyInput    = "75";    ExecuteSetLoyalty(); }
        public void ExecutePresetLoyalty100()      { LoyaltyInput    = "100";   ExecuteSetLoyalty(); }
        public void ExecutePresetSecurity50()      { SecurityInput   = "50";    ExecuteSetSecurity(); }
        public void ExecutePresetSecurity75()      { SecurityInput   = "75";    ExecuteSetSecurity(); }
        public void ExecutePresetSecurity100()     { SecurityInput   = "100";   ExecuteSetSecurity(); }

        // ── Internal ──────────────────────────────────────────────────────

        private void RefreshInfo()
        {
            try
            {
                var sett = Settlement.CurrentSettlement;
                if (sett == null)
                {
                    SettlementInfo = "Not inside a settlement.\nEnter a town, castle, or village to use these tools.";
                    return;
                }

                var sb = new System.Text.StringBuilder();
                sb.AppendLine(sett.Name.ToString()
                    + "  [" + (sett.IsTown ? "Town" : sett.IsCastle ? "Castle" : "Village") + "]");
                sb.AppendLine("Culture: " + (sett.Culture?.Name?.ToString() ?? "?")
                    + "   Owner: " + (sett.OwnerClan?.Name?.ToString() ?? "Unowned"));

                if (sett.Town != null)
                {
                    sb.AppendLine("Prosperity: " + ((int)sett.Town.Prosperity).ToString("N0")
                        + "   Loyalty: " + sett.Town.Loyalty.ToString("F1")
                        + "   Security: " + sett.Town.Security.ToString("F1"));
                    sb.AppendLine("Food: " + (int)sett.Town.FoodStocks
                        + " / " + (int)sett.Town.FoodStocksUpperLimit()
                        + "   Garrison: " + (sett.Town.GarrisonParty?.MemberRoster?.TotalManCount ?? 0));
                }

                sb.Append("Militia: " + (int)sett.Militia);

                SettlementInfo = sb.ToString();
            }
            catch (Exception ex)
            {
                SettlementInfo = "Error loading settlement info.";
                TAOM_CheatPanel.Runtime.Log.ErrorOnce("settlement.tab.refresh", ex);
            }
        }

        private Settlement RequireTown()
        {
            var sett = Settlement.CurrentSettlement;
            if (sett?.Town == null)
            { StatusText = "This action requires being inside a town or castle."; return null; }
            return sett;
        }

        private bool ParsePositiveInt(string raw, string fieldName, out int result)
        {
            if (!int.TryParse(raw, out result) || result <= 0)
            { StatusText = "Enter a valid positive integer for " + fieldName + "."; return false; }
            return true;
        }

        private void Confirm(string message, Action onConfirm)
        {
            bool skip = false;
            try
            {
                var cfg = TAOM_CheatPanel.Settings.CheatPanelSettings.Instance;
                if (cfg != null) skip = !cfg.ShowConfirmations;
            }
            catch { }

            if (skip) { onConfirm?.Invoke(); return; }
            TAOM_CheatPanel.UI.CheatPanelHost.RequestConfirmation(message, onConfirm);
        }

        private void Done(string msg)
        {
            StatusText = msg;
            TAOM_CheatPanel.Runtime.RecentActionsLog.Record(msg);
            TAOM_CheatPanel.Runtime.Log.Info("[SettlementTab] " + msg);
            RefreshInfo(); // update the stats block
        }

        private void HandleError(string key, Exception ex)
        {
            StatusText = "Error: " + ex.Message;
            TAOM_CheatPanel.Runtime.RecentActionsLog.Record("Settlement control failure :: " + ex.Message);
            TAOM_CheatPanel.Runtime.Log.ErrorOnce(key, ex);
        }
    }
}
