using System;
using System.Collections.Generic;
using System.Reflection;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Roster;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;
using TAOM_CheatPanel.Core;

namespace TAOM_CheatPanel.UI.Tabs
{
    public sealed class SpawnTroopsTabVM : ViewModel
    {
        // ── LOTR faction aliases ──────────────────────────────────────────
        // Troops for these factions are registered under a vanilla culture
        // StringId. The alias table maps the display name shown in the UI
        // to the underlying culture StringId so ResolveTroop works correctly.
        private static readonly Dictionary<string, string> LotrFactionAliases =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        // Reverse map: vanilla culture StringId → LOTR display name override.
        private static readonly Dictionary<string, string> VanillaToLotrName =
            BuildReverseAliasMap();

        private static Dictionary<string, string> BuildReverseAliasMap()
        {
            var d = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var kv in LotrFactionAliases)
                d[kv.Value] = kv.Key;
            return d;
        }

        // ── Fields ────────────────────────────────────────────────────────

        private MBBindingList<CultureOptionVM> _cultures;
        private string _selectedCultureId = "";
        private string _selectedTier = "1";
        private string _troopCount = "20";
        private string _resolvedTroopName = "";
        private string _statusText = "";

        public SpawnTroopsTabVM()
        {
            _cultures = new MBBindingList<CultureOptionVM>();
            LoadCultures();
        }

        // ── Bound properties ──────────────────────────────────────────────

        [DataSourceProperty]
        public MBBindingList<CultureOptionVM> Cultures
        {
            get { return _cultures; }
            set { if (_cultures == value) return; _cultures = value; OnPropertyChangedWithValue(value, nameof(Cultures)); }
        }

        [DataSourceProperty]
        public string SelectedCultureId
        {
            get { return _selectedCultureId; }
            set
            {
                if (_selectedCultureId == value) return;
                _selectedCultureId = value;
                OnPropertyChangedWithValue(value, nameof(SelectedCultureId));
                foreach (var c in _cultures) c.IsSelected = c.CultureId == value;
                ResolvePreview();
            }
        }

        [DataSourceProperty]
        public string SelectedTier
        {
            get { return _selectedTier; }
            set { if (_selectedTier == value) return; _selectedTier = value; OnPropertyChangedWithValue(value, nameof(SelectedTier)); ResolvePreview(); }
        }

        [DataSourceProperty]
        public string TroopCount
        {
            get { return _troopCount; }
            set { if (_troopCount == value) return; _troopCount = value; OnPropertyChangedWithValue(value, nameof(TroopCount)); }
        }

        [DataSourceProperty]
        public string ResolvedTroopName
        {
            get { return _resolvedTroopName; }
            set { if (_resolvedTroopName == value) return; _resolvedTroopName = value; OnPropertyChangedWithValue(value, nameof(ResolvedTroopName)); }
        }

        [DataSourceProperty]
        public string StatusText
        {
            get { return _statusText; }
            set { if (_statusText == value) return; _statusText = value; OnPropertyChangedWithValue(value, nameof(StatusText)); }
        }

        // ── Commands — tier shortcuts ─────────────────────────────────────

        public void ExecuteSetTier1() => SelectedTier = "1";
        public void ExecuteSetTier2() => SelectedTier = "2";
        public void ExecuteSetTier3() => SelectedTier = "3";
        public void ExecuteSetTier4() => SelectedTier = "4";
        public void ExecuteSetTier5() => SelectedTier = "5";

        // ── Commands — select culture by index ────────────────────────────

        public void ExecuteSelectCulture(string cultureId) => SelectedCultureId = cultureId;

        // ── Commands — spawn ──────────────────────────────────────────────

        /// <summary>Add resolved troops directly to the player's party.</summary>
        public void ExecuteAddToParty()
        {
            var troop = ResolveTroop();
            if (troop == null) { StatusText = "No matching troop found for that culture/tier."; return; }
            if (!int.TryParse(_troopCount, out int count) || count <= 0) { StatusText = "Invalid troop count."; return; }

            TAOM_CheatPanel.UI.CheatPanelHost.RequestConfirmation(
                $"Add {count}x {troop.Name} (Tier {troop.Tier}) to your party?",
                () =>
                {
                    try
                    {
                        MobileParty.MainParty.AddElementToMemberRoster(troop, count);
                        var msg = $"Added {count}x {troop.Name} (Tier {troop.Tier}) to your party. Party size: {MobileParty.MainParty.MemberRoster.TotalManCount}";
                        StatusText = msg;
                        TAOM_CheatPanel.Runtime.RecentActionsLog.Record(msg);
                    }
                    catch (Exception ex) { HandleError("spawn.add", ex); }
                }
            );
        }

        /// <summary>Spawn a hostile bandit party near the player (useful for testing battles).</summary>
        public void ExecuteSpawnHostileNearby()
        {
            var troop = ResolveTroop();
            if (troop == null) { StatusText = "No matching troop found for that culture/tier."; return; }
            if (!int.TryParse(_troopCount, out int count) || count <= 0) { StatusText = "Invalid troop count."; return; }

            TAOM_CheatPanel.UI.CheatPanelHost.RequestConfirmation(
                $"Spawn a hostile party of {count}x {troop.Name} near your position?",
                () =>
                {
                    try
                    {
                        var banditClan = Clan.BanditFactions?.FirstOrDefault();
                        if (banditClan == null) { StatusText = "No bandit faction available."; return; }

                        var party = Bannerlord115Compat.CreateBanditParty(
                            "taom_spawn_" + Guid.NewGuid().ToString("N").Substring(0, 8),
                            banditClan,
                            Bannerlord115Compat.GetPosition2D(MobileParty.MainParty));
                        if (party == null)
                        {
                            StatusText = "No compatible bandit party template was available.";
                            return;
                        }

                        var memberRoster = new TroopRoster(party.Party);
                        var prisonerRoster = new TroopRoster(party.Party);
                        memberRoster.AddToCounts(troop, count);

                        // Spawn 1–3 map-units away from the player
                        var offset = new TaleWorlds.Library.Vec2(MBRandom.RandomFloatRanged(-2f, 2f), MBRandom.RandomFloatRanged(-2f, 2f));
                        var spawnPos = Bannerlord115Compat.GetPosition2D(MobileParty.MainParty) + offset;

                        party.InitializeMobilePartyAroundPosition(memberRoster, prisonerRoster, Bannerlord115Compat.ToCampaignPosition(spawnPos), 0.5f, 0.1f, false);
                        TrySetPartyName(party, BuildHostilePartyName(troop, count));
                        CustomPartyAiSafety.TryPatrolAroundSettlement(party, null, "hub.tab.spawn.hostile");

                        var msg = $"Spawned hostile party: {count}x {troop.Name} nearby.";
                        StatusText = msg;
                        TAOM_CheatPanel.Runtime.RecentActionsLog.Record(msg);
                        TAOM_CheatPanel.Runtime.Log.Info("[Spawn] " + msg);
                    }
                    catch (Exception ex) { HandleError("spawn.hostile", ex); }
                }
            );
        }

        // ── Helpers ───────────────────────────────────────────────────────

        private void LoadCultures()
        {
            try
            {
                _cultures.Clear();
                _cultures.Add(new CultureOptionVM("", "Any Culture", ExecuteSelectCulture));

                var cultureIds = CharacterObject.All
                    .Where(c => c != null
                             && !c.IsHero
                             && c.Tier > 0
                             && c.Culture != null
                             && !string.IsNullOrEmpty(c.Culture.StringId))
                    .Select(c => c.Culture.StringId)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(GetCultureDisplayName)
                    .ToList();

                foreach (var cultureId in cultureIds)
                    _cultures.Add(new CultureOptionVM(cultureId, GetCultureDisplayName(cultureId), ExecuteSelectCulture));

                var playerCultureId = Hero.MainHero?.Culture?.StringId;
                if (!string.IsNullOrEmpty(playerCultureId)
                    && cultureIds.Any(id => string.Equals(id, playerCultureId, StringComparison.OrdinalIgnoreCase)))
                    SelectedCultureId = playerCultureId;
                else
                    SelectedCultureId = "";

                ResolvePreview();
            }
            catch (Exception ex)
            {
                StatusText = "Error loading cultures.";
                TAOM_CheatPanel.Runtime.Log.ErrorOnce("spawn.cultures", ex);
            }
        }

        private CharacterObject ResolveTroop()
        {
            if (!int.TryParse(_selectedTier, out int tier)) return null;

            bool anyCulture = string.IsNullOrEmpty(_selectedCultureId);
            return CharacterObject.All
                .Where(c => c != null
                         && !c.IsHero
                         && c.Tier > 0
                         && (anyCulture || string.Equals(c.Culture?.StringId, _selectedCultureId, StringComparison.OrdinalIgnoreCase)))
                .Where(TroopEquipmentSafety.IsSpawnSafe)
                .OrderBy(c => c.Tier == tier ? 0 : (c.Tier < tier ? 1 : 2))
                .ThenBy(c => Math.Abs(c.Tier - tier))
                .ThenBy(c => c.IsInfantry ? 0 : (c.IsRanged ? 1 : (c.IsMounted ? 2 : 3)))
                .ThenBy(c => c.Name?.ToString() ?? c.StringId ?? "")
                .FirstOrDefault();
        }

        private void ResolvePreview()
        {
            var troop = ResolveTroop();
            ResolvedTroopName = troop != null
                ? $"Will use: {troop.Name} (Tier {troop.Tier})"
                : $"No troop found for Tier {_selectedTier} / culture '{_selectedCultureId}'";
        }

        private static string GetCultureDisplayName(string cultureId)
        {
            if (string.IsNullOrEmpty(cultureId)) return "Any Culture";
            if (VanillaToLotrName.TryGetValue(cultureId, out string alias)) return alias;

            var obj = MBObjectManager.Instance?.GetObject<CultureObject>(cultureId);
            var name = obj?.Name?.ToString();
            return !string.IsNullOrEmpty(name) ? name : cultureId;
        }

        private static string BuildHostilePartyName(CharacterObject troop, int count)
        {
            string culture = troop != null && troop.Culture != null ? GetCultureDisplayName(troop.Culture.StringId) : "Road";
            string role = troop != null && troop.IsMounted ? "Outriders" : troop != null && troop.IsRanged ? "Bow-Band" : "Warband";
            string scale = count >= 80 ? "Host" : count >= 35 ? "Company" : "Band";
            return "TAOM " + culture + " " + role + " " + scale;
        }

        private static void TrySetPartyName(MobileParty party, string name)
        {
            if (party == null || string.IsNullOrWhiteSpace(name))
                return;

            try
            {
                MethodInfo method = typeof(MobileParty).GetMethod(
                    "SetCustomName",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                    null,
                    new[] { typeof(TextObject) },
                    null);

                if (method != null)
                {
                    method.Invoke(party, new object[] { new TextObject("{=!}" + name) });
                }
            }
            catch
            {
            }
        }

        private void HandleError(string key, Exception ex)
        {
            StatusText = "Error: " + ex.Message;
            TAOM_CheatPanel.Runtime.Log.ErrorOnce(key, ex);
        }
    }

    public sealed class CultureOptionVM : ViewModel
    {
        private readonly Action<string> _onSelect;
        private bool _isSelected;

        public string CultureId { get; }

        public CultureOptionVM(string cultureId, string displayName, Action<string> onSelect)
        {
            CultureId = cultureId;
            DisplayName = displayName;
            _onSelect = onSelect;
        }

        [DataSourceProperty] public string DisplayName { get; }

        [DataSourceProperty]
        public bool IsSelected
        {
            get { return _isSelected; }
            set { if (_isSelected == value) return; _isSelected = value; OnPropertyChangedWithValue(value, nameof(IsSelected)); }
        }

        public void ExecuteSelect() => _onSelect?.Invoke(CultureId);
    }
}
