namespace TAOM_CheatPanel.Settings
{
    public sealed partial class CheatPanelSettings
    {
        public string Id => "TAOM_CheatPanel_Settings";
        public string DisplayName => "TAOM Cheat Panel";
        public string FolderName => "TAOM_CheatPanel";
        public string FormatType => "xml";

        public bool Enabled { get; set; } = true;

        public bool ShowConfirmations { get; set; } = true;

        public string OpenPanelKey { get; set; } = "F10";

        public bool OpenPanelRequireControl { get; set; } = true;

        public bool TakeBothPerks { get; set; } = false;

        public int PlayerXpMultiplier { get; set; } = 1;

        public int ExtraAttributePointsPerLevel { get; set; } = 0;

        public int ExtraFocusPointsPerLevel { get; set; } = 0;

        public bool ShowInTowns { get; set; } = true;

        public bool ShowInCastles { get; set; } = true;

        public bool ShowInVillages { get; set; } = true;

        public bool EnableSettlementTools { get; set; } = true;

        public int SettlementGarrisonCount { get; set; } = 50;

        public int DefaultGold { get; set; } = 10000;

        public int DefaultXp { get; set; } = 5000;

        public int DefaultTroopCount { get; set; } = 50;

        public int DefaultRenown { get; set; } = 500;

        public int DefaultInfluence { get; set; } = 200;

        public int DefaultSpawnTier { get; set; } = 3;

        public bool EnablePassiveCheatAdjustments { get; set; } = false;

        public int DailyGoldStipend { get; set; } = 0;

        public int DailyInfluenceStipend { get; set; } = 0;

        public int DailyRenownStipend { get; set; } = 0;

        public bool KeepMainHeroHealed { get; set; } = false;

        public bool KeepCompanionsHealed { get; set; } = false;

        public bool RecoverWoundedTroopsDaily { get; set; } = false;

        public bool KeepPartyMoraleAtMax { get; set; } = false;

        public bool AutoRestockPartyFood { get; set; } = false;

        public bool AutoRestoreSmithingStamina { get; set; } = false;

        public bool StabiliseOwnedSettlements { get; set; } = false;

        public int OwnedSettlementLoyaltyFloor { get; set; } = 75;

        public int OwnedSettlementSecurityFloor { get; set; } = 75;

        public int OwnedSettlementProsperityFloor { get; set; } = 0;

        public bool FillOwnedSettlementFood { get; set; } = false;

        public int DailyOwnedGarrisonReinforcement { get; set; } = 0;

    }
}
