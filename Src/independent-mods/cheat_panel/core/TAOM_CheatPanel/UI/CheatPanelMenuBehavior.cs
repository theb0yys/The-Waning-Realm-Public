using SupportRuntime = TAOM_CheatPanel.Runtime;
using Settings = TAOM_CheatPanel.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;
using TAOM_CheatPanel.Core;

namespace TAOM_CheatPanel.UI
{
    public sealed class CheatPanelMenuBehavior : CampaignBehaviorBase
    {
        // ── Shared state ──────────────────────────────────────────────────
        internal static int GoldAmount = 10000;
        internal static int XpAmount = 5000;
        internal static int TroopCount = 50;
        internal static int RenownAmount = 500;
        internal static int InfluenceAmount = 200;
        internal static int SpawnTier = 3;
        internal static string SpawnCultureId = "";   // "" = any culture
        internal static string LastStatus = "";
        private bool _menusRegistered;
        private static string _returnMenu;
        private static Settlement _returnSettlement;
        // LOTR factions whose troops live under a vanilla culture StringId.
        private static readonly Dictionary<string, string> VanillaToLotrName =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        // Settlement tools
        internal static int ProsperityAmount = 1000;
        internal static int LoyaltyAmount = 75;
        internal static int SecurityAmount = 75;

        private static readonly int[] GoldPresets = { 1000, 5000, 10000, 50000, 100000 };
        private static readonly int[] XpPresets = { 1000, 5000, 10000, 50000 };
        private static readonly int[] TroopPresets = { 10, 25, 50, 100, 200 };
        private static readonly int[] RenownPresets = { 100, 500, 1000, 5000 };
        private static readonly int[] InfluencePresets = { 50, 200, 500, 1000 };
        private static readonly int[] ProsperityPresets = { 100, 500, 1000, 5000, 10000 };
        private static readonly int[] LoyaltyPresets = { 25, 50, 75, 100 };
        private static readonly int[] SecurityPresets = { 25, 50, 75, 100 };

        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
        }

        public override void SyncData(IDataStore dataStore) { }

        private void OnSessionLaunched(CampaignGameStarter starter)
        {
            _returnMenu = null;
            _returnSettlement = null;
            LastStatus = "";
            SupportRuntime.RecentActionsLog.Clear();
            SupportRuntime.Log.Reset();
            SpawnCultureId = "";

            var cfg = Cfg();
            if (cfg != null)
            {
                GoldAmount = cfg.DefaultGold;
                XpAmount = cfg.DefaultXp;
                TroopCount = cfg.DefaultTroopCount;
                RenownAmount = cfg.DefaultRenown;
                InfluenceAmount = cfg.DefaultInfluence;
                SpawnTier = cfg.DefaultSpawnTier;
            }

            if (starter == null)
                return;

            if (_menusRegistered) return;
            InjectSettlementEntries(starter);
            RegisterHubMenus(starter);
            CheatPanelSettingsMenu.Register(starter);
            _menusRegistered = true;
        }

        private static void InjectSettlementEntries(CampaignGameStarter s)
        {
            Opt(s, "town", "taom_cheat_panel_entry_town",
                "{=!}[TAOM] Cheat Panel",
                CanOpenTownHubEntry,
                OpenHubRoot,
                false, 99);

            Opt(s, "castle_outside", "taom_cheat_panel_entry_castle",
                "{=!}[TAOM] Cheat Panel",
                CanOpenCastleHubEntry,
                OpenHubRoot,
                false, 99);

            Opt(s, "village", "taom_cheat_panel_entry_village",
                "{=!}[TAOM] Cheat Panel",
                CanOpenVillageHubEntry,
                OpenHubRoot,
                false, 99);
        }

        private static void RegisterHubMenus(CampaignGameStarter s)
        {
            s.AddGameMenu("taom_cheat_panel_root", "{TAOM_CHEAT_PANEL_TITLE}", OnInitRoot);
            Opt(s, "taom_cheat_panel_root", "cp_settings", "{=!}Settings", Always, _ => GameMenu.SwitchToMenu("taom_cheat_panel_settings"), false, 9);
            Opt(s, "taom_cheat_panel_root", "hub_cheats", "{=!}Player Cheats", Always, _ => GameMenu.SwitchToMenu("taom_cheat_panel_cheats"), false, 0);
            Opt(s, "taom_cheat_panel_root", "hub_adjustments", "{=!}Cheat Adjustments", Always, _ => GameMenu.SwitchToMenu("taom_cheat_panel_adjustments"), false, 1);
            Opt(s, "taom_cheat_panel_root", "hub_time", "{=!}Time & Speed", Always, _ => GameMenu.SwitchToMenu("taom_cheat_panel_time"), false, 2);
            Opt(s, "taom_cheat_panel_root", "hub_teleport", "{=!}Teleport", Always, _ => GameMenu.SwitchToMenu("taom_cheat_panel_teleport"), false, 3);
            Opt(s, "taom_cheat_panel_root", "hub_spawn", "{=!}Spawn Troops", Always, _ => GameMenu.SwitchToMenu("taom_cheat_panel_spawn"), false, 4);
            Opt(s, "taom_cheat_panel_root", "hub_settlement", "{=!}Settlement Tools", CondSettlementToolsEnabled, _ => GameMenu.SwitchToMenu("taom_cheat_panel_settlement"), false, 5);
            Opt(s, "taom_cheat_panel_root", "hub_kingdom", "{=!}Kingdom Tools", Always, _ => GameMenu.SwitchToMenu("taom_cheat_panel_kingdom"), false, 6);
            Opt(s, "taom_cheat_panel_root", "hub_history", "{=!}Action History", Always, _ => GameMenu.SwitchToMenu("taom_cheat_panel_history"), false, 7);
            Opt(s, "taom_cheat_panel_root", "hub_close", "{=!}Close Cheat Panel", Always, _ => CloseHub(), true, 8);

            RegisterCheatsMenus(s);
            RegisterAdjustmentsMenu(s);
            RegisterTimeMenu(s);
            RegisterTeleportMenus(s);
            RegisterSpawnMenus(s);
            RegisterSettlementMenu(s);
            RegisterKingdomMenu(s);
            RegisterHistoryMenu(s);
        }

        private static void RegisterCheatsMenus(CampaignGameStarter s)
        {
            // Root cheats — 7 category sub-menus
            s.AddGameMenu("taom_cheat_panel_cheats", "{TAOM_CHEAT_PANEL_CHEATS_TEXT}", OnInitCheats);
            Opt(s, "taom_cheat_panel_cheats", "c_player", "{=!}Player Stats", Always, _ => GameMenu.SwitchToMenu("taom_cheat_panel_cheats_player"), false, 0);
            Opt(s, "taom_cheat_panel_cheats", "c_party", "{=!}Party & Troops", Always, _ => GameMenu.SwitchToMenu("taom_cheat_panel_cheats_party"), false, 1);
            Opt(s, "taom_cheat_panel_cheats", "c_companions", "{=!}Companions", Always, _ => GameMenu.SwitchToMenu("taom_cheat_panel_cheats_companions"), false, 2);
            Opt(s, "taom_cheat_panel_cheats", "c_clan", "{=!}Clan & Kingdom", Always, _ => GameMenu.SwitchToMenu("taom_cheat_panel_cheats_clan"), false, 3);
            Opt(s, "taom_cheat_panel_cheats", "c_crafting", "{=!}Crafting & Materials", Always, _ => GameMenu.SwitchToMenu("taom_cheat_panel_cheats_crafting"), false, 4);
            Opt(s, "taom_cheat_panel_cheats", "c_gear", "{=!}Gear & Equipment", Always, _ => GameMenu.SwitchToMenu("taom_cheat_panel_cheats_gear"), false, 5);
            Opt(s, "taom_cheat_panel_cheats", "c_world", "{=!}World & Campaign", Always, _ => GameMenu.SwitchToMenu("taom_cheat_panel_cheats_world"), false, 6);
            Opt(s, "taom_cheat_panel_cheats", "c_back", "{=!}Back", Always, _ => BackToRoot(), true, 7);

            // ══ Player Stats ══════════════════════════════════════════════
            s.AddGameMenu("taom_cheat_panel_cheats_player", "{TAOM_CHEAT_PANEL_CHEATS_PLAYER_TEXT}", OnInitCheatsPlayer);
            Opt(s, "taom_cheat_panel_cheats_player", "cp_gold_set", "{=!}Set Gold Amount", Always, _ => GameMenu.SwitchToMenu("taom_cheat_panel_cheats_gold"), false, 0);
            Opt(s, "taom_cheat_panel_cheats_player", "cp_gold_add", "{=!}Add Gold", Always, _ => DoAddGold(), false, 1, requiresConfirmation: true);
            Opt(s, "taom_cheat_panel_cheats_player", "cp_gold_1m", "{=!}Add 1,000,000 Gold", Always, _ => DoAddExactGold(1000000), false, 2, requiresConfirmation: true);
            Opt(s, "taom_cheat_panel_cheats_player", "cp_xp_set", "{=!}Set XP Amount", Always, _ => GameMenu.SwitchToMenu("taom_cheat_panel_cheats_xp"), false, 3);
            Opt(s, "taom_cheat_panel_cheats_player", "cp_xp_add", "{=!}Add XP (all skills)", Always, _ => DoAddXp(), false, 4, requiresConfirmation: true);
            Opt(s, "taom_cheat_panel_cheats_player", "cp_xp_max", "{=!}Max All Skills", Always, _ => DoMaxAllSkills(), false, 5, requiresConfirmation: true);
            Opt(s, "taom_cheat_panel_cheats_player", "cp_perks", "{=!}Unlock All Perks", Always, _ => DoAddAllPerks(), false, 6, requiresConfirmation: true);
            Opt(s, "taom_cheat_panel_cheats_player", "cp_maxattr", "{=!}Max All Attributes", Always, _ => DoMaxAllAttributes(), false, 7, requiresConfirmation: true);
            Opt(s, "taom_cheat_panel_cheats_player", "cp_attr", "{=!}Add Attribute Points", Always, _ => GameMenu.SwitchToMenu("taom_cheat_panel_cheats_attr"), false, 8);
            Opt(s, "taom_cheat_panel_cheats_player", "cp_focus", "{=!}Add Focus Points", Always, _ => GameMenu.SwitchToMenu("taom_cheat_panel_cheats_focus"), false, 9);
            Opt(s, "taom_cheat_panel_cheats_player", "cp_heal", "{=!}Heal Hero to Full HP", Always, _ => DoHeal(), false, 10, requiresConfirmation: true);
            Opt(s, "taom_cheat_panel_cheats_player", "cp_age_set", "{=!}Set Hero Age", Always, _ => GameMenu.SwitchToMenu("taom_cheat_panel_cheats_age"), false, 11);
            Opt(s, "taom_cheat_panel_cheats_player", "cp_back", "{=!}Back", Always, _ => GameMenu.SwitchToMenu("taom_cheat_panel_cheats"), true, 12);

            BuildPresetMenu(s, "taom_cheat_panel_cheats_gold", "taom_cheat_panel_cheats_player", GoldPresets, "gold", "N0", v => GoldAmount = v);
            BuildPresetMenu(s, "taom_cheat_panel_cheats_xp", "taom_cheat_panel_cheats_player", XpPresets, "XP", "N0", v => XpAmount = v);
            BuildPresetMenu(s, "taom_cheat_panel_cheats_attr", "taom_cheat_panel_cheats_player", new[] { 1, 3, 5, 10, 20 }, "points", "", DoAddAttributePoints);
            BuildPresetMenu(s, "taom_cheat_panel_cheats_focus", "taom_cheat_panel_cheats_player", new[] { 1, 3, 5, 10, 20 }, "points", "", DoAddFocusPoints);
            BuildPresetMenu(s, "taom_cheat_panel_cheats_age", "taom_cheat_panel_cheats_player", new[] { 18, 25, 30, 35, 40, 50, 60 }, "years", "", DoSetHeroAge);

            // ══ Party & Troops ════════════════════════════════════════════
            s.AddGameMenu("taom_cheat_panel_cheats_party", "{TAOM_CHEAT_PANEL_CHEATS_PARTY_TEXT}", OnInitCheatsParty);
            Opt(s, "taom_cheat_panel_cheats_party", "pp_count", "{=!}Set Troop Count", Always, _ => GameMenu.SwitchToMenu("taom_cheat_panel_cheats_troops"), false, 0);
            Opt(s, "taom_cheat_panel_cheats_party", "pp_cult", "{=!}Add Culture Troops (T1)", Always, _ => DoAddTroops(), false, 1, requiresConfirmation: true);
            Opt(s, "taom_cheat_panel_cheats_party", "pp_tier", "{=!}Set Spawn Tier", Always, _ => GameMenu.SwitchToMenu("taom_cheat_panel_cheats_tier"), false, 2);
            Opt(s, "taom_cheat_panel_cheats_party", "pp_spawn", "{=!}Add Troops (Selected Tier)", Always, _ => DoAddTroopsByTier(), false, 3, requiresConfirmation: true);
            Opt(s, "taom_cheat_panel_cheats_party", "pp_elite", "{=!}Add Elite Infantry (T5)", Always, _ => DoAddEliteTroops(), false, 4, requiresConfirmation: true);
            Opt(s, "taom_cheat_panel_cheats_party", "pp_ranged", "{=!}Add Veteran Archers (T4)", Always, _ => DoAddRangedTroops(), false, 5, requiresConfirmation: true);
            Opt(s, "taom_cheat_panel_cheats_party", "pp_cavalry", "{=!}Add Heavy Cavalry (T4)", Always, _ => DoAddCavalry(), false, 6, requiresConfirmation: true);
            Opt(s, "taom_cheat_panel_cheats_party", "pp_optimal", "{=!}Spawn Optimal Army (Mixed)", Always, _ => DoSpawnOptimalArmy(), false, 7, requiresConfirmation: true);
            Opt(s, "taom_cheat_panel_cheats_party", "pp_upgrade", "{=!}Upgrade All Troops (Max Tier)", Always, _ => DoUpgradeAllTroops(), false, 8, requiresConfirmation: true);
            Opt(s, "taom_cheat_panel_cheats_party", "pp_heal", "{=!}Heal Entire Party", Always, _ => DoHealParty(), false, 9, requiresConfirmation: true);
            Opt(s, "taom_cheat_panel_cheats_party", "pp_wounded", "{=!}Recover All Wounded", Always, _ => DoRecoverWoundedParty(), false, 10, requiresConfirmation: true);
            Opt(s, "taom_cheat_panel_cheats_party", "pp_morale", "{=!}Boost Morale to Max", Always, _ => DoBoostMorale(), false, 11, requiresConfirmation: true);
            Opt(s, "taom_cheat_panel_cheats_party", "pp_food", "{=!}Restock Party Food (Grain)", Always, _ => DoRestockFood(), false, 12, requiresConfirmation: true);
            Opt(s, "taom_cheat_panel_cheats_party", "pp_allfood", "{=!}Add All Food Types", Always, _ => DoAddAllFoodTypes(), false, 13, requiresConfirmation: true);
            Opt(s, "taom_cheat_panel_cheats_party", "pp_pack", "{=!}Add Pack Animals", Always, _ => DoAddPackAnimals(), false, 14, requiresConfirmation: true);
            Opt(s, "taom_cheat_panel_cheats_party", "pp_warhorses", "{=!}Add War Horses", Always, _ => DoAddWarHorses(), false, 15, requiresConfirmation: true);
            Opt(s, "taom_cheat_panel_cheats_party", "pp_speed", "{=!}Boost Party Speed", Always, _ => DoBoostPartySpeed(), false, 16, requiresConfirmation: true);
            Opt(s, "taom_cheat_panel_cheats_party", "pp_prisoners", "{=!}Release All Prisoners", Always, _ => DoReleasePrisoners(), false, 17, requiresConfirmation: true);
            Opt(s, "taom_cheat_panel_cheats_party", "pp_back", "{=!}Back", Always, _ => GameMenu.SwitchToMenu("taom_cheat_panel_cheats"), true, 18);

            BuildPresetMenu(s, "taom_cheat_panel_cheats_troops", "taom_cheat_panel_cheats_party", TroopPresets, "troops", "", v => TroopCount = v);

            s.AddGameMenu("taom_cheat_panel_cheats_tier", "{=!}Select troop tier for spawn:", _ => { });
            for (int t = 1; t <= 5; t++)
            { var c = t; Opt(s, "taom_cheat_panel_cheats_tier", "ctier_" + c, "{=!}Tier " + c, Always, _ => { SpawnTier = c; GameMenu.SwitchToMenu("taom_cheat_panel_cheats_party"); }, false, c - 1); }
            Opt(s, "taom_cheat_panel_cheats_tier", "ctier_back", "{=!}Back", Always, _ => GameMenu.SwitchToMenu("taom_cheat_panel_cheats_party"), true, 5);

            // ══ Companions ════════════════════════════════════════════════
            s.AddGameMenu("taom_cheat_panel_cheats_companions", "{TAOM_CHEAT_PANEL_CHEATS_COMP_TEXT}", OnInitCheatsCompanions);
            Opt(s, "taom_cheat_panel_cheats_companions", "cm_heal", "{=!}Heal All Companions", Always, _ => DoHealCompanions(), false, 0, requiresConfirmation: true);
            Opt(s, "taom_cheat_panel_cheats_companions", "cm_maxskills", "{=!}Max All Companion Skills", Always, _ => DoMaxCompanionSkills(), false, 1, requiresConfirmation: true);
            Opt(s, "taom_cheat_panel_cheats_companions", "cm_level", "{=!}Level All Companions (+50k XP)", Always, _ => DoLevelAllCompanions(), false, 2, requiresConfirmation: true);
            Opt(s, "taom_cheat_panel_cheats_companions", "cm_attr", "{=!}Add 10 Attribute Pts to All", Always, _ => DoAddAttrToAllCompanions(), false, 3, requiresConfirmation: true);
            Opt(s, "taom_cheat_panel_cheats_companions", "cm_focus", "{=!}Add 10 Focus Pts to All", Always, _ => DoAddFocusToAllCompanions(), false, 4, requiresConfirmation: true);
            Opt(s, "taom_cheat_panel_cheats_companions", "cm_perks", "{=!}Unlock All Perks for All", Always, _ => DoUnlockPerksAllCompanions(), false, 5, requiresConfirmation: true);
            Opt(s, "taom_cheat_panel_cheats_companions", "cm_back", "{=!}Back", Always, _ => GameMenu.SwitchToMenu("taom_cheat_panel_cheats"), true, 6);

            // ══ Clan & Kingdom ════════════════════════════════════════════
            s.AddGameMenu("taom_cheat_panel_cheats_clan", "{TAOM_CHEAT_PANEL_CHEATS_CLAN_TEXT}", OnInitCheatsClan);
            Opt(s, "taom_cheat_panel_cheats_clan", "cc_renown_set", "{=!}Set Renown Amount", Always, _ => GameMenu.SwitchToMenu("taom_cheat_panel_cheats_renown"), false, 0);
            Opt(s, "taom_cheat_panel_cheats_clan", "cc_renown_add", "{=!}Add Renown", Always, _ => DoAddRenown(), false, 1, requiresConfirmation: true);
            Opt(s, "taom_cheat_panel_cheats_clan", "cc_inf_set", "{=!}Set Influence Amount", Always, _ => GameMenu.SwitchToMenu("taom_cheat_panel_cheats_influence"), false, 2);
            Opt(s, "taom_cheat_panel_cheats_clan", "cc_inf_add", "{=!}Add Influence", Always, _ => DoAddInfluence(), false, 3, requiresConfirmation: true);
            Opt(s, "taom_cheat_panel_cheats_clan", "cc_tier", "{=!}Boost Clan to Tier 6", Always, _ => DoBoostClanMaxTier(), false, 4, requiresConfirmation: true);
            Opt(s, "taom_cheat_panel_cheats_clan", "cc_rel_notables", "{=!}Boost Relations (notables)", Always, _ => GameMenu.SwitchToMenu("taom_cheat_panel_cheats_rel"), false, 5);
            Opt(s, "taom_cheat_panel_cheats_clan", "cc_rel_all", "{=!}Boost Relations ALL Lords +50", Always, _ => DoMaxRelationsAllLords(50), false, 6, requiresConfirmation: true);
            Opt(s, "taom_cheat_panel_cheats_clan", "cc_rel_max", "{=!}Max Relations ALL Lords (+100)", Always, _ => DoMaxRelationsAllLords(100), false, 7, requiresConfirmation: true);
            Opt(s, "taom_cheat_panel_cheats_clan", "cc_peace", "{=!}Make Peace (all wars)", Always, _ => DoMakePeaceAll(), false, 8, requiresConfirmation: true);
            Opt(s, "taom_cheat_panel_cheats_clan", "cc_allsett", "{=!}Max All Owned Settlements", Always, _ => DoMaxAllOwnedSettlements(), false, 9, requiresConfirmation: true);
            Opt(s, "taom_cheat_panel_cheats_clan", "cc_back", "{=!}Back", Always, _ => GameMenu.SwitchToMenu("taom_cheat_panel_cheats"), true, 10);

            BuildPresetMenu(s, "taom_cheat_panel_cheats_renown", "taom_cheat_panel_cheats_clan", RenownPresets, "renown", "N0", v => RenownAmount = v);
            BuildPresetMenu(s, "taom_cheat_panel_cheats_influence", "taom_cheat_panel_cheats_clan", InfluencePresets, "influence", "N0", v => InfluenceAmount = v);

            s.AddGameMenu("taom_cheat_panel_cheats_rel", "{=!}Select relation boost:", _ => { });
            foreach (var r in new[] { 10, 25, 50, 100 })
            { var cv = r; Opt(s, "taom_cheat_panel_cheats_rel", "crel_" + cv, "{=!}+" + cv + " with all notables", Always, _ => DoBoostRelations(cv), false, cv, requiresConfirmation: true); }
            Opt(s, "taom_cheat_panel_cheats_rel", "crel_back", "{=!}Back", Always, _ => GameMenu.SwitchToMenu("taom_cheat_panel_cheats_clan"), true, 999);

            // ══ Crafting & Materials ══════════════════════════════════════
            s.AddGameMenu("taom_cheat_panel_cheats_crafting", "{TAOM_CHEAT_PANEL_CHEATS_CRAFT_TEXT}", OnInitCheatsCrafting);
            Opt(s, "taom_cheat_panel_cheats_crafting", "cr_stamina", "{=!}Restore Smithing Stamina", Always, _ => DoRestoreSmithingStamina(), false, 0, requiresConfirmation: true);
            Opt(s, "taom_cheat_panel_cheats_crafting", "cr_iron", "{=!}Add Iron Ore x200", Always, _ => DoAddCraftMaterial("iron", 200), false, 1, requiresConfirmation: true);
            Opt(s, "taom_cheat_panel_cheats_crafting", "cr_crude", "{=!}Add Crude Iron x200", Always, _ => DoAddCraftMaterial("crude_iron", 200), false, 2, requiresConfirmation: true);
            Opt(s, "taom_cheat_panel_cheats_crafting", "cr_wrought", "{=!}Add Wrought Iron x200", Always, _ => DoAddCraftMaterial("wrought_iron", 200), false, 3, requiresConfirmation: true);
            Opt(s, "taom_cheat_panel_cheats_crafting", "cr_coal", "{=!}Add Coal x200", Always, _ => DoAddCraftMaterial("coal", 200), false, 4, requiresConfirmation: true);
            Opt(s, "taom_cheat_panel_cheats_crafting", "cr_wood", "{=!}Add Hardwood x200", Always, _ => DoAddCraftMaterial("hardwood", 200), false, 5, requiresConfirmation: true);
            Opt(s, "taom_cheat_panel_cheats_crafting", "cr_steel", "{=!}Add Steel x150", Always, _ => DoAddCraftMaterial("steel", 150), false, 6, requiresConfirmation: true);
            Opt(s, "taom_cheat_panel_cheats_crafting", "cr_finesteel", "{=!}Add Fine Steel x100", Always, _ => DoAddCraftMaterial("fine_steel", 100), false, 7, requiresConfirmation: true);
            Opt(s, "taom_cheat_panel_cheats_crafting", "cr_tham", "{=!}Add Thamaskene Steel x50", Always, _ => DoAddCraftMaterial("thamaskene_steel", 50), false, 8, requiresConfirmation: true);
            Opt(s, "taom_cheat_panel_cheats_crafting", "cr_silver", "{=!}Add Silver Ore x100", Always, _ => DoAddCraftMaterial("silver_ore", 100), false, 9, requiresConfirmation: true);
            Opt(s, "taom_cheat_panel_cheats_crafting", "cr_all", "{=!}Add ALL Materials (full stack)", Always, _ => DoAddAllCraftMaterials(), false, 10, requiresConfirmation: true);
            Opt(s, "taom_cheat_panel_cheats_crafting", "cr_back", "{=!}Back", Always, _ => GameMenu.SwitchToMenu("taom_cheat_panel_cheats"), true, 11);

            // ══ Gear & Equipment ══════════════════════════════════════════
            s.AddGameMenu("taom_cheat_panel_cheats_gear", "{TAOM_CHEAT_PANEL_CHEATS_GEAR_TEXT}", OnInitCheatsGear);
            Opt(s, "taom_cheat_panel_cheats_gear", "gr_sword1h", "{=!}Add Best One-Handed Sword", Always, _ => DoAddBestGearItem(ItemObject.ItemTypeEnum.OneHandedWeapon, "one-handed"), false, 0, requiresConfirmation: true);
            Opt(s, "taom_cheat_panel_cheats_gear", "gr_sword2h", "{=!}Add Best Two-Handed Sword", Always, _ => DoAddBestGearItem(ItemObject.ItemTypeEnum.TwoHandedWeapon, "two-handed"), false, 1, requiresConfirmation: true);
            Opt(s, "taom_cheat_panel_cheats_gear", "gr_pole", "{=!}Add Best Polearm", Always, _ => DoAddBestGearItem(ItemObject.ItemTypeEnum.Polearm, "polearm"), false, 2, requiresConfirmation: true);
            Opt(s, "taom_cheat_panel_cheats_gear", "gr_bow", "{=!}Add Best Bow", Always, _ => DoAddBestGearItem(ItemObject.ItemTypeEnum.Bow, "bow"), false, 3, requiresConfirmation: true);
            Opt(s, "taom_cheat_panel_cheats_gear", "gr_crossbow", "{=!}Add Best Crossbow", Always, _ => DoAddBestGearItem(ItemObject.ItemTypeEnum.Crossbow, "crossbow"), false, 4, requiresConfirmation: true);
            Opt(s, "taom_cheat_panel_cheats_gear", "gr_thrown", "{=!}Add Best Thrown Weapons", Always, _ => DoAddBestGearItem(ItemObject.ItemTypeEnum.Thrown, "thrown"), false, 5, requiresConfirmation: true);
            Opt(s, "taom_cheat_panel_cheats_gear", "gr_shield", "{=!}Add Best Shield", Always, _ => DoAddBestGearItem(ItemObject.ItemTypeEnum.Shield, "shield"), false, 6, requiresConfirmation: true);
            Opt(s, "taom_cheat_panel_cheats_gear", "gr_body", "{=!}Add Best Body Armor", Always, _ => DoAddBestGearItem(ItemObject.ItemTypeEnum.BodyArmor, "body armor"), false, 7, requiresConfirmation: true);
            Opt(s, "taom_cheat_panel_cheats_gear", "gr_head", "{=!}Add Best Helmet", Always, _ => DoAddBestGearItem(ItemObject.ItemTypeEnum.HeadArmor, "helmet"), false, 8, requiresConfirmation: true);
            Opt(s, "taom_cheat_panel_cheats_gear", "gr_gloves", "{=!}Add Best Gloves", Always, _ => DoAddBestGearItem(ItemObject.ItemTypeEnum.HandArmor, "gloves"), false, 9, requiresConfirmation: true);
            Opt(s, "taom_cheat_panel_cheats_gear", "gr_boots", "{=!}Add Best Boots", Always, _ => DoAddBestGearItem(ItemObject.ItemTypeEnum.LegArmor, "boots"), false, 10, requiresConfirmation: true);
            Opt(s, "taom_cheat_panel_cheats_gear", "gr_horse", "{=!}Add Best War Horse", Always, _ => DoAddBestHorse(), false, 11, requiresConfirmation: true);
            Opt(s, "taom_cheat_panel_cheats_gear", "gr_goods", "{=!}Add All Trade Goods x100", Always, _ => DoAddAllTradeGoods(), false, 12, requiresConfirmation: true);
            Opt(s, "taom_cheat_panel_cheats_gear", "gr_back", "{=!}Back", Always, _ => GameMenu.SwitchToMenu("taom_cheat_panel_cheats"), true, 13);

            // ══ World & Campaign ══════════════════════════════════════════
            s.AddGameMenu("taom_cheat_panel_cheats_world", "{TAOM_CHEAT_PANEL_CHEATS_WORLD_TEXT}", OnInitCheatsWorld);
            Opt(s, "taom_cheat_panel_cheats_world", "wd_healall", "{=!}Heal ALL Heroes Globally", Always, _ => DoHealAllHeroesGlobal(), false, 0, requiresConfirmation: true);
            Opt(s, "taom_cheat_panel_cheats_world", "wd_hideouts", "{=!}Discover All Hideouts", Always, _ => DoDiscoverAllHideouts(), false, 1, requiresConfirmation: true);
            Opt(s, "taom_cheat_panel_cheats_world", "wd_bandits", "{=!}Destroy Nearby Bandits (r50)", Always, _ => DoClearNearbyBandits(), false, 2, requiresConfirmation: true);
            Opt(s, "taom_cheat_panel_cheats_world", "wd_capture", "{=!}Capture Nearest Enemy Settlement", Always, _ => DoCaptureNearestEnemySettlement(), false, 3, requiresConfirmation: true);
            Opt(s, "taom_cheat_panel_cheats_world", "wd_allsett", "{=!}Max ALL Owned Settlements", Always, _ => DoMaxAllOwnedSettlements(), false, 4, requiresConfirmation: true);
            Opt(s, "taom_cheat_panel_cheats_world", "wd_allcomp", "{=!}Heal All Heroes in All Parties", Always, _ => DoHealAllHeroesGlobal(), false, 5, requiresConfirmation: true);
            Opt(s, "taom_cheat_panel_cheats_world", "wd_reveal_all", "{=!}Reveal All Settlements", Always, _ => DoRevealAllSettlements(), false, 6, requiresConfirmation: true);
            Opt(s, "taom_cheat_panel_cheats_world", "wd_clear_bandits_all", "{=!}Destroy ALL Bandit Parties", Always, _ => DoClearAllBandits(), false, 7, requiresConfirmation: true);
            Opt(s, "taom_cheat_panel_cheats_world", "wd_back", "{=!}Back", Always, _ => GameMenu.SwitchToMenu("taom_cheat_panel_cheats"), true, 8);
        }

        private static void RegisterAdjustmentsMenu(CampaignGameStarter s)
        {
            s.AddGameMenu("taom_cheat_panel_adjustments", "{TAOM_CHEAT_PANEL_ADJUSTMENTS_TEXT}", OnInitAdjustments);
            Opt(s, "taom_cheat_panel_adjustments", "adj_back", "{=!}Back", Always, _ => BackToRoot(), true, 0);
        }

        private static void RegisterTimeMenu(CampaignGameStarter s)
        {
            s.AddGameMenu("taom_cheat_panel_time", "{TAOM_CHEAT_PANEL_TIME_TEXT}", OnInitTime);
            Opt(s, "taom_cheat_panel_time", "t_stop", "{=!}Speed: Stop", Always, _ => { DoSetSpeed(CampaignTimeControlMode.Stop, "Stopped"); GameMenu.SwitchToMenu("taom_cheat_panel_time"); }, false, 0, requiresConfirmation: true);
            Opt(s, "taom_cheat_panel_time", "t_normal", "{=!}Speed: Normal", Always, _ => { DoSetSpeed(CampaignTimeControlMode.StoppablePlay, "Normal"); GameMenu.SwitchToMenu("taom_cheat_panel_time"); }, false, 1, requiresConfirmation: true);
            Opt(s, "taom_cheat_panel_time", "t_fast", "{=!}Speed: Fast", Always, _ => { DoSetSpeed(CampaignTimeControlMode.FastForwardStop, "Fast"); GameMenu.SwitchToMenu("taom_cheat_panel_time"); }, false, 2, requiresConfirmation: true);
            Opt(s, "taom_cheat_panel_time", "t_vfast", "{=!}Speed: Very Fast", Always, _ => { DoSetSpeed(CampaignTimeControlMode.UnstoppableFastForward, "Very Fast"); GameMenu.SwitchToMenu("taom_cheat_panel_time"); }, false, 3, requiresConfirmation: true);
            Opt(s, "taom_cheat_panel_time", "t_1d", "{=!}Advance 1 Day", Always, _ => { DoAdvanceDays(1); GameMenu.SwitchToMenu("taom_cheat_panel_time"); }, false, 4, requiresConfirmation: true);
            Opt(s, "taom_cheat_panel_time", "t_7d", "{=!}Advance 7 Days", Always, _ => { DoAdvanceDays(7); GameMenu.SwitchToMenu("taom_cheat_panel_time"); }, false, 5, requiresConfirmation: true);
            Opt(s, "taom_cheat_panel_time", "t_30d", "{=!}Advance 30 Days", Always, _ => { DoAdvanceDays(30); GameMenu.SwitchToMenu("taom_cheat_panel_time"); }, false, 6, requiresConfirmation: true);
            Opt(s, "taom_cheat_panel_time", "t_season", "{=!}Skip to Next Season", Always, _ => { DoSkipSeason(); GameMenu.SwitchToMenu("taom_cheat_panel_time"); }, false, 7, requiresConfirmation: true);
            Opt(s, "taom_cheat_panel_time", "t_back", "{=!}Back", Always, _ => BackToRoot(), true, 8);
        }

        private static void RegisterTeleportMenus(CampaignGameStarter s)
        {
            s.AddGameMenu("taom_cheat_panel_teleport", "{=!}Teleport — choose a type:", _ => { });
            Opt(s, "taom_cheat_panel_teleport", "tp_towns", "{=!}Towns", Always, _ => GameMenu.SwitchToMenu("taom_cheat_panel_tp_towns"), false, 0);
            Opt(s, "taom_cheat_panel_teleport", "tp_castles", "{=!}Castles", Always, _ => GameMenu.SwitchToMenu("taom_cheat_panel_tp_castles"), false, 1);
            Opt(s, "taom_cheat_panel_teleport", "tp_villages", "{=!}Villages", Always, _ => GameMenu.SwitchToMenu("taom_cheat_panel_tp_villages"), false, 2);
            Opt(s, "taom_cheat_panel_teleport", "tp_back", "{=!}Back", Always, _ => BackToRoot(), true, 3);

            RegisterTeleportList(s, "taom_cheat_panel_tp_towns", "taom_cheat_panel_teleport", sett => sett.IsTown);
            RegisterTeleportList(s, "taom_cheat_panel_tp_castles", "taom_cheat_panel_teleport", sett => sett.IsCastle);
            RegisterTeleportList(s, "taom_cheat_panel_tp_villages", "taom_cheat_panel_teleport", sett => sett.IsVillage);
        }

        private static void RegisterTeleportList(CampaignGameStarter s, string menuId, string backId, Func<Settlement, bool> filter)
        {
            s.AddGameMenu(menuId, "{=!}Select a settlement:", _ => { });
            try
            {
                var list = Settlement.All
                    .Where(x => x != null && filter(x))
                    .OrderBy(x => ((object)x.Name) != null ? x.Name.ToString() : "")
                    .ToList();
                for (int i = 0; i < list.Count; i++)
                {
                    var sett = list[i];
                    var sid = sett.StringId;
                    var sname = ((object)sett.Name) != null ? sett.Name.ToString() : sid;
                    var cult = sett.Culture != null && ((object)sett.Culture.Name) != null ? sett.Culture.Name.ToString() : "?";
                    Opt(s, menuId, "tp_" + sid, "{=!}" + sname + " (" + cult + ")", Always, _ => DoTeleport(sid, sname), false, i, requiresConfirmation: true);
                }
            }
            catch (Exception ex) { SupportRuntime.Log.ErrorOnce("hub.tp.list." + menuId, ex); }
            Opt(s, menuId, "tpback", "{=!}Back", Always, _ => GameMenu.SwitchToMenu(backId), true, 9999);
        }

        private static void RegisterSpawnMenus(CampaignGameStarter s)
        {
            s.AddGameMenu("taom_cheat_panel_spawn", "{TAOM_CHEAT_PANEL_SPAWN_TEXT}", OnInitSpawn);
            Opt(s, "taom_cheat_panel_spawn", "sp_culture", "{=!}Select Culture", Always, _ => GameMenu.SwitchToMenu("taom_cheat_panel_spawn_culture"), false, 0);
            Opt(s, "taom_cheat_panel_spawn", "sp_tier", "{=!}Set Troop Tier", Always, _ => GameMenu.SwitchToMenu("taom_cheat_panel_spawn_tier"), false, 1);
            Opt(s, "taom_cheat_panel_spawn", "sp_count", "{=!}Set Troop Count", Always, _ => GameMenu.SwitchToMenu("taom_cheat_panel_spawn_count"), false, 2);
            Opt(s, "taom_cheat_panel_spawn", "sp_add", "{=!}Add Troops to Party", Always, _ => DoSpawnToParty(), false, 3, requiresConfirmation: true);
            Opt(s, "taom_cheat_panel_spawn", "sp_hostile", "{=!}Spawn Hostile Party Nearby", Always, _ => DoSpawnHostile(), false, 4, requiresConfirmation: true);
            Opt(s, "taom_cheat_panel_spawn", "sp_elite", "{=!}Add Elite Troops (Tier 5)", Always, _ => DoAddEliteTroops(), false, 5, requiresConfirmation: true);
            Opt(s, "taom_cheat_panel_spawn", "sp_back", "{=!}Back", Always, _ => BackToRoot(), true, 6);

            // Culture list registered at session launch — same pattern as RegisterTeleportList.
            RegisterSpawnCultureList(s);

            s.AddGameMenu("taom_cheat_panel_spawn_tier", "{=!}Select tier (1=basic, 5=elite):", _ => { });
            for (int t = 1; t <= 5; t++)
            { var c = t; Opt(s, "taom_cheat_panel_spawn_tier", "tier_" + c, "{=!}Tier " + c, Always, _ => { SpawnTier = c; GameMenu.SwitchToMenu("taom_cheat_panel_spawn"); }, false, c - 1); }
            Opt(s, "taom_cheat_panel_spawn_tier", "tier_back", "{=!}Back", Always, _ => GameMenu.SwitchToMenu("taom_cheat_panel_spawn"), true, 5);

            BuildPresetMenu(s, "taom_cheat_panel_spawn_count", "taom_cheat_panel_spawn", TroopPresets, "troops", "", v => TroopCount = v);
        }

        private static void RegisterSpawnCultureList(CampaignGameStarter s)
        {
            s.AddGameMenu("taom_cheat_panel_spawn_culture", "{TAOM_CHEAT_PANEL_SPAWN_CULTURE_TEXT}", OnInitSpawnCulture);

            // "Any" is always option 0.
            Opt(s, "taom_cheat_panel_spawn_culture", "cult_any", "{=!}Any (no filter)",
                Always, _ => { SpawnCultureId = ""; GameMenu.SwitchToMenu("taom_cheat_panel_spawn"); }, false, 0);

            try
            {
                var cultures = CharacterObject.All
                    .Where(c => c != null && !c.IsHero && c.Culture != null && !string.IsNullOrEmpty(c.Culture.StringId))
                    .Select(c => c.Culture.StringId)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(id => GetCultureDisplayName(id))
                    .ToList();

                for (int i = 0; i < cultures.Count; i++)
                {
                    var id = cultures[i];
                    Opt(s, "taom_cheat_panel_spawn_culture", "cult_" + id.ToLowerInvariant(),
                        "{=!}" + GetCultureDisplayName(id),
                        Always, _ => { SpawnCultureId = id; GameMenu.SwitchToMenu("taom_cheat_panel_spawn"); },
                        false, i + 1);
                }

                SupportRuntime.Log.Info("[SpawnCulture] Registered " + cultures.Count + " culture options.");
            }
            catch (Exception ex) { SupportRuntime.Log.ErrorOnce("hub.spawn.culturelist", ex); }

            Opt(s, "taom_cheat_panel_spawn_culture", "cult_back", "{=!}Back",
                Always, _ => GameMenu.SwitchToMenu("taom_cheat_panel_spawn"), true, 9999);
        }

        private static void RegisterSettlementMenu(CampaignGameStarter s)
        {
            s.AddGameMenu("taom_cheat_panel_settlement", "{TAOM_CHEAT_PANEL_SETTLEMENT_TEXT}", OnInitSettlement);

            // Most tools only make sense inside a town or castle (Town object required).
            Opt(s, "taom_cheat_panel_settlement", "sett_prosperity", "{=!}Set Prosperity", CondTownOrCastle, _ => GameMenu.SwitchToMenu("taom_cheat_panel_sett_pros"), false, 0);
            Opt(s, "taom_cheat_panel_settlement", "sett_loyalty", "{=!}Set Loyalty", CondTownOrCastle, _ => GameMenu.SwitchToMenu("taom_cheat_panel_sett_loy"), false, 1);
            Opt(s, "taom_cheat_panel_settlement", "sett_security", "{=!}Set Security", CondTownOrCastle, _ => GameMenu.SwitchToMenu("taom_cheat_panel_sett_sec"), false, 2);
            Opt(s, "taom_cheat_panel_settlement", "sett_food", "{=!}Fill Food Stocks", CondTownOrCastle, _ => DoFillSettlementFood(), false, 3, requiresConfirmation: true);
            Opt(s, "taom_cheat_panel_settlement", "sett_garrison", "{=!}Reinforce Garrison", CondTownOrCastle, _ => DoReinforceGarrison(), false, 4, requiresConfirmation: true);
            Opt(s, "taom_cheat_panel_settlement", "sett_militia", "{=!}Boost Militia (+200)", Always, _ => DoBoostMilitia(), false, 5, requiresConfirmation: true);
            Opt(s, "taom_cheat_panel_settlement", "sett_give", "{=!}Give to My Clan", CondTownOrCastle, _ => DoGiveToClan(), false, 6, requiresConfirmation: true);
            Opt(s, "taom_cheat_panel_settlement", "sett_restore", "{=!}Fully Restore Current Settlement", CondTownOrCastle, _ => DoFullyRestoreCurrentSettlement(), false, 7, requiresConfirmation: true);
            Opt(s, "taom_cheat_panel_settlement", "sett_back", "{=!}Back", Always, _ => BackToRoot(), true, 9);

            // Direct-apply preset menus: pick a value → it is set immediately → return to settlement menu.
            BuildDirectPresetMenu(s, "taom_cheat_panel_sett_pros", "taom_cheat_panel_settlement", ProsperityPresets, "prosperity", "N0", DoSetProsperity);
            BuildDirectPresetMenu(s, "taom_cheat_panel_sett_loy", "taom_cheat_panel_settlement", LoyaltyPresets, "loyalty", "", DoSetLoyalty);
            BuildDirectPresetMenu(s, "taom_cheat_panel_sett_sec", "taom_cheat_panel_settlement", SecurityPresets, "security", "", DoSetSecurity);
        }

        private static void RegisterKingdomMenu(CampaignGameStarter s)
        {
            s.AddGameMenu("taom_cheat_panel_kingdom", "{TAOM_CHEAT_PANEL_KINGDOM_TEXT}", OnInitKingdom);
            Opt(s, "taom_cheat_panel_kingdom", "k_peace", "{=!}Make Peace (all wars)", Always, _ => DoMakePeaceAll(), false, 0, requiresConfirmation: true);
            Opt(s, "taom_cheat_panel_kingdom", "k_relation", "{=!}Max Relations (all notables)", Always, _ => GameMenu.SwitchToMenu("taom_cheat_panel_kingdom_rel"), false, 1);
            Opt(s, "taom_cheat_panel_kingdom", "k_prisoners", "{=!}Release All Prisoners", Always, _ => DoReleasePrisoners(), false, 2, requiresConfirmation: true);
            Opt(s, "taom_cheat_panel_kingdom", "k_wounded", "{=!}Recover All Wounded Troops", Always, _ => DoRecoverWounded(), false, 3, requiresConfirmation: true);
            Opt(s, "taom_cheat_panel_kingdom", "k_settle_food", "{=!}Fill Settlement Food Stocks", Always, _ => DoFillSettlementFood(), false, 4, requiresConfirmation: true);
            Opt(s, "taom_cheat_panel_kingdom", "k_settle_gar", "{=!}Reinforce Settlement Garrison", Always, _ => DoReinforceGarrison(), false, 5, requiresConfirmation: true);
            Opt(s, "taom_cheat_panel_kingdom", "k_back", "{=!}Back", Always, _ => BackToRoot(), true, 6);

            s.AddGameMenu("taom_cheat_panel_kingdom_rel", "{=!}Select relation boost:", _ => { });
            var relPresets = new[] { 10, 25, 50, 100 };
            for (int i = 0; i < relPresets.Length; i++)
            { var c = relPresets[i]; var idx = i; Opt(s, "taom_cheat_panel_kingdom_rel", "rel_" + c, "{=!}+" + c + " with all notables", Always, _ => DoBoostRelations(c), false, idx, requiresConfirmation: true); }
            Opt(s, "taom_cheat_panel_kingdom_rel", "rel_back", "{=!}Back", Always, _ => GameMenu.SwitchToMenu("taom_cheat_panel_kingdom"), true, relPresets.Length);
        }

        private static void RegisterHistoryMenu(CampaignGameStarter s)
        {
            s.AddGameMenu("taom_cheat_panel_history", "{TAOM_CHEAT_PANEL_HISTORY_TEXT}", OnInitHistory);
            Opt(s, "taom_cheat_panel_history", "hist_clear", "{=!}Clear History",
                args => { args.optionLeaveType = GameMenuOption.LeaveType.Default; return SupportRuntime.RecentActionsLog.GetSnapshot().Count > 0; },
                _ => { SupportRuntime.RecentActionsLog.Clear(); GameMenu.SwitchToMenu("taom_cheat_panel_history"); }, false, 0);
            Opt(s, "taom_cheat_panel_history", "hist_back", "{=!}Back", Always, _ => BackToRoot(), true, 1);
        }

        private static void OnInitRoot(MenuCallbackArgs _)
        {
            try
            {
                var h = Hero.MainHero;
                var sett = Settlement.CurrentSettlement;
                var settLine = sett != null ? "\nAt: " + sett.Name.ToString() + " (" + (sett.IsTown ? "Town" : sett.IsCastle ? "Castle" : "Village") + ")" : "";
                var txt = "TAOM Cheat Panel\n\n" +
                    "Hero: " + (h != null ? h.Name.ToString() : "?") +
                    "   Gold: " + (h != null ? h.Gold.ToString("N0") : "?") +
                    "   Renown: " + (h?.Clan != null ? ((int)h.Clan.Renown).ToString("N0") : "?") +
                    "\nParty: " + (MobileParty.MainParty?.MemberRoster?.TotalManCount.ToString() ?? "?") + " troops" +
                    "   Wounded: " + (MobileParty.MainParty?.MemberRoster?.TotalWoundedRegulars.ToString() ?? "?") +
                    settLine +
                    (LastStatus.Length > 0 ? "\n\n\u25ba " + LastStatus : "");
                MBTextManager.SetTextVariable("TAOM_CHEAT_PANEL_TITLE", txt);
            }
            catch { MBTextManager.SetTextVariable("TAOM_CHEAT_PANEL_TITLE", "TAOM Cheat Panel"); }
        }

        private static void OnInitCheats(MenuCallbackArgs _)
        {
            MBTextManager.SetTextVariable("TAOM_CHEAT_PANEL_CHEATS_TEXT",
                "Player Cheats  —  select a category:" +
                (LastStatus.Length > 0 ? "\n\n\u25ba " + LastStatus : ""));
        }

        private static void OnInitAdjustments(MenuCallbackArgs _)
        {
            MBTextManager.SetTextVariable("TAOM_CHEAT_PANEL_ADJUSTMENTS_TEXT", BuildAdjustmentText());
        }

        private static void OnInitCheatsPlayer(MenuCallbackArgs _)
        {
            var h = Hero.MainHero;
            var dev = h?.HeroDeveloper;
            MBTextManager.SetTextVariable("TAOM_CHEAT_PANEL_CHEATS_PLAYER_TEXT",
                "Player Stats\n\n" +
                "Hero: " + (h != null ? h.Name.ToString() : "?") +
                "   Level: " + (h != null ? h.Level.ToString() : "?") +
                "   Age: " + (h != null ? ((int)h.Age).ToString() : "?") + "\n" +
                "Gold: " + (h != null ? h.Gold.ToString("N0") : "?") + "\n" +
                "Attr pts: " + (dev != null ? dev.UnspentAttributePoints.ToString() : "?") +
                "   Focus pts: " + (dev != null ? dev.UnspentFocusPoints.ToString() : "?") + "\n" +
                "Gold to add: " + GoldAmount.ToString("N0") +
                "   XP to add: " + XpAmount.ToString("N0") +
                (LastStatus.Length > 0 ? "\n\n\u25ba " + LastStatus : ""));
        }

        private static void OnInitCheatsParty(MenuCallbackArgs _)
        {
            var p = MobileParty.MainParty;
            MBTextManager.SetTextVariable("TAOM_CHEAT_PANEL_CHEATS_PARTY_TEXT",
                "Party & Troops\n\n" +
                "Party size: " + (p?.MemberRoster?.TotalManCount.ToString() ?? "?") + "\n" +
                "Wounded: " + (p?.MemberRoster?.TotalWoundedRegulars.ToString() ?? "?") + "\n" +
                "Morale: " + (p != null ? ((int)p.Morale).ToString() : "?") + "\n" +
                "Troop count: " + TroopCount + "   Tier: " + SpawnTier +
                (LastStatus.Length > 0 ? "\n\n\u25ba " + LastStatus : ""));
        }

        private static void OnInitCheatsCompanions(MenuCallbackArgs _)
        {
            var companions = Hero.MainHero?.CompanionsInParty?.ToList();
            int count = companions?.Count ?? 0;
            MBTextManager.SetTextVariable("TAOM_CHEAT_PANEL_CHEATS_COMP_TEXT",
                "Companions\n\nCompanions in party: " + count + "\n" +
                (companions != null && count > 0
                    ? string.Join("\n", companions.Take(5).Select(c => "  " + c.Name.ToString() + " (Lv " + c.Level + ")"))
                    : "  (none in party)") +
                (LastStatus.Length > 0 ? "\n\n\u25ba " + LastStatus : ""));
        }

        private static void OnInitCheatsClan(MenuCallbackArgs _)
        {
            var h = Hero.MainHero;
            var clan = h?.Clan;
            var kingdom = clan?.Kingdom;
            MBTextManager.SetTextVariable("TAOM_CHEAT_PANEL_CHEATS_CLAN_TEXT",
                "Clan & Kingdom\n\n" +
                "Clan: " + (clan != null ? clan.Name.ToString() : "None") + "\n" +
                "Tier: " + (clan != null ? clan.Tier.ToString() : "?") + "\n" +
                "Kingdom: " + (kingdom != null ? kingdom.Name.ToString() : "None") + "\n" +
                "Renown: " + (clan != null ? ((int)clan.Renown).ToString("N0") : "?") + "\n" +
                "Influence: " + (clan != null ? ((int)clan.Influence).ToString("N0") : "?") + "\n" +
                "Fiefs: " + (clan != null ? clan.Fiefs.Count.ToString() : "?") + "\n" +
                "Renown to add: " + RenownAmount.ToString("N0") +
                "   Influence to add: " + InfluenceAmount.ToString("N0") +
                (LastStatus.Length > 0 ? "\n\n\u25ba " + LastStatus : ""));
        }

        private static void OnInitCheatsCrafting(MenuCallbackArgs _)
        {
            MBTextManager.SetTextVariable("TAOM_CHEAT_PANEL_CHEATS_CRAFT_TEXT",
                "Crafting & Materials\n\nAdd smithing materials to party inventory." +
                (LastStatus.Length > 0 ? "\n\n\u25ba " + LastStatus : ""));
        }

        private static void OnInitCheatsGear(MenuCallbackArgs _)
        {
            MBTextManager.SetTextVariable("TAOM_CHEAT_PANEL_CHEATS_GEAR_TEXT",
                "Gear & Equipment\n\nAdds the highest-value item of each type to party inventory." +
                (LastStatus.Length > 0 ? "\n\n\u25ba " + LastStatus : ""));
        }

        private static void OnInitCheatsWorld(MenuCallbackArgs _)
        {
            int aliveHeroes = 0;
            try { aliveHeroes = Campaign.Current.AliveHeroes.Count(h => h != null); } catch { }
            MBTextManager.SetTextVariable("TAOM_CHEAT_PANEL_CHEATS_WORLD_TEXT",
                "World & Campaign\n\nLiving heroes: " + aliveHeroes +
                (LastStatus.Length > 0 ? "\n\n\u25ba " + LastStatus : ""));
        }

        private static void OnInitTime(MenuCallbackArgs _)
        {
            MBTextManager.SetTextVariable("TAOM_CHEAT_PANEL_TIME_TEXT", BuildTimeText());
        }

        private static void OnInitSpawn(MenuCallbackArgs _)
        {
            var troop = ResolveTroop(SpawnTier, SpawnCultureId);
            var cultureLine = string.IsNullOrEmpty(SpawnCultureId)
                ? "Culture: Any"
                : "Culture: " + GetCultureDisplayName(SpawnCultureId);
            MBTextManager.SetTextVariable("TAOM_CHEAT_PANEL_SPAWN_TEXT",
                "Spawn Troops\n\n" +
                "Tier: " + SpawnTier + "   Count: " + TroopCount + "\n" +
                cultureLine + "\n" +
                "Troop: " + (troop != null ? troop.Name.ToString() : "none found for tier " + SpawnTier + " / " + (string.IsNullOrEmpty(SpawnCultureId) ? "any culture" : SpawnCultureId)) +
                (LastStatus.Length > 0 ? "\n\n\u25ba " + LastStatus : ""));
        }

        private static void OnInitSpawnCulture(MenuCallbackArgs _)
        {
            MBTextManager.SetTextVariable("TAOM_CHEAT_PANEL_SPAWN_CULTURE_TEXT",
                "Select Culture\n\n" +
                "Current: " + (string.IsNullOrEmpty(SpawnCultureId) ? "Any" : GetCultureDisplayName(SpawnCultureId)) +
                (LastStatus.Length > 0 ? "\n\n\u25ba " + LastStatus : ""));
        }

        private static void OnInitSettlement(MenuCallbackArgs _)
        {
            MBTextManager.SetTextVariable("TAOM_CHEAT_PANEL_SETTLEMENT_TEXT", BuildSettlementText());
        }

        private static void OnInitKingdom(MenuCallbackArgs _)
        {
            try
            {
                var h = Hero.MainHero;
                var clan = h?.Clan;
                var kingdom = clan?.Kingdom;
                MBTextManager.SetTextVariable("TAOM_CHEAT_PANEL_KINGDOM_TEXT",
                    "Kingdom Tools\n\n" +
                    "Clan: " + (clan != null ? clan.Name.ToString() : "None") + "\n" +
                    "Kingdom: " + (kingdom != null ? kingdom.Name.ToString() : "None") + "\n" +
                    "Influence: " + (clan != null ? ((int)clan.Influence).ToString("N0") : "?") +
                    (LastStatus.Length > 0 ? "\n\n\u25ba " + LastStatus : ""));
            }
            catch { MBTextManager.SetTextVariable("TAOM_CHEAT_PANEL_KINGDOM_TEXT", "Kingdom Tools"); }
        }

        private static void OnInitHistory(MenuCallbackArgs _)
        {
            MBTextManager.SetTextVariable("TAOM_CHEAT_PANEL_HISTORY_TEXT", BuildHistoryText());
        }

        private static void DoAddGold()
        {
            try { Hero.MainHero.ChangeHeroGold(GoldAmount); SetStatus("Added " + GoldAmount.ToString("N0") + " gold. Total: " + Hero.MainHero.Gold.ToString("N0")); }
            catch (Exception ex) { HandleError("cheats.gold", ex); }
            GameMenu.SwitchToMenu("taom_cheat_panel_cheats_player");
        }

        private static void DoAddXp()
        {
            try
            {
                var dev = Hero.MainHero.HeroDeveloper;
                var skills = new[] {
                    DefaultSkills.OneHanded, DefaultSkills.TwoHanded, DefaultSkills.Polearm,
                    DefaultSkills.Bow, DefaultSkills.Crossbow, DefaultSkills.Throwing,
                    DefaultSkills.Riding, DefaultSkills.Athletics, DefaultSkills.Crafting,
                    DefaultSkills.Tactics, DefaultSkills.Scouting, DefaultSkills.Roguery,
                    DefaultSkills.Charm, DefaultSkills.Leadership, DefaultSkills.Trade,
                    DefaultSkills.Steward, DefaultSkills.Medicine, DefaultSkills.Engineering
                };
                foreach (var sk in skills)
                    dev.AddSkillXp(sk, XpAmount, shouldNotify: false, isAffectedByFocusFactor: false);
                SetStatus("Added " + XpAmount.ToString("N0") + " XP to " + skills.Length + " skills.");
            }
            catch (Exception ex) { HandleError("cheats.xp", ex); }
            GameMenu.SwitchToMenu("taom_cheat_panel_cheats_player");
        }

        private static void DoHeal()
        {
            try { var h = Hero.MainHero; h.HitPoints = h.MaxHitPoints; SetStatus("Healed " + h.Name.ToString() + " to full HP."); }
            catch (Exception ex) { HandleError("cheats.heal", ex); }
            GameMenu.SwitchToMenu("taom_cheat_panel_cheats_player");
        }

        private static void DoHealParty()
        {
            try
            {
                int count = 0;
                var roster = MobileParty.MainParty.MemberRoster;
                for (int i = 0; i < roster.Count; i++)
                {
                    var element = roster.GetElementCopyAtIndex(i);
                    if (element.WoundedNumber > 0)
                    {
                        roster.AddToCounts(element.Character, 0, woundedCount: -element.WoundedNumber);
                        count += element.WoundedNumber;
                    }
                }
                Hero.MainHero.HitPoints = Hero.MainHero.MaxHitPoints;
                SetStatus("Healed party. " + count + " wounded troops recovered.");
            }
            catch (Exception ex) { HandleError("cheats.healparty", ex); }
            GameMenu.SwitchToMenu("taom_cheat_panel_cheats_party");
        }

        private static void DoAddTroops()
        {
            try
            {
                var culture = Hero.MainHero?.Culture;
                var basic = culture?.BasicTroop;
                var troop = TroopEquipmentSafety.IsSpawnSafe(basic)
                    ? basic
                    : ResolveTroop(1, culture != null ? culture.StringId : "", TroopRolePreference.Any)
                        ?? ResolveTroop(1, "", TroopRolePreference.Any);
                if (troop == null) { SetStatus("No basic troop found."); GameMenu.SwitchToMenu("taom_cheat_panel_cheats_party"); return; }
                MobileParty.MainParty.AddElementToMemberRoster(troop, TroopCount);
                SetStatus("Added " + TroopCount + "x " + troop.Name.ToString() + ".");
            }
            catch (Exception ex) { HandleError("cheats.troops", ex); }
            GameMenu.SwitchToMenu("taom_cheat_panel_cheats_party");
        }

        private static void DoAddRenown()
        {
            try { Hero.MainHero.Clan.AddRenown(RenownAmount, shouldNotify: true); SetStatus("Added " + RenownAmount.ToString("N0") + " renown. Total: " + ((int)Hero.MainHero.Clan.Renown).ToString("N0")); }
            catch (Exception ex) { HandleError("cheats.renown", ex); }
            GameMenu.SwitchToMenu("taom_cheat_panel_cheats_clan");
        }

        private static void DoAddInfluence()
        {
            try { Hero.MainHero.Clan.Influence += InfluenceAmount; SetStatus("Added " + InfluenceAmount.ToString("N0") + " influence."); }
            catch (Exception ex) { HandleError("cheats.influence", ex); }
            GameMenu.SwitchToMenu("taom_cheat_panel_cheats_clan");
        }

        private static void DoBoostMorale()
        {
            try { MobileParty.MainParty.RecentEventsMorale = 100f; SetStatus("Party morale boosted to 100."); }
            catch (Exception ex) { HandleError("cheats.morale", ex); }
            GameMenu.SwitchToMenu("taom_cheat_panel_cheats_party");
        }

        private static void DoRestockFood()
        {
            try
            {
                var roster = MobileParty.MainParty.ItemRoster;
                var grain = Game.Current.ObjectManager.GetObject<ItemObject>("grain");
                if (grain == null) { SetStatus("Could not find grain item."); GameMenu.SwitchToMenu("taom_cheat_panel_cheats_party"); return; }
                roster.AddToCounts(grain, 200);
                SetStatus("Added 200 grain to party inventory.");
            }
            catch (Exception ex) { HandleError("cheats.food", ex); }
            GameMenu.SwitchToMenu("taom_cheat_panel_cheats_party");
        }

        private static void DoHealCompanions()
        {
            try
            {
                int count = 0;
                foreach (var h in Hero.MainHero.CompanionsInParty)
                { if (h.HitPoints < h.MaxHitPoints) { h.HitPoints = h.MaxHitPoints; count++; } }
                SetStatus("Healed " + count + " companion(s).");
            }
            catch (Exception ex) { HandleError("cheats.companions", ex); }
            GameMenu.SwitchToMenu("taom_cheat_panel_cheats_party");
        }

        private static void DoMaxAllSkills()
        {
            try
            {
                var dev = Hero.MainHero.HeroDeveloper;
                var skills = new[]
                {
                    DefaultSkills.OneHanded, DefaultSkills.TwoHanded, DefaultSkills.Polearm,
                    DefaultSkills.Bow,       DefaultSkills.Crossbow,  DefaultSkills.Throwing,
                    DefaultSkills.Riding,    DefaultSkills.Athletics, DefaultSkills.Crafting,
                    DefaultSkills.Tactics,   DefaultSkills.Scouting,  DefaultSkills.Roguery,
                    DefaultSkills.Charm,     DefaultSkills.Leadership,DefaultSkills.Trade,
                    DefaultSkills.Steward,   DefaultSkills.Medicine,  DefaultSkills.Engineering
                };
                foreach (var sk in skills)
                    dev.AddSkillXp(sk, 999999f, shouldNotify: false, isAffectedByFocusFactor: false);
                SetStatus("Maximum XP added to all " + skills.Length + " skills.");
            }
            catch (Exception ex) { HandleError("cheats.maxskills", ex); }
            GameMenu.SwitchToMenu("taom_cheat_panel_cheats_player");
        }

        private static void DoAddAttributePoints(int count)
        {
            try
            {
                var dev = Hero.MainHero.HeroDeveloper;
                dev.UnspentAttributePoints += count;
                SetStatus("Added " + count + " attribute point(s). Unspent: " + dev.UnspentAttributePoints + ".");
            }
            catch (Exception ex) { HandleError("cheats.attr", ex); }
            GameMenu.SwitchToMenu("taom_cheat_panel_cheats_player");
        }

        private static void DoAddFocusPoints(int count)
        {
            try
            {
                var dev = Hero.MainHero.HeroDeveloper;
                dev.UnspentFocusPoints += count;
                SetStatus("Added " + count + " focus point(s). Unspent: " + dev.UnspentFocusPoints + ".");
            }
            catch (Exception ex) { HandleError("cheats.focus", ex); }
            GameMenu.SwitchToMenu("taom_cheat_panel_cheats_player");
        }

        private static void DoSetHeroAge(int age)
        {
            try
            {
                var h = Hero.MainHero;
                // Age is driven by BirthDay; shift it to produce the target age
                h.SetBirthDay(CampaignTime.Now - CampaignTime.Years(age));
                SetStatus("Hero age set to " + age + ".");
            }
            catch (Exception ex) { HandleError("cheats.age", ex); }
            GameMenu.SwitchToMenu("taom_cheat_panel_cheats_player");
        }

        private static void DoAddTroopsByTier()
        {
            try
            {
                var troop = ResolveTroop(SpawnTier, SpawnCultureId);
                if (troop == null) { SetStatus("No T" + SpawnTier + " troop found."); GameMenu.SwitchToMenu("taom_cheat_panel_cheats_party"); return; }
                MobileParty.MainParty.AddElementToMemberRoster(troop, TroopCount);
                SetStatus("Added " + TroopCount + "x " + troop.Name.ToString() + " (T" + troop.Tier + ").");
            }
            catch (Exception ex) { HandleError("cheats.tierspawn", ex); }
            GameMenu.SwitchToMenu("taom_cheat_panel_cheats_party");
        }

        private static void DoAddCavalry()
        {
            try
            {
                var troop = ResolveTroop(4, SpawnCultureId, TroopRolePreference.Mounted);
                if (troop == null) { SetStatus("No T4 cavalry found."); GameMenu.SwitchToMenu("taom_cheat_panel_cheats_party"); return; }
                MobileParty.MainParty.AddElementToMemberRoster(troop, TroopCount);
                SetStatus("Added " + TroopCount + "x " + troop.Name.ToString() + " (T4 cavalry).");
            }
            catch (Exception ex) { HandleError("cheats.cavalry", ex); }
            GameMenu.SwitchToMenu("taom_cheat_panel_cheats_party");
        }

        private static void DoRecoverWoundedParty()
        {
            try
            {
                int count = 0;
                var roster = MobileParty.MainParty.MemberRoster;
                for (int i = 0; i < roster.Count; i++)
                {
                    var el = roster.GetElementCopyAtIndex(i);
                    if (el.WoundedNumber > 0) { roster.AddToCounts(el.Character, 0, woundedCount: -el.WoundedNumber); count += el.WoundedNumber; }
                }
                SetStatus("Recovered " + count + " wounded troop(s).");
            }
            catch (Exception ex) { HandleError("cheats.recover", ex); }
            GameMenu.SwitchToMenu("taom_cheat_panel_cheats_party");
        }

        private static void DoBoostPartySpeed()
        {
            try
            {
                // Add a large food supply which also lightens morale/supply drain,
                // and add horses to the item roster to push speed calculation up.
                var roster = MobileParty.MainParty.ItemRoster;
                var horse = Game.Current.ObjectManager.GetObject<ItemObject>("sumpter_horse");
                if (horse != null) roster.AddToCounts(horse, 20);
                var grain = Game.Current.ObjectManager.GetObject<ItemObject>("grain");
                if (grain != null) roster.AddToCounts(grain, 100);
                SetStatus("Added pack horses and food to improve party speed.");
            }
            catch (Exception ex) { HandleError("cheats.speed", ex); }
            GameMenu.SwitchToMenu("taom_cheat_panel_cheats_party");
        }

        private static void DoBoostClanMaxTier()
        {
            try
            {
                var clan = Hero.MainHero?.Clan;
                if (clan == null) { SetStatus("No clan found."); GameMenu.SwitchToMenu("taom_cheat_panel_cheats_clan"); return; }
                // Tier 6 requires ~6100 renown; add the deficit plus a buffer
                float tier6Target = 6200f;
                if (clan.Renown < tier6Target)
                    clan.AddRenown(tier6Target - clan.Renown, shouldNotify: true);
                SetStatus("Renown set to " + ((int)clan.Renown).ToString("N0") + " — clan should reach Tier 6.");
            }
            catch (Exception ex) { HandleError("cheats.clantier", ex); }
            GameMenu.SwitchToMenu("taom_cheat_panel_cheats_clan");
        }

        private static void DoRestoreSmithingStamina()
        {
            try
            {
                // Access CraftingCampaignBehavior stamina dictionary via reflection
                var craftType = ResolveLoadedType(
                    "TaleWorlds.CampaignSystem.CampaignBehaviors.CraftingCampaignBehavior",
                    "TaleWorlds.CampaignSystem");
                if (craftType != null)
                {
                    var getCB = typeof(Campaign).GetMethod("GetCampaignBehavior")
                        ?.MakeGenericMethod(craftType);
                    var behavior = getCB?.Invoke(Campaign.Current, null);
                    if (behavior != null)
                    {
                        var field = craftType.GetField("_heroCraftingStamina",
                            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        if (field != null)
                        {
                            var dict = field.GetValue(behavior) as System.Collections.Generic.Dictionary<Hero, int>;
                            if (dict != null)
                            {
                                var heroes = MobileParty.MainParty.MemberRoster.GetTroopRoster()
                                    .Where(e => e.Character != null && e.Character.IsHero && e.Character.HeroObject != null)
                                    .Select(e => e.Character.HeroObject).ToList();
                                foreach (var h in heroes)
                                    dict[h] = 100;
                                SetStatus("Smithing stamina restored for " + heroes.Count + " hero(es).");
                                GameMenu.SwitchToMenu("taom_cheat_panel_cheats_crafting");
                                return;
                            }
                        }
                    }
                }
                SetStatus("Smithing stamina: CraftingCampaignBehavior not found — not restored.");
            }
            catch (Exception ex) { HandleError("cheats.smithstamina", ex); }
            GameMenu.SwitchToMenu("taom_cheat_panel_cheats_crafting");
        }

        private static void DoAddCraftMaterial(string itemId, int count)
        {
            try
            {
                var item = Game.Current.ObjectManager.GetObject<ItemObject>(itemId);
                if (item == null) { SetStatus("Item '" + itemId + "' not found."); GameMenu.SwitchToMenu("taom_cheat_panel_cheats_crafting"); return; }
                MobileParty.MainParty.ItemRoster.AddToCounts(item, count);
                SetStatus("Added " + count + "x " + item.Name.ToString() + ".");
            }
            catch (Exception ex) { HandleError("cheats.mat." + itemId, ex); }
            GameMenu.SwitchToMenu("taom_cheat_panel_cheats_crafting");
        }

        private static void DoAddAllCraftMaterials()
        {
            try
            {
                var mats = new (string id, int qty)[]
                {
                    ("iron",             200),
                    ("coal",             200),
                    ("hardwood",         200),
                    ("fine_steel",       100),
                    ("thamaskene_steel",  50),
                    ("silver_ore",       100),
                    ("wrought_iron",     200),
                    ("crude_iron",       200),
                    ("steel",            150),
                };
                int added = 0;
                var roster = MobileParty.MainParty.ItemRoster;
                foreach (var (id, qty) in mats)
                {
                    var item = Game.Current.ObjectManager.GetObject<ItemObject>(id);
                    if (item != null) { roster.AddToCounts(item, qty); added++; }
                }
                SetStatus("Added all smithing materials (" + added + " types).");
            }
            catch (Exception ex) { HandleError("cheats.allmat", ex); }
            GameMenu.SwitchToMenu("taom_cheat_panel_cheats_crafting");
        }

        private static void DoAddExactGold(int amount)
        {
            try { Hero.MainHero.ChangeHeroGold(amount); SetStatus("Added " + amount.ToString("N0") + " gold. Total: " + Hero.MainHero.Gold.ToString("N0")); }
            catch (Exception ex) { HandleError("cheats.exactgold", ex); }
            GameMenu.SwitchToMenu("taom_cheat_panel_cheats_player");
        }

        private static void DoAddAllPerks()
        {
            try
            {
                var dev = Hero.MainHero.HeroDeveloper;
                int count = 0;
                foreach (var perk in PerkObject.All.Where(p => p != null))
                    if (!dev.GetPerkValue(perk)) { dev.AddPerk(perk); count++; }
                SetStatus("Unlocked " + count + " perk(s) for " + Hero.MainHero.Name + ".");
            }
            catch (Exception ex) { HandleError("cheats.allperks", ex); }
            GameMenu.SwitchToMenu("taom_cheat_panel_cheats_player");
        }

        private static void DoMaxAllAttributes()
        {
            try
            {
                var dev = Hero.MainHero.HeroDeveloper;
                // Add 30 attribute points (max all 6 attributes from 0 costs 4+3+3+3+3+3 = up to 19; buffer covers even low starts)
                dev.UnspentAttributePoints += 30;
                // Attempt direct reflection set on the backing dict
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
                            var setter = dictType.GetMethod("set_Item") ?? dictType.GetMethod("Add");
                            if (keys != null && setter != null)
                            {
                                foreach (var key in keys)
                                    setter.Invoke(dict, new object[] { key, 10 });
                                dev.UnspentAttributePoints -= 30; // cancel the addition since we set directly
                                SetStatus("All attributes set to 10.");
                                GameMenu.SwitchToMenu("taom_cheat_panel_cheats_player");
                                return;
                            }
                        }
                    }
                }
                catch { /* reflection failed, fall through to point-based fallback */ }
                SetStatus("Added 30 attribute points to spend. (Open the Attributes screen to distribute.)");
            }
            catch (Exception ex) { HandleError("cheats.maxattr", ex); }
            GameMenu.SwitchToMenu("taom_cheat_panel_cheats_player");
        }

        private static void DoAddRangedTroops()
        {
            try
            {
                var troop = ResolveTroop(4, SpawnCultureId, TroopRolePreference.Ranged);
                if (troop == null) { SetStatus("No T4 ranged troop found."); GameMenu.SwitchToMenu("taom_cheat_panel_cheats_party"); return; }
                MobileParty.MainParty.AddElementToMemberRoster(troop, TroopCount);
                SetStatus("Added " + TroopCount + "x " + troop.Name + " (T4 ranged).");
            }
            catch (Exception ex) { HandleError("cheats.ranged", ex); }
            GameMenu.SwitchToMenu("taom_cheat_panel_cheats_party");
        }

        private static void DoSpawnOptimalArmy()
        {
            try
            {
                var inf = ResolveTroop(5, SpawnCultureId, TroopRolePreference.Infantry);
                var arc = ResolveTroop(4, SpawnCultureId, TroopRolePreference.Ranged);
                var cav = ResolveTroop(4, SpawnCultureId, TroopRolePreference.Mounted);
                int added = 0;
                var p = MobileParty.MainParty;
                if (inf != null) { p.AddElementToMemberRoster(inf, 60); added += 60; }
                if (arc != null) { p.AddElementToMemberRoster(arc, 30); added += 30; }
                if (cav != null) { p.AddElementToMemberRoster(cav, 20); added += 20; }
                SetStatus("Spawned optimal army: " + added + " troops (60 T5 inf / 30 T4 arc / 20 T4 cav). Party: " + p.MemberRoster.TotalManCount + " total.");
            }
            catch (Exception ex) { HandleError("cheats.optarmy", ex); }
            GameMenu.SwitchToMenu("taom_cheat_panel_cheats_party");
        }

        private static void DoUpgradeAllTroops()
        {
            try
            {
                var roster = MobileParty.MainParty.MemberRoster;
                var snapshot = roster.GetTroopRoster().Where(e => !e.Character.IsHero).ToList();
                int upgraded = 0;
                foreach (var entry in snapshot)
                {
                    var top = GetTopTierCharacter(entry.Character);
                    if (top != null && top != entry.Character)
                    {
                        roster.AddToCounts(entry.Character, -entry.Number);
                        roster.AddToCounts(top, entry.Number);
                        upgraded += entry.Number;
                    }
                }
                SetStatus("Upgraded " + upgraded + " troop(s) to top of their upgrade tree.");
            }
            catch (Exception ex) { HandleError("cheats.upgradeall", ex); }
            GameMenu.SwitchToMenu("taom_cheat_panel_cheats_party");
        }

        private static CharacterObject GetTopTierCharacter(CharacterObject troop)
        {
            return TroopEquipmentSafety.ResolveSafeUpgradeTarget(troop);
        }

        private static void DoAddAllFoodTypes()
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
                SetStatus("Added 100 of each food type (" + added + " types found).");
            }
            catch (Exception ex) { HandleError("cheats.allfood", ex); }
            GameMenu.SwitchToMenu("taom_cheat_panel_cheats_party");
        }

        private static void DoAddPackAnimals()
        {
            try
            {
                int added = 0;
                added += AddItemById("sumpter_horse", 50);
                added += AddItemById("mule", 50);
                added += AddItemById("camel", 25);
                if (added == 0)
                {
                    var animal = Game.Current.ObjectManager.GetObjectTypeList<ItemObject>()
                        .Where(i => i != null && i.HasHorseComponent && i.IsAnimal)
                        .OrderByDescending(i => i.Value)
                        .FirstOrDefault();
                    if (animal != null)
                    {
                        MobileParty.MainParty.ItemRoster.AddToCounts(animal, 50);
                        added += 50;
                    }
                }

                SetStatus(added > 0 ? "Added " + added + " pack/carry animal(s)." : "No pack animal items found.");
            }
            catch (Exception ex) { HandleError("cheats.packanimals", ex); }
            GameMenu.SwitchToMenu("taom_cheat_panel_cheats_party");
        }

        private static void DoAddWarHorses()
        {
            try
            {
                var horse = Game.Current.ObjectManager.GetObjectTypeList<ItemObject>()
                    .Where(i => i != null && i.HasHorseComponent && i.HorseComponent != null && !i.IsAnimal)
                    .OrderByDescending(i => i.Value)
                    .FirstOrDefault();
                if (horse == null) { SetStatus("No war horse item found."); GameMenu.SwitchToMenu("taom_cheat_panel_cheats_party"); return; }
                MobileParty.MainParty.ItemRoster.AddToCounts(horse, 25);
                SetStatus("Added 25x " + horse.Name.ToString() + ".");
            }
            catch (Exception ex) { HandleError("cheats.warhorses", ex); }
            GameMenu.SwitchToMenu("taom_cheat_panel_cheats_party");
        }

        private static void DoMaxCompanionSkills()
        {
            try
            {
                int count = 0;
                foreach (var c in Hero.MainHero.CompanionsInParty.ToList())
                {
                    foreach (var sk in AllSkillObjects())
                        c.HeroDeveloper.AddSkillXp(sk, 999999f, shouldNotify: false, isAffectedByFocusFactor: false);
                    count++;
                }
                SetStatus("Maxed all skills for " + count + " companion(s).");
            }
            catch (Exception ex) { HandleError("cheats.compskills", ex); }
            GameMenu.SwitchToMenu("taom_cheat_panel_cheats_companions");
        }

        private static void DoLevelAllCompanions()
        {
            try
            {
                int count = 0;
                foreach (var c in Hero.MainHero.CompanionsInParty.ToList())
                {
                    foreach (var sk in AllSkillObjects())
                        c.HeroDeveloper.AddSkillXp(sk, 50000f, shouldNotify: false, isAffectedByFocusFactor: false);
                    count++;
                }
                SetStatus("Added 50k XP to all skills for " + count + " companion(s).");
            }
            catch (Exception ex) { HandleError("cheats.complevel", ex); }
            GameMenu.SwitchToMenu("taom_cheat_panel_cheats_companions");
        }

        private static void DoAddAttrToAllCompanions()
        {
            try
            {
                int count = 0;
                foreach (var c in Hero.MainHero.CompanionsInParty.ToList())
                { c.HeroDeveloper.UnspentAttributePoints += 10; count++; }
                SetStatus("Added 10 attribute points to " + count + " companion(s).");
            }
            catch (Exception ex) { HandleError("cheats.compattr", ex); }
            GameMenu.SwitchToMenu("taom_cheat_panel_cheats_companions");
        }

        private static void DoAddFocusToAllCompanions()
        {
            try
            {
                int count = 0;
                foreach (var c in Hero.MainHero.CompanionsInParty.ToList())
                { c.HeroDeveloper.UnspentFocusPoints += 10; count++; }
                SetStatus("Added 10 focus points to " + count + " companion(s).");
            }
            catch (Exception ex) { HandleError("cheats.compfocus", ex); }
            GameMenu.SwitchToMenu("taom_cheat_panel_cheats_companions");
        }

        private static void DoUnlockPerksAllCompanions()
        {
            try
            {
                int heroCount = 0; int perkTotal = 0;
                foreach (var c in Hero.MainHero.CompanionsInParty.ToList())
                {
                    var dev = c.HeroDeveloper;
                    foreach (var perk in PerkObject.All.Where(p => p != null))
                        if (!dev.GetPerkValue(perk)) { dev.AddPerk(perk); perkTotal++; }
                    heroCount++;
                }
                SetStatus("Unlocked " + perkTotal + " perk(s) across " + heroCount + " companion(s).");
            }
            catch (Exception ex) { HandleError("cheats.compperks", ex); }
            GameMenu.SwitchToMenu("taom_cheat_panel_cheats_companions");
        }

        private static void DoMaxRelationsAllLords(int amount)
        {
            try
            {
                int count = 0;
                foreach (var h in Campaign.Current.AliveHeroes.Where(h => h != null && h != Hero.MainHero && h.IsActive))
                { ChangeRelationAction.ApplyRelationChangeBetweenHeroes(Hero.MainHero, h, amount, false); count++; }
                SetStatus("Boosted relations by +" + amount + " with " + count + " active heroes.");
            }
            catch (Exception ex) { HandleError("kingdom.rellords", ex); }
            GameMenu.SwitchToMenu("taom_cheat_panel_cheats_clan");
        }

        private static void DoMaxAllOwnedSettlements()
        {
            try
            {
                var clan = Hero.MainHero?.Clan;
                if (clan == null) { SetStatus("No clan."); GameMenu.SwitchToMenu("taom_cheat_panel_cheats_clan"); return; }
                int count = 0;
                foreach (var town in clan.Fiefs)
                {
                    town.Prosperity = Math.Min(town.Prosperity + 5000f, 20000f);
                    town.Loyalty = 100f;
                    town.Security = 100f;
                    town.FoodStocks = town.FoodStocksUpperLimit();
                    var troop = ResolveSafeGarrisonTroop(town.Settlement != null ? town.Settlement.Culture : null);
                    if (troop != null && town.GarrisonParty != null)
                        town.GarrisonParty.AddElementToMemberRoster(troop, 50);
                    count++;
                }
                SetStatus("Maxed prosperity / loyalty / security / food for " + count + " fief(s).");
            }
            catch (Exception ex) { HandleError("cheats.allsett", ex); }
            GameMenu.SwitchToMenu("taom_cheat_panel_cheats_clan");
        }

        private static void DoAddBestGearItem(ItemObject.ItemTypeEnum type, string label)
        {
            try
            {
                var item = Game.Current.ObjectManager.GetObjectTypeList<ItemObject>()
                    .Where(i => i != null && i.ItemType == type)
                    .OrderByDescending(i => i.Value)
                    .FirstOrDefault();
                if (item == null) { SetStatus("No " + label + " found in object manager."); GameMenu.SwitchToMenu("taom_cheat_panel_cheats_gear"); return; }
                MobileParty.MainParty.ItemRoster.AddToCounts(item, 1);
                SetStatus("Added: " + item.Name + " (" + label + ", value " + item.Value.ToString("N0") + ").");
            }
            catch (Exception ex) { HandleError("cheats.gear." + label, ex); }
            GameMenu.SwitchToMenu("taom_cheat_panel_cheats_gear");
        }

        private static void DoAddBestHorse()
        {
            try
            {
                var horse = Game.Current.ObjectManager.GetObjectTypeList<ItemObject>()
                    .Where(i => i != null && i.HasHorseComponent && i.HorseComponent != null && !i.IsAnimal)
                    .OrderByDescending(i => i.Value)
                    .FirstOrDefault();
                if (horse == null) { SetStatus("No horse found."); GameMenu.SwitchToMenu("taom_cheat_panel_cheats_gear"); return; }
                MobileParty.MainParty.ItemRoster.AddToCounts(horse, 1);
                SetStatus("Added: " + horse.Name + " (value " + horse.Value.ToString("N0") + ").");
            }
            catch (Exception ex) { HandleError("cheats.horse", ex); }
            GameMenu.SwitchToMenu("taom_cheat_panel_cheats_gear");
        }

        private static void DoAddAllTradeGoods()
        {
            try
            {
                var goods = Game.Current.ObjectManager.GetObjectTypeList<ItemObject>()
                    .Where(i => i != null && i.ItemType == ItemObject.ItemTypeEnum.Goods)
                    .ToList();
                var roster = MobileParty.MainParty.ItemRoster;
                foreach (var item in goods)
                    roster.AddToCounts(item, 100);
                SetStatus("Added 100 of each trade good (" + goods.Count + " types).");
            }
            catch (Exception ex) { HandleError("cheats.tradegoods", ex); }
            GameMenu.SwitchToMenu("taom_cheat_panel_cheats_gear");
        }

        private static void DoHealAllHeroesGlobal()
        {
            try
            {
                int count = 0;
                foreach (var h in Campaign.Current.AliveHeroes.Where(h => h != null && h.HitPoints < h.MaxHitPoints))
                { h.HitPoints = h.MaxHitPoints; count++; }
                SetStatus("Healed " + count + " hero(es) globally.");
            }
            catch (Exception ex) { HandleError("world.healall", ex); }
            GameMenu.SwitchToMenu("taom_cheat_panel_cheats_world");
        }

        private static void DoDiscoverAllHideouts()
        {
            try
            {
                int count = 0;
                foreach (var s in Settlement.All.Where(s => s != null && s.IsHideout))
                { s.IsVisible = true; count++; }
                SetStatus("Revealed " + count + " hideout(s) on the map.");
            }
            catch (Exception ex) { HandleError("world.hideouts", ex); }
            GameMenu.SwitchToMenu("taom_cheat_panel_cheats_world");
        }

        private static void DoClearNearbyBandits()
        {
            try
            {
                var pos = Bannerlord115Compat.GetPosition2D(MobileParty.MainParty);
                var bandits = MobileParty.All
                    .Where(p => p != null && p.IsActive && p.IsBandit && Bannerlord115Compat.GetPosition2D(p).Distance(pos) < 50f)
                    .ToList();
                int count = 0;
                foreach (var b in bandits)
                {
                    try { DestroyPartyAction.Apply(MobileParty.MainParty.Party, b); count++; }
                    catch { /* party may already be gone */ }
                }
                SetStatus("Destroyed " + count + " bandit party/parties within range 50.");
            }
            catch (Exception ex) { HandleError("world.bandits", ex); }
            GameMenu.SwitchToMenu("taom_cheat_panel_cheats_world");
        }

        private static void DoCaptureNearestEnemySettlement()
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
                if (target == null) { SetStatus("No enemy settlement found."); GameMenu.SwitchToMenu("taom_cheat_panel_cheats_world"); return; }
                ChangeOwnerOfSettlementAction.ApplyByDefault(Hero.MainHero, target);
                SetStatus("Captured " + target.Name + " for " + (Hero.MainHero.Clan?.Name ?? Hero.MainHero.Name) + ".");
            }
            catch (Exception ex) { HandleError("world.capture", ex); }
            GameMenu.SwitchToMenu("taom_cheat_panel_cheats_world");
        }

        private static void DoRevealAllSettlements()
        {
            try
            {
                int count = 0;
                foreach (var settlement in Settlement.All.Where(s => s != null && !s.IsVisible))
                {
                    settlement.IsVisible = true;
                    count++;
                }

                SetStatus("Revealed " + count + " hidden settlement(s), hideout(s), or map locations.");
            }
            catch (Exception ex) { HandleError("world.revealall", ex); }
            GameMenu.SwitchToMenu("taom_cheat_panel_cheats_world");
        }

        private static void DoClearAllBandits()
        {
            try
            {
                var bandits = MobileParty.All
                    .Where(p => p != null && p.IsActive && p.IsBandit)
                    .ToList();
                int count = 0;
                foreach (var bandit in bandits)
                {
                    try { DestroyPartyAction.Apply(MobileParty.MainParty.Party, bandit); count++; }
                    catch { }
                }

                SetStatus("Destroyed " + count + " bandit party/parties across the map.");
            }
            catch (Exception ex) { HandleError("world.clearallbandits", ex); }
            GameMenu.SwitchToMenu("taom_cheat_panel_cheats_world");
        }

        private static SkillObject[] AllSkillObjects() => new[]
        {
            DefaultSkills.OneHanded,  DefaultSkills.TwoHanded,  DefaultSkills.Polearm,
            DefaultSkills.Bow,        DefaultSkills.Crossbow,   DefaultSkills.Throwing,
            DefaultSkills.Riding,     DefaultSkills.Athletics,  DefaultSkills.Crafting,
            DefaultSkills.Tactics,    DefaultSkills.Scouting,   DefaultSkills.Roguery,
            DefaultSkills.Charm,      DefaultSkills.Leadership, DefaultSkills.Trade,
            DefaultSkills.Steward,    DefaultSkills.Medicine,   DefaultSkills.Engineering
        };

        private static void DoSetSpeed(CampaignTimeControlMode mode, string label)
        {
            try { Campaign.Current.TimeControlMode = mode; SetStatus("Speed: " + label); }
            catch (Exception ex) { HandleError("time.speed", ex); }
        }

        private static void DoAdvanceDays(int days)
        {
            try
            {
                var field = typeof(Campaign).GetField("_mapTimeTracker",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (field != null)
                {
                    var tracker = field.GetValue(Campaign.Current);
                    var tick = tracker?.GetType().GetMethod("Tick",
                        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                    if (tick != null)
                    {
                        float spd = (float)CampaignTime.Days(1).ToSeconds;
                        for (int i = 0; i < days; i++) tick.Invoke(tracker, new object[] { spd });
                        SetStatus("Advanced " + days + " day(s).");
                        return;
                    }
                }
                SetStatus("Time advance unavailable — use Very Fast speed.");
            }
            catch (Exception ex) { HandleError("time.advance", ex); }
        }

        private static void DoSkipSeason()
        {
            try
            {
                int daysInSeason = 21;
                int daysUntilNext = daysInSeason - ((int)CampaignTime.Now.ToDays % daysInSeason);
                DoAdvanceDays(daysUntilNext);
                SetStatus("Skipped to next season.");
            }
            catch (Exception ex) { HandleError("time.season", ex); }
        }

        private static void DoTeleport(string sid, string name)
        {
            try
            {
                var sett = Settlement.Find(sid);
                if (sett == null) { SetStatus("Settlement not found."); GameMenu.SwitchToMenu("taom_cheat_panel_teleport"); return; }
                if (Hero.MainHero.CurrentSettlement != null)
                    LeaveSettlementAction.ApplyForParty(MobileParty.MainParty);
                Bannerlord115Compat.SetPosition2D(MobileParty.MainParty, sett.GatePosition);
                SetStatus("Teleported to " + name + ".");
                GameMenu.SwitchToMenu("taom_cheat_panel_teleport");
            }
            catch (Exception ex) { HandleError("teleport." + sid, ex); GameMenu.SwitchToMenu("taom_cheat_panel_teleport"); }
        }

        private static void DoSpawnToParty()
        {
            try
            {
                var troop = ResolveTroop(SpawnTier, SpawnCultureId);
                if (troop == null) { SetStatus("No tier " + SpawnTier + " troop found" + (string.IsNullOrEmpty(SpawnCultureId) ? "." : " for culture '" + GetCultureDisplayName(SpawnCultureId) + "'.")); GameMenu.SwitchToMenu("taom_cheat_panel_spawn"); return; }
                MobileParty.MainParty.AddElementToMemberRoster(troop, TroopCount);
                SetStatus("Added " + TroopCount + "x " + troop.Name.ToString() + " (T" + troop.Tier + ").");
            }
            catch (Exception ex) { HandleError("spawn.add", ex); }
            GameMenu.SwitchToMenu("taom_cheat_panel_spawn");
        }

        private static void DoAddEliteTroops()
        {
            try
            {
                var troop = ResolveTroop(5, SpawnCultureId);
                if (troop == null) { SetStatus("No tier 5 troop found" + (string.IsNullOrEmpty(SpawnCultureId) ? "." : " for culture '" + GetCultureDisplayName(SpawnCultureId) + "'.")); GameMenu.SwitchToMenu("taom_cheat_panel_spawn"); return; }
                MobileParty.MainParty.AddElementToMemberRoster(troop, TroopCount);
                SetStatus("Added " + TroopCount + "x elite " + troop.Name.ToString() + " (T" + troop.Tier + ").");
            }
            catch (Exception ex) { HandleError("spawn.elite", ex); }
            GameMenu.SwitchToMenu("taom_cheat_panel_spawn");
        }

        private static void DoSpawnHostile()
        {
            try
            {
                var troop = ResolveTroop(SpawnTier, SpawnCultureId);
                var banditClan = Clan.BanditFactions != null ? Clan.BanditFactions.FirstOrDefault() : null;
                if (troop == null || banditClan == null) { SetStatus("Cannot spawn: missing troop or bandit faction."); GameMenu.SwitchToMenu("taom_cheat_panel_spawn"); return; }

                var party = Bannerlord115Compat.CreateBanditParty(
                    "taom_spawn_" + Guid.NewGuid().ToString("N").Substring(0, 8),
                    banditClan,
                    Bannerlord115Compat.GetPosition2D(MobileParty.MainParty));
                if (party == null) { SetStatus("Cannot spawn: no compatible bandit party template was available."); GameMenu.SwitchToMenu("taom_cheat_panel_spawn"); return; }

                var mr = new TroopRoster(party.Party);
                var pr = new TroopRoster(party.Party);
                mr.AddToCounts(troop, TroopCount);

                var pos = Bannerlord115Compat.GetPosition2D(MobileParty.MainParty) + new Vec2(MBRandom.RandomFloatRanged(-2f, 2f), MBRandom.RandomFloatRanged(-2f, 2f));
                party.InitializeMobilePartyAroundPosition(mr, pr, Bannerlord115Compat.ToCampaignPosition(pos), 0.5f, 0.1f, false);
                TrySetPartyName(party, BuildHostilePartyName(troop, TroopCount));
                CustomPartyAiSafety.TryPatrolAroundSettlement(party, null, "hub.spawn.hostile");
                SetStatus("Spawned " + TroopCount + "x hostile " + troop.Name.ToString() + " (T" + troop.Tier + ") nearby.");
            }
            catch (Exception ex) { HandleError("spawn.hostile", ex); }
            GameMenu.SwitchToMenu("taom_cheat_panel_spawn");
        }

        private static void DoSetProsperity(int amount)
        {
            try
            {
                var sett = Settlement.CurrentSettlement;
                if (sett?.Town == null) { SetStatus("Not inside a town or castle."); GameMenu.SwitchToMenu("taom_cheat_panel_settlement"); return; }
                sett.Town.Prosperity = amount;
                SetStatus("Set prosperity of " + sett.Name.ToString() + " to " + amount.ToString("N0") + ".");
            }
            catch (Exception ex) { HandleError("settlement.prosperity", ex); }
        }

        private static void DoSetLoyalty(int amount)
        {
            try
            {
                var sett = Settlement.CurrentSettlement;
                if (sett?.Town == null) { SetStatus("Not inside a town or castle."); GameMenu.SwitchToMenu("taom_cheat_panel_settlement"); return; }
                sett.Town.Loyalty = amount;
                SetStatus("Set loyalty of " + sett.Name.ToString() + " to " + amount + ".");
            }
            catch (Exception ex) { HandleError("settlement.loyalty", ex); }
        }

        private static void DoSetSecurity(int amount)
        {
            try
            {
                var sett = Settlement.CurrentSettlement;
                if (sett?.Town == null) { SetStatus("Not inside a town or castle."); GameMenu.SwitchToMenu("taom_cheat_panel_settlement"); return; }
                sett.Town.Security = amount;
                SetStatus("Set security of " + sett.Name.ToString() + " to " + amount + ".");
            }
            catch (Exception ex) { HandleError("settlement.security", ex); }
        }

        private static void DoFillSettlementFood()
        {
            try
            {
                var sett = Settlement.CurrentSettlement;
                if (sett?.Town == null) { SetStatus("Not inside a town or castle."); GameMenu.SwitchToMenu("taom_cheat_panel_settlement"); return; }
                sett.Town.FoodStocks = sett.Town.FoodStocksUpperLimit();
                SetStatus("Filled food stocks in " + sett.Name.ToString() + ".");
            }
            catch (Exception ex) { HandleError("settlement.food", ex); }
            GameMenu.SwitchToMenu("taom_cheat_panel_settlement");
        }

        private static void DoReinforceGarrison()
        {
            try
            {
                var sett = Settlement.CurrentSettlement;
                if (sett?.Town == null) { SetStatus("Not inside a town or castle."); GameMenu.SwitchToMenu("taom_cheat_panel_settlement"); return; }
                var troop = ResolveSafeGarrisonTroop(sett.Culture);
                if (troop == null) { SetStatus("No troop found for this culture."); GameMenu.SwitchToMenu("taom_cheat_panel_settlement"); return; }
                var count = Cfg()?.SettlementGarrisonCount ?? 50;
                sett.Town.GarrisonParty.AddElementToMemberRoster(troop, count);
                SetStatus("Added " + count + "x " + troop.Name.ToString() + " to garrison of " + sett.Name.ToString() + ".");
            }
            catch (Exception ex) { HandleError("settlement.garrison", ex); }
            GameMenu.SwitchToMenu("taom_cheat_panel_settlement");
        }

        private static void DoBoostMilitia()
        {
            try
            {
                var sett = Settlement.CurrentSettlement;
                if (sett == null) { SetStatus("Not inside a settlement."); GameMenu.SwitchToMenu("taom_cheat_panel_settlement"); return; }
                sett.Militia = Math.Min(sett.Militia + 200f, 2000f);
                SetStatus("Militia in " + sett.Name.ToString() + " boosted to " + (int)sett.Militia + ".");
            }
            catch (Exception ex) { HandleError("settlement.militia", ex); }
            GameMenu.SwitchToMenu("taom_cheat_panel_settlement");
        }

        private static void DoGiveToClan()
        {
            try
            {
                var sett = Settlement.CurrentSettlement;
                if (sett?.Town == null) { SetStatus("Not inside a town or castle."); GameMenu.SwitchToMenu("taom_cheat_panel_settlement"); return; }
                var hero = Hero.MainHero;
                if (hero?.Clan == null) { SetStatus("You don't have a clan."); GameMenu.SwitchToMenu("taom_cheat_panel_settlement"); return; }
                ChangeOwnerOfSettlementAction.ApplyByDefault(hero, sett);
                SetStatus("Transferred " + sett.Name.ToString() + " to " + hero.Clan.Name.ToString() + ".");
            }
            catch (Exception ex) { HandleError("settlement.give", ex); }
            GameMenu.SwitchToMenu("taom_cheat_panel_settlement");
        }

        private static void DoFullyRestoreCurrentSettlement()
        {
            try
            {
                var sett = Settlement.CurrentSettlement;
                if (sett?.Town == null) { SetStatus("Not inside a town or castle."); GameMenu.SwitchToMenu("taom_cheat_panel_settlement"); return; }

                var town = sett.Town;
                town.Prosperity = Math.Max(town.Prosperity, 10000f);
                town.Loyalty = 100f;
                town.Security = 100f;
                town.FoodStocks = town.FoodStocksUpperLimit();
                sett.Militia = Math.Min(sett.Militia + 500f, 3000f);

                int garrisonAdded = 0;
                var troop = ResolveSafeGarrisonTroop(sett.Culture);
                if (troop != null && town.GarrisonParty != null)
                {
                    garrisonAdded = Math.Max(50, Cfg()?.SettlementGarrisonCount ?? 50);
                    town.GarrisonParty.AddElementToMemberRoster(troop, garrisonAdded);
                }

                SetStatus("Fully restored " + sett.Name.ToString() + " (food, loyalty, security, militia, prosperity"
                    + (garrisonAdded > 0 ? ", +" + garrisonAdded + " garrison" : "") + ").");
            }
            catch (Exception ex) { HandleError("settlement.restore", ex); }
            GameMenu.SwitchToMenu("taom_cheat_panel_settlement");
        }

        private static void DoMakePeaceAll()
        {
            try
            {
                var kingdom = Hero.MainHero?.Clan?.Kingdom;
                if (kingdom == null) { SetStatus("You are not in a kingdom."); GameMenu.SwitchToMenu("taom_cheat_panel_kingdom"); return; }
                int count = 0;
                foreach (var other in Kingdom.All.ToList())
                {
                    if (other == kingdom) continue;
                    if (FactionManager.IsAtWarAgainstFaction(kingdom, other))
                    { MakePeaceAction.Apply(kingdom, other); count++; }
                }
                SetStatus("Made peace with " + count + " kingdom(s).");
            }
            catch (Exception ex) { HandleError("kingdom.peace", ex); }
            GameMenu.SwitchToMenu("taom_cheat_panel_kingdom");
        }

        private static void DoBoostRelations(int amount)
        {
            try
            {
                int count = 0;
                foreach (var notable in Campaign.Current.AliveHeroes.Where(h => h != null && h.IsNotable))
                { ChangeRelationAction.ApplyRelationChangeBetweenHeroes(Hero.MainHero, notable, amount, false); count++; }
                SetStatus("Boosted relations by +" + amount + " with " + count + " notables.");
            }
            catch (Exception ex) { HandleError("kingdom.relations", ex); }
            GameMenu.SwitchToMenu("taom_cheat_panel_kingdom");
        }

        private static void DoReleasePrisoners()
        {
            try
            {
                var roster = MobileParty.MainParty.PrisonRoster;
                int count = roster.TotalManCount;
                roster.Clear();
                SetStatus("Released " + count + " prisoner(s).");
            }
            catch (Exception ex) { HandleError("kingdom.prisoners", ex); }
            GameMenu.SwitchToMenu("taom_cheat_panel_kingdom");
        }

        private static void DoRecoverWounded()
        {
            try
            {
                int count = 0;
                var roster = MobileParty.MainParty.MemberRoster;
                for (int i = 0; i < roster.Count; i++)
                {
                    var el = roster.GetElementCopyAtIndex(i);
                    if (el.WoundedNumber > 0) { roster.AddToCounts(el.Character, 0, woundedCount: -el.WoundedNumber); count += el.WoundedNumber; }
                }
                SetStatus("Recovered " + count + " wounded troop(s).");
            }
            catch (Exception ex) { HandleError("kingdom.wounded", ex); }
            GameMenu.SwitchToMenu("taom_cheat_panel_kingdom");
        }

        private static void BuildPresetMenu(CampaignGameStarter s, string menuId, string backId,
            int[] presets, string unit, string fmt, Action<int> setter)
        {
            s.AddGameMenu(menuId, "{=!}Select " + unit + ":", _ => { });
            for (int i = 0; i < presets.Length; i++)
            {
                var v = presets[i];
                var label = fmt == "N0" ? v.ToString("N0") : v.ToString();
                Opt(s, menuId, menuId + "_" + v, "{=!}" + label + " " + unit, Always,
                    _ => { setter(v); GameMenu.SwitchToMenu(backId); }, false, i);
            }
            Opt(s, menuId, menuId + "_back", "{=!}Back", Always, _ => GameMenu.SwitchToMenu(backId), true, presets.Length);
        }

        private static void BuildDirectPresetMenu(CampaignGameStarter s, string menuId, string backId,
            int[] presets, string unit, string fmt, Action<int> apply)
        {
            s.AddGameMenu(menuId, "{=!}Set " + unit + " to:", _ => { });
            for (int i = 0; i < presets.Length; i++)
            {
                var v = presets[i];
                var label = fmt == "N0" ? v.ToString("N0") : v.ToString();
                Opt(s, menuId, menuId + "_" + v, "{=!}" + label, Always,
                    _ => { apply(v); GameMenu.SwitchToMenu(backId); }, false, i, requiresConfirmation: true);
            }
            Opt(s, menuId, menuId + "_back", "{=!}Back", Always, _ => GameMenu.SwitchToMenu(backId), true, presets.Length);
        }

        private static void Opt(CampaignGameStarter s,
            string menuId, string optId, string text,
            GameMenuOption.OnConditionDelegate cond,
            GameMenuOption.OnConsequenceDelegate cons,
            bool isLeave = false, int idx = 0, bool requiresConfirmation = false)
        {
            if (requiresConfirmation)
            {
                var command = cons;
                cons = args => CheatPanelHost.RequestConfirmation(
                    text.Replace("{=!}", "") + "?", () => command(args));
            }
            s.AddGameMenuOption(menuId, optId, text, cond, cons, isLeave, idx);
        }

        private static bool Always(MenuCallbackArgs args)
        {
            args.optionLeaveType = GameMenuOption.LeaveType.Default;
            return true;
        }

        private static bool CanOpenTownHubEntry(MenuCallbackArgs args)
        {
            return CanOpenHubEntry(args, cfg => cfg.ShowInTowns);
        }

        private static bool CanOpenCastleHubEntry(MenuCallbackArgs args)
        {
            return CanOpenHubEntry(args, cfg => cfg.ShowInCastles);
        }

        private static bool CanOpenVillageHubEntry(MenuCallbackArgs args)
        {
            return CanOpenHubEntry(args, cfg => cfg.ShowInVillages);
        }

        private static bool CanOpenHubEntry(
            MenuCallbackArgs args,
            Func<Settings.CheatPanelSettings, bool> isVisible)
        {
            args.optionLeaveType = GameMenuOption.LeaveType.Default;
            var cfg = Cfg();
            return cfg == null || isVisible(cfg);
        }

        private static void OpenHubRoot(MenuCallbackArgs args)
        {
            _returnMenu = Campaign.Current.CurrentMenuContext?.GameMenu?.StringId;
            _returnSettlement = Settlement.CurrentSettlement;
            LastStatus = "";
            GameMenu.SwitchToMenu("taom_cheat_panel_root");
        }

        internal void OpenFromHotkey()
        {
            if (!_menusRegistered) return;
            var menu = Campaign.Current.CurrentMenuContext?.GameMenu?.StringId;
            if (!CheatPanelHotkeyOptions.CanOpenFrom(menu)) return;
            _returnMenu = menu;
            _returnSettlement = Settlement.CurrentSettlement;
            LastStatus = "";
            GameMenu.ActivateGameMenu("taom_cheat_panel_root");
        }

        private static bool CondTownOrCastle(MenuCallbackArgs args)
        {
            args.optionLeaveType = GameMenuOption.LeaveType.Default;
            var sett = Settlement.CurrentSettlement;
            bool valid = sett?.Town != null;
            if (!valid)
                args.Tooltip = new TaleWorlds.Localization.TextObject("{=!}Only available inside a town or castle.");
            return valid;
        }

        private static bool CondSettlementToolsEnabled(MenuCallbackArgs args)
        {
            args.optionLeaveType = GameMenuOption.LeaveType.Default;
            var cfg = Cfg();
            return cfg == null || cfg.EnableSettlementTools;
        }

        private static void BackToRoot() { LastStatus = ""; GameMenu.SwitchToMenu("taom_cheat_panel_root"); }

        private static void CloseHub()
        {
            LastStatus = "";
            var returnMenu = _returnMenu;
            var returnSettlement = _returnSettlement;
            _returnMenu = null;
            _returnSettlement = null;
            if (!string.IsNullOrEmpty(returnMenu) && returnSettlement == Settlement.CurrentSettlement
                && IsSupportedSettlement(returnSettlement))
            {
                GameMenu.SwitchToMenu(returnMenu);
                return;
            }
            var settlement = Settlement.CurrentSettlement;
            if (IsSupportedSettlement(settlement))
            {
                GameMenu.SwitchToMenu(GetSettlementMenuId(settlement));
                return;
            }

            GameMenu.ExitToLast();
        }

        private static bool IsSupportedSettlement(Settlement settlement)
        {
            return settlement != null && (settlement.IsTown || settlement.IsCastle || settlement.IsVillage);
        }

        private static string GetSettlementMenuId(Settlement settlement)
        {
            if (settlement != null && settlement.IsVillage)
            {
                return "village";
            }

            if (settlement != null && settlement.IsCastle)
            {
                return "castle";
            }

            return "town";
        }

        private static void SetStatus(string msg)
        {
            LastStatus = msg;
            SupportRuntime.RecentActionsLog.Record(msg);
            SupportRuntime.Log.Info("[HubMenu] " + msg);
        }

        private static void HandleError(string key, Exception ex)
        {
            LastStatus = "Error: " + ex.Message;
            SupportRuntime.Log.ErrorOnce(key, ex);
        }

        private static CharacterObject ResolveTroop(int tier, string cultureId = "", TroopRolePreference preference = TroopRolePreference.Any)
        {
            return TroopEquipmentSafety.ResolveBestArmoredTroop(tier, cultureId, preference);
        }

        private static CharacterObject ResolveSafeGarrisonTroop(CultureObject culture)
        {
            var elite = culture != null ? culture.EliteBasicTroop : null;
            var basic = culture != null ? culture.BasicTroop : null;
            string cultureId = culture != null ? culture.StringId : "";
            return TroopEquipmentSafety.IsSpawnSafe(elite)
                ? elite
                : TroopEquipmentSafety.IsSpawnSafe(basic)
                    ? basic
                    : ResolveTroop(4, cultureId, TroopRolePreference.Infantry)
                        ?? ResolveTroop(4, "", TroopRolePreference.Infantry);
        }

        private static string GetCultureDisplayName(string cultureId)
        {
            if (string.IsNullOrEmpty(cultureId)) return "Any";
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

        private static int AddItemById(string itemId, int count)
        {
            var item = Game.Current?.ObjectManager?.GetObject<ItemObject>(itemId);
            if (item == null)
            {
                return 0;
            }

            MobileParty.MainParty.ItemRoster.AddToCounts(item, count);
            return count;
        }

        private static string BuildAdjustmentText()
        {
            try
            {
                var sb = new System.Text.StringBuilder("Cheat Adjustments\n\n");
                sb.AppendLine("Configure passive adjustments in Cheat Panel Settings.");
                sb.AppendLine("Master switch: " + OnOff(TAOM_CheatPanel.Runtime.CheatPanelSettingsAccess.EnablePassiveCheatAdjustments));
                sb.AppendLine();
                sb.AppendLine("Daily Stipends");
                sb.AppendLine("Gold: " + TAOM_CheatPanel.Runtime.CheatPanelSettingsAccess.DailyGoldStipend.ToString("N0")
                    + "   Influence: " + TAOM_CheatPanel.Runtime.CheatPanelSettingsAccess.DailyInfluenceStipend.ToString("N0")
                    + "   Renown: " + TAOM_CheatPanel.Runtime.CheatPanelSettingsAccess.DailyRenownStipend.ToString("N0"));
                sb.AppendLine();
                sb.AppendLine("Party Upkeep");
                sb.AppendLine("Hero heal: " + OnOff(TAOM_CheatPanel.Runtime.CheatPanelSettingsAccess.KeepMainHeroHealed)
                    + "   Companion heal: " + OnOff(TAOM_CheatPanel.Runtime.CheatPanelSettingsAccess.KeepCompanionsHealed)
                    + "   Recover wounded: " + OnOff(TAOM_CheatPanel.Runtime.CheatPanelSettingsAccess.RecoverWoundedTroopsDaily));
                sb.AppendLine("Morale 100: " + OnOff(TAOM_CheatPanel.Runtime.CheatPanelSettingsAccess.KeepPartyMoraleAtMax)
                    + "   Restock food: " + OnOff(TAOM_CheatPanel.Runtime.CheatPanelSettingsAccess.AutoRestockPartyFood)
                    + "   Smithing stamina: " + OnOff(TAOM_CheatPanel.Runtime.CheatPanelSettingsAccess.AutoRestoreSmithingStamina));
                sb.AppendLine();
                sb.AppendLine("Owned Settlement Stabiliser");
                sb.AppendLine("Enabled: " + OnOff(TAOM_CheatPanel.Runtime.CheatPanelSettingsAccess.StabiliseOwnedSettlements)
                    + "   Loyalty floor: " + TAOM_CheatPanel.Runtime.CheatPanelSettingsAccess.OwnedSettlementLoyaltyFloor
                    + "   Security floor: " + TAOM_CheatPanel.Runtime.CheatPanelSettingsAccess.OwnedSettlementSecurityFloor);
                sb.AppendLine("Prosperity floor: " + TAOM_CheatPanel.Runtime.CheatPanelSettingsAccess.OwnedSettlementProsperityFloor.ToString("N0")
                    + "   Fill food: " + OnOff(TAOM_CheatPanel.Runtime.CheatPanelSettingsAccess.FillOwnedSettlementFood)
                    + "   Daily garrison: " + TAOM_CheatPanel.Runtime.CheatPanelSettingsAccess.DailyOwnedGarrisonReinforcement);

                if (LastStatus.Length > 0)
                    sb.Append("\n\u25ba " + LastStatus);

                return sb.ToString();
            }
            catch
            {
                return "Cheat Adjustments\n\nAdjustment status unavailable.";
            }
        }

        private static string OnOff(bool value)
        {
            return value ? "ON" : "off";
        }

        private static string BuildTimeText()
        {
            try
            {
                var now = CampaignTime.Now;
                var seasons = new[] { "Spring", "Summer", "Autumn", "Winter" };
                var season = seasons[(int)now.GetSeasonOfYear];
                var mode = Campaign.Current.TimeControlMode;
                var speed = mode == CampaignTimeControlMode.Stop ? "Stopped"
                            : mode == CampaignTimeControlMode.StoppablePlay ? "Normal"
                            : mode == CampaignTimeControlMode.FastForwardStop ? "Fast"
                            : mode == CampaignTimeControlMode.UnstoppableFastForward ? "Very Fast"
                            : mode.ToString();
                return "Time & Speed\n\nDate: Day " + (int)now.ToDays + " of Year " + now.GetYear + "  (" + season + ")\nSpeed: " + speed
                    + (LastStatus.Length > 0 ? "\n\n\u25ba " + LastStatus : "");
            }
            catch { return "Time & Speed\n\n(unavailable)"; }
        }

        private static string BuildSettlementText()
        {
            try
            {
                var sett = Settlement.CurrentSettlement;
                if (sett == null)
                    return "Settlement Tools\n\nNot currently inside a settlement.\n" +
                           "Enter a town, castle, or village to use these tools." +
                           (LastStatus.Length > 0 ? "\n\n\u25ba " + LastStatus : "");

                var sb = new System.Text.StringBuilder("Settlement Tools\n\n");
                sb.AppendLine("Name:    " + sett.Name.ToString());
                sb.AppendLine("Type:    " + (sett.IsTown ? "Town" : sett.IsCastle ? "Castle" : "Village"));
                sb.AppendLine("Culture: " + (sett.Culture?.Name?.ToString() ?? "?"));
                sb.AppendLine("Owner:   " + (sett.OwnerClan?.Name?.ToString() ?? "Unowned"));

                if (sett.Town != null)
                {
                    sb.AppendLine("Prosperity: " + ((int)sett.Town.Prosperity).ToString("N0"));
                    sb.AppendLine("Loyalty:    " + sett.Town.Loyalty.ToString("F1"));
                    sb.AppendLine("Security:   " + sett.Town.Security.ToString("F1"));
                    sb.AppendLine("Food:       " + ((int)sett.Town.FoodStocks) + " / " + ((int)sett.Town.FoodStocksUpperLimit()));
                    sb.AppendLine("Garrison:   " + (sett.Town.GarrisonParty?.MemberRoster?.TotalManCount ?? 0));
                }

                sb.AppendLine("Militia:    " + (int)sett.Militia);

                if (LastStatus.Length > 0)
                    sb.Append("\n\u25ba " + LastStatus);

                return sb.ToString();
            }
            catch { return "Settlement Tools\n\n(data unavailable)"; }
        }

        private static string BuildHistoryText()
        {
            var entries = SupportRuntime.RecentActionsLog.GetSnapshot();
            if (entries.Count == 0) return "Action History\n\nNo actions recorded this session.";
            var sb = new System.Text.StringBuilder("Action History  (" + entries.Count + " entries)\n\n");
            foreach (var e in entries.Take(15)) sb.AppendLine("[" + e.Timestamp + "] " + e.Description);
            if (entries.Count > 15) sb.AppendLine("... and " + (entries.Count - 15) + " more");
            return sb.ToString();
        }

        private static Type ResolveLoadedType(string fullName, string assemblyName)
        {
            if (string.IsNullOrWhiteSpace(fullName))
                return null;

            Assembly[] assemblies;
            try
            {
                assemblies = AppDomain.CurrentDomain.GetAssemblies();
            }
            catch
            {
                return null;
            }

            for (int i = 0; i < assemblies.Length; i++)
            {
                Assembly assembly = assemblies[i];
                if (assembly == null)
                    continue;

                try
                {
                    if (!string.IsNullOrWhiteSpace(assemblyName)
                        && !string.Equals(assembly.GetName().Name, assemblyName, StringComparison.OrdinalIgnoreCase))
                        continue;

                    Type type = assembly.GetType(fullName, false, false);
                    if (type != null)
                        return type;
                }
                catch
                {
                }
            }

            return null;
        }

        private static Settings.CheatPanelSettings Cfg()
        {
            try { return Settings.CheatPanelSettings.Instance; } catch { return null; }
        }
    }
}
