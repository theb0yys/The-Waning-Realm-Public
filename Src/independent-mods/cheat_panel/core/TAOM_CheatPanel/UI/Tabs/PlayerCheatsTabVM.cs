using System;
using System.Linq;
using System.Reflection;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TAOM_CheatPanel.Core;

namespace TAOM_CheatPanel.UI.Tabs
{
    /// <summary>
    /// Gauntlet hub tab — massively expanded cheats across 7 sections:
    /// Player Stats · Party & Troops · Companions · Clan & Kingdom ·
    /// Crafting & Materials · Gear & Equipment · World & Campaign.
    /// </summary>
    public sealed class PlayerCheatsTabVM : ViewModel
    {
        // ── Input fields ──────────────────────────────────────────────────
        private string _goldAmount = "10000";
        private string _xpAmount = "5000";
        private string _troopCount = "50";
        private string _troopTier = "3";
        private string _renownAmount = "500";
        private string _influenceAmount = "200";
        private string _attrPoints = "5";
        private string _focusPoints = "5";
        private string _targetAge = "30";
        private string _relationAmount = "50";
        private string _craftQty = "100";
        private string _statusText = "Choose an action.";

        [DataSourceProperty] public string GoldAmount { get { return _goldAmount; } set { if (_goldAmount == value) return; _goldAmount = value; OnPropertyChangedWithValue(value, nameof(GoldAmount)); } }
        [DataSourceProperty] public string XpAmount { get { return _xpAmount; } set { if (_xpAmount == value) return; _xpAmount = value; OnPropertyChangedWithValue(value, nameof(XpAmount)); } }
        [DataSourceProperty] public string TroopCount { get { return _troopCount; } set { if (_troopCount == value) return; _troopCount = value; OnPropertyChangedWithValue(value, nameof(TroopCount)); } }
        [DataSourceProperty] public string TroopTier { get { return _troopTier; } set { if (_troopTier == value) return; _troopTier = value; OnPropertyChangedWithValue(value, nameof(TroopTier)); } }
        [DataSourceProperty] public string RenownAmount { get { return _renownAmount; } set { if (_renownAmount == value) return; _renownAmount = value; OnPropertyChangedWithValue(value, nameof(RenownAmount)); } }
        [DataSourceProperty] public string InfluenceAmount { get { return _influenceAmount; } set { if (_influenceAmount == value) return; _influenceAmount = value; OnPropertyChangedWithValue(value, nameof(InfluenceAmount)); } }
        [DataSourceProperty] public string AttrPoints { get { return _attrPoints; } set { if (_attrPoints == value) return; _attrPoints = value; OnPropertyChangedWithValue(value, nameof(AttrPoints)); } }
        [DataSourceProperty] public string FocusPoints { get { return _focusPoints; } set { if (_focusPoints == value) return; _focusPoints = value; OnPropertyChangedWithValue(value, nameof(FocusPoints)); } }
        [DataSourceProperty] public string TargetAge { get { return _targetAge; } set { if (_targetAge == value) return; _targetAge = value; OnPropertyChangedWithValue(value, nameof(TargetAge)); } }
        [DataSourceProperty] public string RelationAmount { get { return _relationAmount; } set { if (_relationAmount == value) return; _relationAmount = value; OnPropertyChangedWithValue(value, nameof(RelationAmount)); } }
        [DataSourceProperty] public string CraftQty { get { return _craftQty; } set { if (_craftQty == value) return; _craftQty = value; OnPropertyChangedWithValue(value, nameof(CraftQty)); } }

        [DataSourceProperty]
        public string StatusText
        {
            get { return _statusText; }
            set { if (_statusText == value) return; _statusText = value; OnPropertyChangedWithValue(value, nameof(StatusText)); }
        }

        // ═════════════════════════════════════════════════════════════════
        // SECTION 1 — PLAYER STATS
        // ═════════════════════════════════════════════════════════════════

        public void ExecuteAddGold()
        {
            if (!int.TryParse(_goldAmount, out int v) || v <= 0) { SetStatus("Enter a valid positive integer for gold."); return; }
            Confirm("Add " + v.ToString("N0") + " gold?", () =>
            {
                try { Hero.MainHero.ChangeHeroGold(v); Done("Added " + v.ToString("N0") + " gold. Total: " + Hero.MainHero.Gold.ToString("N0")); }
                catch (Exception ex) { Err("cheats.gold", ex); }
            });
        }

        public void ExecuteAddXpAllSkills()
        {
            if (!float.TryParse(_xpAmount, out float v) || v <= 0) { SetStatus("Enter a valid positive number for XP."); return; }
            Confirm("Add " + v.ToString("N0") + " XP to all 18 skills?", () =>
            {
                try
                {
                    var dev = Hero.MainHero.HeroDeveloper;
                    var sk = Skills();
                    foreach (var s in sk) dev.AddSkillXp(s, v, shouldNotify: false, isAffectedByFocusFactor: false);
                    Done("Added " + v.ToString("N0") + " XP to " + sk.Length + " skills.");
                }
                catch (Exception ex) { Err("cheats.xp", ex); }
            });
        }

        public void ExecuteMaxAllSkills()
        {
            Confirm("Max all 18 skills?", () =>
            {
                try
                {
                    var dev = Hero.MainHero.HeroDeveloper;
                    foreach (var s in Skills()) dev.AddSkillXp(s, 999999f, shouldNotify: false, isAffectedByFocusFactor: false);
                    Done("Maximum XP applied to all 18 skills.");
                }
                catch (Exception ex) { Err("cheats.maxskills", ex); }
            });
        }

        public void ExecuteUnlockAllPerks()
        {
            Confirm("Unlock ALL perks for your hero?", () =>
            {
                try
                {
                    var dev = Hero.MainHero.HeroDeveloper;
                    int count = 0;
                    foreach (var p in PerkObject.All.Where(p => p != null))
                        if (!dev.GetPerkValue(p)) { dev.AddPerk(p); count++; }
                    Done("Unlocked " + count + " perk(s).");
                }
                catch (Exception ex) { Err("cheats.perks", ex); }
            });
        }

        public void ExecuteMaxAllAttributes()
        {
            Confirm("Add 30 attribute points + attempt to set all attributes to 10?", () =>
            {
                try
                {
                    var dev = Hero.MainHero.HeroDeveloper;
                    dev.UnspentAttributePoints += 30;
                    // Attempt direct reflection set
                    try
                    {
                        var field = dev.GetType().GetField("_heroAttributes",
                            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        if (field != null)
                        {
                            var dict = field.GetValue(dev);
                            if (dict != null)
                            {
                                var dictType = dict.GetType();
                                var keys = dictType.GetProperty("Keys")?.GetValue(dict) as System.Collections.IEnumerable;
                                var indexer = dictType.GetProperty("Item");
                                if (keys != null && indexer != null)
                                {
                                    foreach (var key in keys)
                                        indexer.SetValue(dict, 10, new[] { key });
                                    dev.UnspentAttributePoints -= 30;
                                    Done("All attributes set to 10 via internal state.");
                                    return;
                                }
                            }
                        }
                    }
                    catch { /* fall through */ }
                    Done("Added 30 attribute points. Spend them in the Attributes screen.");
                }
                catch (Exception ex) { Err("cheats.maxattr", ex); }
            });
        }

        public void ExecuteAddAttributePoints()
        {
            if (!int.TryParse(_attrPoints, out int v) || v <= 0) { SetStatus("Enter a valid positive integer."); return; }
            try
            {
                Hero.MainHero.HeroDeveloper.UnspentAttributePoints += v;
                Done("Added " + v + " attr pts. Unspent: " + Hero.MainHero.HeroDeveloper.UnspentAttributePoints + ".");
            }
            catch (Exception ex) { Err("cheats.attr", ex); }
        }

        public void ExecuteAddFocusPoints()
        {
            if (!int.TryParse(_focusPoints, out int v) || v <= 0) { SetStatus("Enter a valid positive integer."); return; }
            try
            {
                Hero.MainHero.HeroDeveloper.UnspentFocusPoints += v;
                Done("Added " + v + " focus pts. Unspent: " + Hero.MainHero.HeroDeveloper.UnspentFocusPoints + ".");
            }
            catch (Exception ex) { Err("cheats.focus", ex); }
        }

        public void ExecuteSetHeroAge()
        {
            if (!int.TryParse(_targetAge, out int v) || v < 16 || v > 120) { SetStatus("Enter an age between 16 and 120."); return; }
            Confirm("Set hero age to " + v + "?", () =>
            {
                try { Hero.MainHero.SetBirthDay(CampaignTime.Now - CampaignTime.Years(v)); Done("Hero age set to " + v + "."); }
                catch (Exception ex) { Err("cheats.age", ex); }
            });
        }

        public void ExecuteHealHero()
        {
            try { var h = Hero.MainHero; h.HitPoints = h.MaxHitPoints; Done("Healed " + h.Name + " to full HP (" + h.MaxHitPoints + ")."); }
            catch (Exception ex) { Err("cheats.heal", ex); }
        }

        // Gold presets
        // BUG FIX: were writing to backing field directly, bypassing the property setter.
        // OnPropertyChangedWithValue never fired so the UI textbox didn't update before
        // the confirm dialog opened. Use the property so the binding stays in sync.
        public void ExecuteGold1k()   { GoldAmount = "1000";    ExecuteAddGold(); }
        public void ExecuteGold10k()  { GoldAmount = "10000";   ExecuteAddGold(); }
        public void ExecuteGold100k() { GoldAmount = "100000";  ExecuteAddGold(); }
        public void ExecuteGold500k() { GoldAmount = "500000";  ExecuteAddGold(); }
        public void ExecuteGold1m()   { GoldAmount = "1000000"; ExecuteAddGold(); }

        // XP presets — same fix as above.
        public void ExecuteXp5k()   { XpAmount = "5000";   ExecuteAddXpAllSkills(); }
        public void ExecuteXp50k()  { XpAmount = "50000";  ExecuteAddXpAllSkills(); }
        public void ExecuteXp500k() { XpAmount = "500000"; ExecuteAddXpAllSkills(); }

        // Attribute / Focus presets
        public void ExecuteAttr1() { _attrPoints = "1"; ExecuteAddAttributePoints(); }
        public void ExecuteAttr5() { _attrPoints = "5"; ExecuteAddAttributePoints(); }
        public void ExecuteAttr10() { _attrPoints = "10"; ExecuteAddAttributePoints(); }
        public void ExecuteAttr20() { _attrPoints = "20"; ExecuteAddAttributePoints(); }
        public void ExecuteFocus1() { _focusPoints = "1"; ExecuteAddFocusPoints(); }
        public void ExecuteFocus5() { _focusPoints = "5"; ExecuteAddFocusPoints(); }
        public void ExecuteFocus10() { _focusPoints = "10"; ExecuteAddFocusPoints(); }
        public void ExecuteFocus20() { _focusPoints = "20"; ExecuteAddFocusPoints(); }

        // ═════════════════════════════════════════════════════════════════
        // SECTION 2 — PARTY & TROOPS
        // ═════════════════════════════════════════════════════════════════

        public void ExecuteAddCultureTroops()
        {
            if (!int.TryParse(_troopCount, out int count) || count <= 0) { SetStatus("Enter a valid troop count."); return; }
            var cultureId = Hero.MainHero?.Culture?.StringId;
            var basic = Hero.MainHero?.Culture?.BasicTroop;
            var troop = TroopEquipmentSafety.IsSpawnSafe(basic) ? basic : ResolveTroopByTier(1, cultureId);
            if (troop == null) { SetStatus("No basic culture troop found."); return; }
            Confirm("Add " + count + "x " + troop.Name + " (T" + troop.Tier + " culture)?", () =>
            {
                try { MobileParty.MainParty.AddElementToMemberRoster(troop, count); Done("Added " + count + "x " + troop.Name + ". Total: " + MobileParty.MainParty.MemberRoster.TotalManCount + "."); }
                catch (Exception ex) { Err("cheats.troopsculture", ex); }
            });
        }

        public void ExecuteAddTroopsByTier()
        {
            if (!int.TryParse(_troopCount, out int count) || count <= 0) { SetStatus("Enter a valid troop count."); return; }
            if (!int.TryParse(_troopTier, out int tier) || tier < 1 || tier > 6) { SetStatus("Enter a tier between 1 and 6."); return; }
            var troop = ResolveTroopByTier(tier, null);
            if (troop == null) { SetStatus("No T" + tier + " troop found."); return; }
            Confirm("Add " + count + "x " + troop.Name + " (T" + troop.Tier + ")?", () =>
            {
                try { MobileParty.MainParty.AddElementToMemberRoster(troop, count); Done("Added " + count + "x " + troop.Name + " (T" + troop.Tier + ")."); }
                catch (Exception ex) { Err("cheats.troopstier", ex); }
            });
        }

        public void ExecuteAddEliteInfantry()
        {
            if (!int.TryParse(_troopCount, out int count) || count <= 0) { SetStatus("Enter a valid troop count."); return; }
            var t = ResolveTroopByTier(5, null, TroopRolePreference.Infantry);
            if (t == null) { SetStatus("No T5 infantry found."); return; }
            Confirm("Add " + count + "x " + t.Name + " (T" + t.Tier + " infantry)?", () =>
            {
                try { MobileParty.MainParty.AddElementToMemberRoster(t, count); Done("Added " + count + "x " + t.Name + "."); }
                catch (Exception ex) { Err("cheats.elite", ex); }
            });
        }

        public void ExecuteAddVeteranArchers()
        {
            if (!int.TryParse(_troopCount, out int count) || count <= 0) { SetStatus("Enter a valid troop count."); return; }
            var t = ResolveTroopByTier(4, null, TroopRolePreference.Ranged);
            if (t == null) { SetStatus("No T4 archer found."); return; }
            Confirm("Add " + count + "x " + t.Name + " (T4 archers)?", () =>
            {
                try { MobileParty.MainParty.AddElementToMemberRoster(t, count); Done("Added " + count + "x " + t.Name + "."); }
                catch (Exception ex) { Err("cheats.archers", ex); }
            });
        }

        public void ExecuteAddHeavyCavalry()
        {
            if (!int.TryParse(_troopCount, out int count) || count <= 0) { SetStatus("Enter a valid troop count."); return; }
            var t = ResolveTroopByTier(4, null, TroopRolePreference.Mounted);
            if (t == null) { SetStatus("No T4 cavalry found."); return; }
            Confirm("Add " + count + "x " + t.Name + " (T4 cavalry)?", () =>
            {
                try { MobileParty.MainParty.AddElementToMemberRoster(t, count); Done("Added " + count + "x " + t.Name + "."); }
                catch (Exception ex) { Err("cheats.cavalry", ex); }
            });
        }

        public void ExecuteSpawnOptimalArmy()
        {
            Confirm("Spawn optimal mixed army (60 T5 infantry + 30 T4 archers + 20 T4 cavalry)?", () =>
            {
                try
                {
                    var inf = ResolveTroopByTier(5, null, TroopRolePreference.Infantry);
                    var arc = ResolveTroopByTier(4, null, TroopRolePreference.Ranged);
                    var cav = ResolveTroopByTier(4, null, TroopRolePreference.Mounted);
                    int added = 0;
                    var p = MobileParty.MainParty;
                    if (inf != null) { p.AddElementToMemberRoster(inf, 60); added += 60; }
                    if (arc != null) { p.AddElementToMemberRoster(arc, 30); added += 30; }
                    if (cav != null) { p.AddElementToMemberRoster(cav, 20); added += 20; }
                    Done("Spawned " + added + " optimal troops. Party: " + p.MemberRoster.TotalManCount + " total.");
                }
                catch (Exception ex) { Err("cheats.optarmy", ex); }
            });
        }

        public void ExecuteUpgradeAllTroops()
        {
            Confirm("Upgrade all non-hero troops to the top of their upgrade tree?", () =>
            {
                try
                {
                    var roster = MobileParty.MainParty.MemberRoster;
                    var snap = roster.GetTroopRoster().Where(e => !e.Character.IsHero).ToList();
                    int upgraded = 0;
                    foreach (var entry in snap)
                    {
                        var top = GetTop(entry.Character);
                        if (top != null && top != entry.Character)
                        {
                            roster.AddToCounts(entry.Character, -entry.Number);
                            roster.AddToCounts(top, entry.Number);
                            upgraded += entry.Number;
                        }
                    }
                    Done("Upgraded " + upgraded + " troop(s) to top tier.");
                }
                catch (Exception ex) { Err("cheats.upgradeall", ex); }
            });
        }

        public void ExecuteHealParty()
        {
            Confirm("Heal all party members?", () =>
            {
                try
                {
                    // BUG FIX: ExecuteRecoverAllWounded was a near-identical copy of this method
                    // (same roster loop, same wounded-count fix). Collapsed to one shared impl.
                    int count = HealWoundedRoster();
                    Hero.MainHero.HitPoints = Hero.MainHero.MaxHitPoints;
                    Done("Healed party: " + count + " wounded troop(s) recovered.");
                }
                catch (Exception ex) { Err("cheats.healparty", ex); }
            });
        }

        // Delegates to ExecuteHealParty — the two commands did identical work.
        public void ExecuteRecoverAllWounded() => ExecuteHealParty();

        private int HealWoundedRoster()
        {
            int count = 0;
            var roster = MobileParty.MainParty.MemberRoster;
            for (int i = 0; i < roster.Count; i++)
            {
                var el = roster.GetElementCopyAtIndex(i);
                if (el.WoundedNumber > 0) { roster.AddToCounts(el.Character, 0, woundedCount: -el.WoundedNumber); count += el.WoundedNumber; }
            }
            return count;
        }

        public void ExecuteBoostMorale()
        {
            try { MobileParty.MainParty.RecentEventsMorale = 100f; Done("Party morale set to 100."); }
            catch (Exception ex) { Err("cheats.morale", ex); }
        }

        public void ExecuteRestockFood()
        {
            try
            {
                var grain = Game.Current.ObjectManager.GetObject<ItemObject>("grain");
                if (grain == null) { SetStatus("Grain not found."); return; }
                MobileParty.MainParty.ItemRoster.AddToCounts(grain, 200);
                Done("Added 200 grain.");
            }
            catch (Exception ex) { Err("cheats.food", ex); }
        }

        public void ExecuteAddAllFoodTypes()
        {
            try
            {
                var ids = new[] { "grain", "meat", "fish", "vegetables", "butter", "cheese",
                                  "wine", "beer", "olives", "olive_oil", "dates", "bread", "honey" };
                int added = 0;
                var roster = MobileParty.MainParty.ItemRoster;
                foreach (var id in ids)
                {
                    var item = Game.Current.ObjectManager.GetObject<ItemObject>(id);
                    if (item != null) { roster.AddToCounts(item, 100); added++; }
                }
                Done("Added 100 of each food type (" + added + " found).");
            }
            catch (Exception ex) { Err("cheats.allfood", ex); }
        }

        public void ExecuteAddPackHorses()
        {
            try
            {
                var horse = Game.Current.ObjectManager.GetObject<ItemObject>("sumpter_horse");
                if (horse == null) { SetStatus("Sumpter horse not found."); return; }
                MobileParty.MainParty.ItemRoster.AddToCounts(horse, 20);
                Done("Added 20 sumpter horses.");
            }
            catch (Exception ex) { Err("cheats.packhorses", ex); }
        }

        public void ExecuteReleasePrisoners()
        {
            Confirm("Release all prisoners?", () =>
            {
                try
                {
                    int n = MobileParty.MainParty.PrisonRoster.TotalManCount;
                    MobileParty.MainParty.PrisonRoster.Clear();
                    Done("Released " + n + " prisoner(s).");
                }
                catch (Exception ex) { Err("cheats.prisoners", ex); }
            });
        }

        // Troop count presets
        public void ExecuteTroops10() { TroopCount = "10"; }
        public void ExecuteTroops25() { TroopCount = "25"; }
        public void ExecuteTroops50() { TroopCount = "50"; }
        public void ExecuteTroops100() { TroopCount = "100"; }
        public void ExecuteTroops200() { TroopCount = "200"; }
        public void ExecuteTroops500() { TroopCount = "500"; }

        // ═════════════════════════════════════════════════════════════════
        // SECTION 3 — COMPANIONS
        // ═════════════════════════════════════════════════════════════════

        public void ExecuteHealAllCompanions()
        {
            try
            {
                int n = 0;
                foreach (var c in Hero.MainHero.CompanionsInParty.ToList())
                { if (c.HitPoints < c.MaxHitPoints) { c.HitPoints = c.MaxHitPoints; n++; } }
                Done("Healed " + n + " companion(s).");
            }
            catch (Exception ex) { Err("cheats.compheal", ex); }
        }

        public void ExecuteMaxCompanionSkills()
        {
            Confirm("Max all skills for every companion in party?", () =>
            {
                try
                {
                    int n = 0;
                    foreach (var c in Hero.MainHero.CompanionsInParty.ToList())
                    { foreach (var s in Skills()) c.HeroDeveloper.AddSkillXp(s, 999999f, shouldNotify: false, isAffectedByFocusFactor: false); n++; }
                    Done("Maxed all skills for " + n + " companion(s).");
                }
                catch (Exception ex) { Err("cheats.compmax", ex); }
            });
        }

        public void ExecuteLevelAllCompanions()
        {
            Confirm("Add 50,000 XP to all skills for every companion?", () =>
            {
                try
                {
                    int n = 0;
                    foreach (var c in Hero.MainHero.CompanionsInParty.ToList())
                    { foreach (var s in Skills()) c.HeroDeveloper.AddSkillXp(s, 50000f, shouldNotify: false, isAffectedByFocusFactor: false); n++; }
                    Done("Added 50k XP per skill to " + n + " companion(s).");
                }
                catch (Exception ex) { Err("cheats.complevel", ex); }
            });
        }

        public void ExecuteAddAttrToAllCompanions()
        {
            if (!int.TryParse(_attrPoints, out int v) || v <= 0) { SetStatus("Enter a valid attr point amount."); return; }
            try
            {
                int n = 0;
                foreach (var c in Hero.MainHero.CompanionsInParty.ToList())
                { c.HeroDeveloper.UnspentAttributePoints += v; n++; }
                Done("Added " + v + " attr pts to " + n + " companion(s).");
            }
            catch (Exception ex) { Err("cheats.compattr", ex); }
        }

        public void ExecuteAddFocusToAllCompanions()
        {
            if (!int.TryParse(_focusPoints, out int v) || v <= 0) { SetStatus("Enter a valid focus point amount."); return; }
            try
            {
                int n = 0;
                foreach (var c in Hero.MainHero.CompanionsInParty.ToList())
                { c.HeroDeveloper.UnspentFocusPoints += v; n++; }
                Done("Added " + v + " focus pts to " + n + " companion(s).");
            }
            catch (Exception ex) { Err("cheats.compfocus", ex); }
        }

        public void ExecuteUnlockPerksAllCompanions()
        {
            Confirm("Unlock all perks for every companion in party?", () =>
            {
                try
                {
                    int heroes = 0; int perks = 0;
                    foreach (var c in Hero.MainHero.CompanionsInParty.ToList())
                    {
                        var dev = c.HeroDeveloper;
                        foreach (var p in PerkObject.All.Where(p => p != null))
                            if (!dev.GetPerkValue(p)) { dev.AddPerk(p); perks++; }
                        heroes++;
                    }
                    Done("Unlocked " + perks + " perks across " + heroes + " companion(s).");
                }
                catch (Exception ex) { Err("cheats.compperks", ex); }
            });
        }

        // ═════════════════════════════════════════════════════════════════
        // SECTION 4 — CLAN & KINGDOM
        // ═════════════════════════════════════════════════════════════════

        public void ExecuteAddRenown()
        {
            if (!float.TryParse(_renownAmount, out float v) || v <= 0) { SetStatus("Enter a valid positive number for renown."); return; }
            Confirm("Add " + v.ToString("N0") + " renown?", () =>
            {
                try { Hero.MainHero.Clan.AddRenown(v, shouldNotify: true); Done("Added " + v.ToString("N0") + " renown. Total: " + ((int)Hero.MainHero.Clan.Renown).ToString("N0") + "."); }
                catch (Exception ex) { Err("cheats.renown", ex); }
            });
        }

        public void ExecuteAddInfluence()
        {
            if (!float.TryParse(_influenceAmount, out float v) || v <= 0) { SetStatus("Enter a valid positive number for influence."); return; }
            Confirm("Add " + v.ToString("N0") + " influence?", () =>
            {
                try { Hero.MainHero.Clan.Influence += v; Done("Added " + v.ToString("N0") + " influence. Total: " + ((int)Hero.MainHero.Clan.Influence).ToString("N0") + "."); }
                catch (Exception ex) { Err("cheats.influence", ex); }
            });
        }

        public void ExecuteBoostClanTier6()
        {
            Confirm("Add renown to reach Clan Tier 6?", () =>
            {
                try
                {
                    var clan = Hero.MainHero.Clan;
                    float target = 6200f;
                    if (clan.Renown < target) clan.AddRenown(target - clan.Renown, shouldNotify: true);
                    Done("Renown: " + ((int)clan.Renown).ToString("N0") + ". Clan should be Tier 6.");
                }
                catch (Exception ex) { Err("cheats.clantier", ex); }
            });
        }

        public void ExecuteBoostRelationsNotables()
        {
            if (!int.TryParse(_relationAmount, out int v) || v <= 0) { SetStatus("Enter a valid positive integer."); return; }
            Confirm("Boost relations by +" + v + " with all notables?", () =>
            {
                try
                {
                    int n = 0;
                    foreach (var h in Campaign.Current.AliveHeroes.Where(h => h != null && h.IsNotable))
                    { TaleWorlds.CampaignSystem.Actions.ChangeRelationAction.ApplyRelationChangeBetweenHeroes(Hero.MainHero, h, v, false); n++; }
                    Done("+" + v + " relations with " + n + " notables.");
                }
                catch (Exception ex) { Err("cheats.relnotables", ex); }
            });
        }

        public void ExecuteBoostRelationsAllLords()
        {
            if (!int.TryParse(_relationAmount, out int v) || v <= 0) { SetStatus("Enter a valid positive integer."); return; }
            Confirm("Boost relations by +" + v + " with ALL active heroes?", () =>
            {
                try
                {
                    int n = 0;
                    foreach (var h in Campaign.Current.AliveHeroes.Where(h => h != null && h != Hero.MainHero && h.IsActive))
                    { TaleWorlds.CampaignSystem.Actions.ChangeRelationAction.ApplyRelationChangeBetweenHeroes(Hero.MainHero, h, v, false); n++; }
                    Done("+" + v + " relations with " + n + " active heroes.");
                }
                catch (Exception ex) { Err("cheats.relall", ex); }
            });
        }

        public void ExecuteMakePeaceAll()
        {
            Confirm("Make peace with all kingdoms at war with you?", () =>
            {
                try
                {
                    var kingdom = Hero.MainHero?.Clan?.Kingdom;
                    if (kingdom == null) { SetStatus("Not in a kingdom."); return; }
                    int n = 0;
                    foreach (var other in Kingdom.All.ToList())
                    {
                        if (other == kingdom) continue;
                        if (FactionManager.IsAtWarAgainstFaction(kingdom, other))
                        { TaleWorlds.CampaignSystem.Actions.MakePeaceAction.Apply(kingdom, other); n++; }
                    }
                    Done("Made peace with " + n + " kingdom(s).");
                }
                catch (Exception ex) { Err("cheats.peace", ex); }
            });
        }

        public void ExecuteMaxAllOwnedSettlements()
        {
            Confirm("Max prosperity / loyalty / security / food / garrison for ALL your fiefs?", () =>
            {
                try
                {
                    var clan = Hero.MainHero?.Clan;
                    if (clan == null) { SetStatus("No clan."); return; }
                    int n = 0;
                    foreach (var town in clan.Fiefs)
                    {
                        town.Prosperity = Math.Min(town.Prosperity + 5000f, 20000f);
                        town.Loyalty = 100f;
                        town.Security = 100f;
                        town.FoodStocks = town.FoodStocksUpperLimit();
                        var troop = ResolveSafeGarrisonTroop(town.Settlement != null ? town.Settlement.Culture : null);
                        if (troop != null && town.GarrisonParty != null)
                            town.GarrisonParty.AddElementToMemberRoster(troop, 50);
                        n++;
                    }
                    Done("Maxed all stats for " + n + " owned fief(s).");
                }
                catch (Exception ex) { Err("cheats.allsett", ex); }
            });
        }

        // Renown / influence presets
        public void ExecuteRenown100() { _renownAmount = "100"; ExecuteAddRenown(); }
        public void ExecuteRenown500() { _renownAmount = "500"; ExecuteAddRenown(); }
        public void ExecuteRenown1000() { _renownAmount = "1000"; ExecuteAddRenown(); }
        public void ExecuteRenown5000() { _renownAmount = "5000"; ExecuteAddRenown(); }
        public void ExecuteInf50() { _influenceAmount = "50"; ExecuteAddInfluence(); }
        public void ExecuteInf200() { _influenceAmount = "200"; ExecuteAddInfluence(); }
        public void ExecuteInf500() { _influenceAmount = "500"; ExecuteAddInfluence(); }
        public void ExecuteInf1000() { _influenceAmount = "1000"; ExecuteAddInfluence(); }

        // ═════════════════════════════════════════════════════════════════
        // SECTION 5 — CRAFTING & MATERIALS
        // ═════════════════════════════════════════════════════════════════

        public void ExecuteRestoreSmithingStamina()
        {
            try
            {
                var craftType = ResolveLoadedType(
                    "TaleWorlds.CampaignSystem.CampaignBehaviors.CraftingCampaignBehavior, TaleWorlds.CampaignSystem");
                if (craftType != null)
                {
                    var getCB = typeof(Campaign).GetMethod("GetCampaignBehavior")
                        ?.MakeGenericMethod(craftType);
                    var beh = getCB?.Invoke(Campaign.Current, null);
                    var field = craftType.GetField("_heroCraftingStamina",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (beh != null && field != null)
                    {
                        var dict = field.GetValue(beh)
                            as System.Collections.Generic.Dictionary<Hero, int>;
                        if (dict != null)
                        {
                            var heroes = MobileParty.MainParty.MemberRoster.GetTroopRoster()
                                .Where(e => e.Character?.IsHero == true && e.Character.HeroObject != null)
                                .Select(e => e.Character.HeroObject).ToList();
                            foreach (var h in heroes) dict[h] = 100;
                            Done("Smithing stamina restored for " + heroes.Count + " hero(es).");
                            return;
                        }
                    }
                }
                SetStatus("CraftingCampaignBehavior not found.");
            }
            catch (Exception ex) { Err("cheats.smithstamina", ex); }
        }

        public void ExecuteAddIronOre() => AddMat("iron", 200);
        public void ExecuteAddCrudeIron() => AddMat("crude_iron", 200);
        public void ExecuteAddWroughtIron() => AddMat("wrought_iron", 200);
        public void ExecuteAddCoal() => AddMat("coal", 200);
        public void ExecuteAddHardwood() => AddMat("hardwood", 200);
        public void ExecuteAddSteel() => AddMat("steel", 150);
        public void ExecuteAddFineSteel() => AddMat("fine_steel", 100);
        public void ExecuteAddThamaskeneSteel() => AddMat("thamaskene_steel", 50);
        public void ExecuteAddSilverOre() => AddMat("silver_ore", 100);

        public void ExecuteAddAllCraftingMaterials()
        {
            Confirm("Add full stacks of all smithing materials?", () =>
            {
                try
                {
                    int q = int.TryParse(_craftQty, out int v) && v > 0 ? v : 100;
                    var mats = new (string id, int qty)[]
                    {
                        ("iron",            q*2), ("crude_iron",   q*2), ("wrought_iron", q*2),
                        ("coal",            q*2), ("hardwood",     q*2), ("steel",        q),
                        ("fine_steel",      q),   ("silver_ore",   q),   ("thamaskene_steel", q/2),
                    };
                    int added = 0;
                    var roster = MobileParty.MainParty.ItemRoster;
                    foreach (var (id, qty) in mats)
                    {
                        var item = Game.Current.ObjectManager.GetObject<ItemObject>(id);
                        if (item != null) { roster.AddToCounts(item, qty); added++; }
                    }
                    Done("Added all smithing materials (" + added + " types found).");
                }
                catch (Exception ex) { Err("cheats.allmat", ex); }
            });
        }

        // ═════════════════════════════════════════════════════════════════
        // SECTION 6 — GEAR & EQUIPMENT
        // ═════════════════════════════════════════════════════════════════

        public void ExecuteAddBestOneHanded() => AddBestItem(ItemObject.ItemTypeEnum.OneHandedWeapon, "one-handed weapon");
        public void ExecuteAddBestTwoHanded() => AddBestItem(ItemObject.ItemTypeEnum.TwoHandedWeapon, "two-handed weapon");
        public void ExecuteAddBestPolearm() => AddBestItem(ItemObject.ItemTypeEnum.Polearm, "polearm");
        public void ExecuteAddBestBow() => AddBestItem(ItemObject.ItemTypeEnum.Bow, "bow");
        public void ExecuteAddBestCrossbow() => AddBestItem(ItemObject.ItemTypeEnum.Crossbow, "crossbow");
        public void ExecuteAddBestThrown() => AddBestItem(ItemObject.ItemTypeEnum.Thrown, "thrown weapon");
        public void ExecuteAddBestShield() => AddBestItem(ItemObject.ItemTypeEnum.Shield, "shield");
        public void ExecuteAddBestBodyArmor() => AddBestItem(ItemObject.ItemTypeEnum.BodyArmor, "body armor");
        public void ExecuteAddBestHelmet() => AddBestItem(ItemObject.ItemTypeEnum.HeadArmor, "helmet");
        public void ExecuteAddBestGloves() => AddBestItem(ItemObject.ItemTypeEnum.HandArmor, "gloves");
        public void ExecuteAddBestBoots() => AddBestItem(ItemObject.ItemTypeEnum.LegArmor, "boots");

        public void ExecuteAddBestHorse()
        {
            try
            {
                var horse = Game.Current.ObjectManager.GetObjectTypeList<ItemObject>()
                    .Where(i => i != null && i.HasHorseComponent && i.HorseComponent != null && !i.IsAnimal)
                    .OrderByDescending(i => i.Value)
                    .FirstOrDefault();
                if (horse == null) { SetStatus("No horse found."); return; }
                MobileParty.MainParty.ItemRoster.AddToCounts(horse, 1);
                Done("Added: " + horse.Name + " (value " + horse.Value.ToString("N0") + ").");
            }
            catch (Exception ex) { Err("cheats.horse", ex); }
        }

        public void ExecuteAddAllTradeGoods()
        {
            Confirm("Add 100 of every trade good?", () =>
            {
                try
                {
                    var goods = Game.Current.ObjectManager.GetObjectTypeList<ItemObject>()
                        .Where(i => i != null && i.ItemType == ItemObject.ItemTypeEnum.Goods)
                        .ToList();
                    var roster = MobileParty.MainParty.ItemRoster;
                    foreach (var item in goods) roster.AddToCounts(item, 100);
                    Done("Added 100 of each trade good (" + goods.Count + " types).");
                }
                catch (Exception ex) { Err("cheats.tradegoods", ex); }
            });
        }

        // ═════════════════════════════════════════════════════════════════
        // SECTION 7 — WORLD & CAMPAIGN
        // ═════════════════════════════════════════════════════════════════

        public void ExecuteHealAllHeroesGlobal()
        {
            Confirm("Heal ALL living heroes globally?", () =>
            {
                try
                {
                    int n = 0;
                    foreach (var h in Campaign.Current.AliveHeroes.Where(h => h != null && h.HitPoints < h.MaxHitPoints))
                    { h.HitPoints = h.MaxHitPoints; n++; }
                    Done("Healed " + n + " hero(es) globally.");
                }
                catch (Exception ex) { Err("world.healall", ex); }
            });
        }

        public void ExecuteDiscoverAllHideouts()
        {
            try
            {
                int n = 0;
                foreach (var s in Settlement.All.Where(s => s != null && s.IsHideout))
                { s.IsVisible = true; n++; }
                Done("Revealed " + n + " hideout(s) on the map.");
            }
            catch (Exception ex) { Err("world.hideouts", ex); }
        }

        public void ExecuteClearNearbyBandits()
        {
            Confirm("Destroy all bandit parties within range 50?", () =>
            {
                try
                {
                    var pos = Bannerlord115Compat.GetPosition2D(MobileParty.MainParty);
                    var bandits = MobileParty.All
                        .Where(p => p != null && p.IsActive && p.IsBandit && Bannerlord115Compat.GetPosition2D(p).Distance(pos) < 50f)
                        .ToList();
                    int n = 0;
                    foreach (var b in bandits)
                    {
                        try { TaleWorlds.CampaignSystem.Actions.DestroyPartyAction.Apply(MobileParty.MainParty.Party, b); n++; }
                        catch { /* party already destroyed */ }
                    }
                    Done("Destroyed " + n + " bandit party/parties.");
                }
                catch (Exception ex) { Err("world.bandits", ex); }
            });
        }

        public void ExecuteCaptureNearestEnemySettlement()
        {
            Confirm("Capture the nearest enemy town or castle for your clan?", () =>
            {
                try
                {
                    var pos = Bannerlord115Compat.GetPosition2D(MobileParty.MainParty);
                    var target = Settlement.All
                        .Where(s => s != null && (s.IsTown || s.IsCastle)
                                 && s.MapFaction != null
                                 && s.MapFaction != Hero.MainHero.MapFaction)
                        .OrderBy(s => Bannerlord115Compat.GetPosition2D(s).Distance(pos))
                        .FirstOrDefault();
                    if (target == null) { SetStatus("No enemy settlement found."); return; }
                    TaleWorlds.CampaignSystem.Actions.ChangeOwnerOfSettlementAction.ApplyByDefault(Hero.MainHero, target);
                    Done("Captured " + target.Name + " for " + (Hero.MainHero.Clan?.Name ?? Hero.MainHero.Name) + ".");
                }
                catch (Exception ex) { Err("world.capture", ex); }
            });
        }

        public void ExecuteMaxAllOwnedSettlementsWorld()
        {
            ExecuteMaxAllOwnedSettlements(); // reuse clan section logic
        }

        // ═════════════════════════════════════════════════════════════════
        // HELPERS
        // ═════════════════════════════════════════════════════════════════

        private void AddMat(string id, int qty)
        {
            try
            {
                int q = int.TryParse(_craftQty, out int v) && v > 0 ? v : qty;
                var item = Game.Current.ObjectManager.GetObject<ItemObject>(id);
                if (item == null) { SetStatus(id + " not found in object manager."); return; }
                MobileParty.MainParty.ItemRoster.AddToCounts(item, q);
                Done("Added " + q + "x " + item.Name + ".");
            }
            catch (Exception ex) { Err("cheats.mat." + id, ex); }
        }

        private void AddBestItem(ItemObject.ItemTypeEnum type, string label)
        {
            try
            {
                var item = Game.Current.ObjectManager.GetObjectTypeList<ItemObject>()
                    .Where(i => i != null && i.ItemType == type)
                    .OrderByDescending(i => i.Value)
                    .FirstOrDefault();
                if (item == null) { SetStatus("No " + label + " found."); return; }
                MobileParty.MainParty.ItemRoster.AddToCounts(item, 1);
                Done("Added: " + item.Name + " (" + label + ", " + item.Value.ToString("N0") + " gold).");
            }
            catch (Exception ex) { Err("cheats.gear." + label, ex); }
        }

        private static CharacterObject GetTop(CharacterObject t)
        {
            return TroopEquipmentSafety.ResolveSafeUpgradeTarget(t);
        }

        private static CharacterObject ResolveTroopByTier(int tier, string cultureId, TroopRolePreference preference = TroopRolePreference.Any)
        {
            return TroopEquipmentSafety.ResolveBestArmoredTroop(tier, cultureId, preference);
        }

        private static CharacterObject ResolveSafeGarrisonTroop(CultureObject culture)
        {
            var elite = culture != null ? culture.EliteBasicTroop : null;
            var basic = culture != null ? culture.BasicTroop : null;
            string cultureId = culture != null ? culture.StringId : null;
            return TroopEquipmentSafety.IsSpawnSafe(elite)
                ? elite
                : TroopEquipmentSafety.IsSpawnSafe(basic)
                    ? basic
                    : TroopEquipmentSafety.ResolveBestArmoredTroop(4, cultureId, TroopRolePreference.Infantry)
                        ?? TroopEquipmentSafety.ResolveBestArmoredTroop(4, null, TroopRolePreference.Infantry);
        }

        private static SkillObject[] Skills() => new[]
        {
            DefaultSkills.OneHanded,  DefaultSkills.TwoHanded,  DefaultSkills.Polearm,
            DefaultSkills.Bow,        DefaultSkills.Crossbow,   DefaultSkills.Throwing,
            DefaultSkills.Riding,     DefaultSkills.Athletics,  DefaultSkills.Crafting,
            DefaultSkills.Tactics,    DefaultSkills.Scouting,   DefaultSkills.Roguery,
            DefaultSkills.Charm,      DefaultSkills.Leadership, DefaultSkills.Trade,
            DefaultSkills.Steward,    DefaultSkills.Medicine,   DefaultSkills.Engineering
        };

        private void Confirm(string message, Action onConfirm)
        {
            bool skip = false;
            try { var cfg = TAOM_CheatPanel.Settings.CheatPanelSettings.Instance; if (cfg != null) skip = !cfg.ShowConfirmations; } catch { }
            if (skip) { onConfirm?.Invoke(); return; }
            TAOM_CheatPanel.UI.CheatPanelHost.RequestConfirmation(message, onConfirm);
        }

        private void Done(string msg) { StatusText = msg; TAOM_CheatPanel.Runtime.RecentActionsLog.Record(msg); TAOM_CheatPanel.Runtime.Log.Info("[PlayerCheats] " + msg); }
        private void SetStatus(string msg) { StatusText = msg; TAOM_CheatPanel.Runtime.Log.Info("[PlayerCheats] " + msg); }
        private void Err(string key, Exception ex) { SetStatus("Error: " + ex.Message + " (see log)"); TAOM_CheatPanel.Runtime.Log.ErrorOnce(key, ex); }

        private static Type ResolveLoadedType(string assemblyQualifiedTypeName)
        {
            if (string.IsNullOrWhiteSpace(assemblyQualifiedTypeName))
                return null;

            string typeName = assemblyQualifiedTypeName;
            string assemblyName = null;
            int comma = typeName.IndexOf(',');
            if (comma >= 0)
            {
                assemblyName = typeName.Substring(comma + 1).Trim();
                typeName = typeName.Substring(0, comma).Trim();
            }

            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly == null)
                    continue;

                try
                {
                    if (!string.IsNullOrWhiteSpace(assemblyName))
                    {
                        AssemblyName name = assembly.GetName();
                        if (name == null || !string.Equals(name.Name, assemblyName, StringComparison.Ordinal))
                            continue;
                    }

                    Type type = assembly.GetType(typeName, false);
                    if (type != null)
                        return type;
                }
                catch
                {
                }
            }

            return null;
        }
    }
}
