using System;
using MCM.Abstractions.Base.Global;
using MCM.Abstractions.FluentBuilder;
using MCM.Common;
using TAOM_CheatPanel.Settings;
using TaleWorlds.Library;

namespace TAOM_CheatPanel.UI
{
    internal static class CheatPanelMcm
    {
        private static FluentGlobalSettings _menu;
        private static Dropdown<string> _openPanelKey;
        private static CheatPanelSettings Settings => CheatPanelSettings.Instance;

        internal static void Register()
        {
            if (_menu != null) return;
            try
            {
                var builder = BaseSettingsBuilder.Create("TAOM_CheatPanel_Settings", "TAOM Cheat Panel");
                if (builder == null) throw new InvalidOperationException("MCM settings builder is unavailable.");
                builder.SetFolderName("TAOM_CheatPanel")
                    // No MCM serializer handles this format. Keep the existing XML
                    // store authoritative for both the native menu and MCM Apply.
                    .SetFormat("taom-cheat-panel-owned-xml")
                    .SetOnPropertyChanged((sender, args) =>
                    {
                        if (args.PropertyName == "SAVE_TRIGGERED" && !CheatPanelSettings.Save())
                            InformationManager.DisplayMessage(new InformationMessage("Cheat settings could not be saved. See Action History."));
                    });
                builder.CreateGroup("General", group =>
                {
                    var selected = Array.IndexOf(CheatPanelHotkeyOptions.Names, Settings.OpenPanelKey);
                    _openPanelKey = new Dropdown<string>(CheatPanelHotkeyOptions.Names, Math.Max(0, selected));
                    _openPanelKey.PropertyChanged += (sender, args) =>
                    {
                        if (args.PropertyName == "SelectedIndex")
                        {
                            var index = _openPanelKey.SelectedIndex;
                            Settings.OpenPanelKey = index >= 0 && index < _openPanelKey.Count
                                ? _openPanelKey[index] : "Disabled";
                        }
                    };
                    group.AddDropdown("OpenPanelKey", "Open panel key", 10,
                        new ProxyRef<Dropdown<string>>(() => _openPanelKey,
                            value => _openPanelKey.SelectedIndex = value?.SelectedIndex ?? 0),
                        row => row.SetRequireRestart(false).SetHintText("Open the Cheat Panel on the campaign map or in a settlement menu. Disabled turns the shortcut off. Choose a key unused by other controls; D0-D9 are the number-row keys."));
                    Bool(group, "OpenPanelRequireControl", "Hold Ctrl for panel shortcut", () => Settings.OpenPanelRequireControl,
                        v => Settings.OpenPanelRequireControl = v, "Default: Ctrl+F10. Alt and Shift combinations do not open the panel.");
                    Bool(group, "Enabled", "Enable cheats", () => Settings.Enabled, v => Settings.Enabled = v,
                        "Master switch for Cheat Panel actions and automatic cheats.");
                    Bool(group, "ShowConfirmations", "Confirm risky actions", () => Settings.ShowConfirmations, v => Settings.ShowConfirmations = v,
                        "Ask before manual cheat actions.");
                });
                builder.CreateGroup("Player progression", group =>
                {
                    Bool(group, "TakeBothPerks", "Take both perks", () => Settings.TakeBothPerks, v => Settings.TakeBothPerks = v,
                        "Main hero only. Grants both choices at earned skill tiers. Existing tiers update by the next campaign hour. Turning off does not remove perks.");
                    Integer(group, "PlayerXpMultiplier", "Player XP multiplier", 1, 10,
                        () => Settings.PlayerXpMultiplier, v => Settings.PlayerXpMultiplier = v,
                        "Normal main-hero skill and level XP. 1 is unchanged. Manual Cheat Panel XP is not multiplied. Other XP mods may stack.");
                    Integer(group, "ExtraAttributePointsPerLevel", "Extra attribute points per level", 0, 10,
                        () => Settings.ExtraAttributePointsPerLevel, v => Settings.ExtraAttributePointsPerLevel = v,
                        "Extra unspent points on future main-hero level-ups. 0 is unchanged; no retroactive awards.");
                    Integer(group, "ExtraFocusPointsPerLevel", "Extra focus points per level", 0, 10,
                        () => Settings.ExtraFocusPointsPerLevel, v => Settings.ExtraFocusPointsPerLevel = v,
                        "Extra unspent points on future main-hero level-ups. 0 is unchanged; no retroactive awards.");
                });
                builder.CreateGroup("Daily upkeep", group =>
                {
                    const string daily = "Requires Enable cheats and Enable daily upkeep. Applies on the next daily campaign tick.";
                    Bool(group, "EnablePassiveCheatAdjustments", "Enable daily upkeep", () => Settings.EnablePassiveCheatAdjustments, v => Settings.EnablePassiveCheatAdjustments = v, "Master switch for daily upkeep, stipends and settlement adjustments.");
                    Bool(group, "KeepMainHeroHealed", "Heal main hero daily", () => Settings.KeepMainHeroHealed, v => Settings.KeepMainHeroHealed = v, daily);
                    Bool(group, "KeepCompanionsHealed", "Heal companions daily", () => Settings.KeepCompanionsHealed, v => Settings.KeepCompanionsHealed = v, daily);
                    Bool(group, "RecoverWoundedTroopsDaily", "Recover wounded troops daily", () => Settings.RecoverWoundedTroopsDaily, v => Settings.RecoverWoundedTroopsDaily = v, daily);
                    Bool(group, "KeepPartyMoraleAtMax", "Restore party morale daily", () => Settings.KeepPartyMoraleAtMax, v => Settings.KeepPartyMoraleAtMax = v, daily);
                    Bool(group, "AutoRestockPartyFood", "Restock party food daily", () => Settings.AutoRestockPartyFood, v => Settings.AutoRestockPartyFood = v, daily);
                    Bool(group, "AutoRestoreSmithingStamina", "Restore smithing stamina daily", () => Settings.AutoRestoreSmithingStamina, v => Settings.AutoRestoreSmithingStamina = v, daily);
                    Integer(group, "DailyGoldStipend", "Daily gold", 0, 1000000, () => Settings.DailyGoldStipend, v => Settings.DailyGoldStipend = v, daily);
                    Integer(group, "DailyInfluenceStipend", "Daily influence", 0, 5000, () => Settings.DailyInfluenceStipend, v => Settings.DailyInfluenceStipend = v, daily);
                    Integer(group, "DailyRenownStipend", "Daily renown", 0, 10000, () => Settings.DailyRenownStipend, v => Settings.DailyRenownStipend = v, daily);
                });
                _menu = builder.BuildAsGlobal();
                _menu.Register();
            }
            catch (Exception error)
            {
                _menu = null;
                Runtime.Log.ErrorOnce("settings.mcm.register", error);
                InformationManager.DisplayMessage(new InformationMessage("Cheat Panel MCM could not open. Use the panel's Settings menu; see Action History."));
            }
        }

        private static void Bool(ISettingsPropertyGroupBuilder group, string id, string label, Func<bool> get, Action<bool> set, string hint)
        {
            group.AddBool(id, label, new ProxyRef<bool>(get, set), row => row.SetRequireRestart(false).SetHintText(hint));
        }

        private static void Integer(ISettingsPropertyGroupBuilder group, string id, string label, int min, int max, Func<int> get, Action<int> set, string hint)
        {
            group.AddInteger(id, label, min, max, new ProxyRef<int>(
                () => Runtime.ProgressionRules.Clamp(get(), min, max), v => set(Runtime.ProgressionRules.Clamp(v, min, max))),
                row => row.SetRequireRestart(false).SetHintText(hint));
        }

        internal static void Unregister()
        {
            _menu?.Unregister();
            _menu = null;
            _openPanelKey = null;
        }
    }
}
