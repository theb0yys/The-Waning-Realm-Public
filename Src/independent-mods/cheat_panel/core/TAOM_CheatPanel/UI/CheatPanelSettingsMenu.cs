using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TAOM_CheatPanel.Settings;

namespace TAOM_CheatPanel.UI
{
    internal static class CheatPanelSettingsMenu
    {
        private const string Root = "taom_cheat_panel_settings";
        private static CheatPanelSettings Settings => CheatPanelSettings.Instance;

        public static void Register(CampaignGameStarter starter)
        {
            starter.AddGameMenu(Root, "{=!}TAOM Cheat Panel Settings\n\nChanges save automatically. Passive cheats start disabled.", _ => { });
            AddBool(starter, "Enabled", "Enable TAOM Cheat Panel", () => Settings.Enabled, value => Settings.Enabled = value);
            AddBool(starter, "ShowConfirmations", "Ask Before Risky Actions", () => Settings.ShowConfirmations, value => Settings.ShowConfirmations = value);
            AddBool(starter, "TakeBothPerks", "Take Both Earned Perks (Main Hero)", () => Settings.TakeBothPerks, value => Settings.TakeBothPerks = value);
            AddInteger(starter, "PlayerXpMultiplier", "Player XP Multiplier", () => Settings.PlayerXpMultiplier, value => Settings.PlayerXpMultiplier = value, new[] { 1, 2, 3, 5, 10 });
            AddInteger(starter, "ExtraAttributePointsPerLevel", "Extra Attribute Points per Level", () => Settings.ExtraAttributePointsPerLevel, value => Settings.ExtraAttributePointsPerLevel = value, new[] { 0, 1, 2, 3, 5, 10 });
            AddInteger(starter, "ExtraFocusPointsPerLevel", "Extra Focus Points per Level", () => Settings.ExtraFocusPointsPerLevel, value => Settings.ExtraFocusPointsPerLevel = value, new[] { 0, 1, 2, 3, 5, 10 });
            AddInteger(starter, "SettlementGarrisonCount", "Troops Added to Garrison", () => Settings.SettlementGarrisonCount, value => Settings.SettlementGarrisonCount = value, new[] { 10, 25, 50, 75, 100, 200, 500 });
            AddInteger(starter, "DefaultGold", "Gold to Add by Default", () => Settings.DefaultGold, value => Settings.DefaultGold = value, new[] { 100, 200, 500, 1000, 5000, 10000, 50000, 100000, 1000000 });
            AddInteger(starter, "DefaultXp", "XP to Add by Default", () => Settings.DefaultXp, value => Settings.DefaultXp = value, new[] { 100, 200, 500, 1000, 5000, 10000, 50000, 100000 });
            AddInteger(starter, "DefaultTroopCount", "Troops to Add by Default", () => Settings.DefaultTroopCount, value => Settings.DefaultTroopCount = value, new[] { 1, 5, 10, 25, 50, 75, 100, 200, 500 });
            AddInteger(starter, "DefaultRenown", "Renown to Add by Default", () => Settings.DefaultRenown, value => Settings.DefaultRenown = value, new[] { 10, 25, 50, 75, 100, 200, 500, 1000, 5000, 10000 });
            AddInteger(starter, "DefaultInfluence", "Influence to Add by Default", () => Settings.DefaultInfluence, value => Settings.DefaultInfluence = value, new[] { 10, 25, 50, 75, 100, 200, 500, 1000, 5000 });
            AddInteger(starter, "DefaultSpawnTier", "Troop Tier to Spawn by Default", () => Settings.DefaultSpawnTier, value => Settings.DefaultSpawnTier = value, new[] { 1, 3, 5 });
            AddBool(starter, "EnablePassiveCheatAdjustments", "Enable Passive Cheat Adjustments", () => Settings.EnablePassiveCheatAdjustments, value => Settings.EnablePassiveCheatAdjustments = value);
            AddInteger(starter, "DailyGoldStipend", "Daily Gold Stipend", () => Settings.DailyGoldStipend, value => Settings.DailyGoldStipend = value, new[] { 0, 1, 5, 10, 25, 50, 75, 100, 200, 500, 1000, 5000, 10000, 50000, 100000, 1000000 });
            AddInteger(starter, "DailyInfluenceStipend", "Daily Influence Stipend", () => Settings.DailyInfluenceStipend, value => Settings.DailyInfluenceStipend = value, new[] { 0, 1, 5, 10, 25, 50, 75, 100, 200, 500, 1000, 5000 });
            AddInteger(starter, "DailyRenownStipend", "Daily Renown Stipend", () => Settings.DailyRenownStipend, value => Settings.DailyRenownStipend = value, new[] { 0, 1, 5, 10, 25, 50, 75, 100, 200, 500, 1000, 5000, 10000 });
            AddBool(starter, "KeepMainHeroHealed", "Keep Main Hero Healed", () => Settings.KeepMainHeroHealed, value => Settings.KeepMainHeroHealed = value);
            AddBool(starter, "KeepCompanionsHealed", "Keep Companions Healed", () => Settings.KeepCompanionsHealed, value => Settings.KeepCompanionsHealed = value);
            AddBool(starter, "RecoverWoundedTroopsDaily", "Recover Wounded Troops Daily", () => Settings.RecoverWoundedTroopsDaily, value => Settings.RecoverWoundedTroopsDaily = value);
            AddBool(starter, "KeepPartyMoraleAtMax", "Keep Party Morale at 100", () => Settings.KeepPartyMoraleAtMax, value => Settings.KeepPartyMoraleAtMax = value);
            AddBool(starter, "AutoRestockPartyFood", "Auto-Restock Party Food", () => Settings.AutoRestockPartyFood, value => Settings.AutoRestockPartyFood = value);
            AddBool(starter, "AutoRestoreSmithingStamina", "Auto-Restore Smithing Stamina", () => Settings.AutoRestoreSmithingStamina, value => Settings.AutoRestoreSmithingStamina = value);
            AddBool(starter, "StabiliseOwnedSettlements", "Stabilise Owned Settlements", () => Settings.StabiliseOwnedSettlements, value => Settings.StabiliseOwnedSettlements = value);
            AddInteger(starter, "OwnedSettlementLoyaltyFloor", "Owned Settlement Loyalty Floor", () => Settings.OwnedSettlementLoyaltyFloor, value => Settings.OwnedSettlementLoyaltyFloor = value, new[] { 0, 1, 5, 10, 25, 50, 75, 100 });
            AddInteger(starter, "OwnedSettlementSecurityFloor", "Owned Settlement Security Floor", () => Settings.OwnedSettlementSecurityFloor, value => Settings.OwnedSettlementSecurityFloor = value, new[] { 0, 1, 5, 10, 25, 50, 75, 100 });
            AddInteger(starter, "OwnedSettlementProsperityFloor", "Owned Settlement Prosperity Floor", () => Settings.OwnedSettlementProsperityFloor, value => Settings.OwnedSettlementProsperityFloor = value, new[] { 0, 1, 5, 10, 25, 50, 75, 100, 200, 500, 1000, 5000, 10000, 20000 });
            AddBool(starter, "FillOwnedSettlementFood", "Fill Owned Settlement Food", () => Settings.FillOwnedSettlementFood, value => Settings.FillOwnedSettlementFood = value);
            AddInteger(starter, "DailyOwnedGarrisonReinforcement", "Daily Owned Garrison Reinforcement", () => Settings.DailyOwnedGarrisonReinforcement, value => Settings.DailyOwnedGarrisonReinforcement = value, new[] { 0, 1, 5, 10, 25, 50, 75, 100, 200 });
            starter.AddGameMenuOption(Root, "cp_settings_back", "{=!}Back", Always,
                _ => GameMenu.SwitchToMenu("taom_cheat_panel_root"), true);
        }

        private static bool Always(MenuCallbackArgs args)
        {
            args.optionLeaveType = GameMenuOption.LeaveType.Default;
            return true;
        }

        private static void AddBool(CampaignGameStarter starter, string key, string label, Func<bool> get, Action<bool> set)
        {
            var variable = "TAOM_CP_SETTING_" + key;
            starter.AddGameMenuOption(Root, "cp_setting_" + key, "{=!}" + label + ": {" + variable + "}",
                args => { MBTextManager.SetTextVariable(variable, get() ? "ON" : "OFF"); return Always(args); },
                _ => { set(!get()); Save(); GameMenu.SwitchToMenu(Root); });
        }

        private static void AddInteger(CampaignGameStarter starter, string key, string label, Func<int> get, Action<int> set, int[] values)
        {
            var menu = Root + "_" + key;
            var variable = "TAOM_CP_SETTING_" + key;
            starter.AddGameMenuOption(Root, "cp_setting_" + key, "{=!}" + label + ": {" + variable + "}",
                args => { MBTextManager.SetTextVariable(variable, get().ToString("N0")); return Always(args); },
                _ => GameMenu.SwitchToMenu(menu));
            starter.AddGameMenu(menu, "{=!}" + label, _ => { });
            foreach (var value in values)
                starter.AddGameMenuOption(menu, menu + "_" + value, "{=!}" + value.ToString("N0"), Always,
                    _ => { set(value); Save(); GameMenu.SwitchToMenu(Root); });
            starter.AddGameMenuOption(menu, menu + "_back", "{=!}Back", Always, _ => GameMenu.SwitchToMenu(Root), true);
        }

        private static void Save()
        {
            if (!CheatPanelSettings.Save())
                InformationManager.DisplayMessage(new InformationMessage("Cheat settings changed for this session, but could not be saved. See Action History."));
        }
    }
}
