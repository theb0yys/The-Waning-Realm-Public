using System;
using TAOM_CheatPanel.Settings;

namespace TAOM_CheatPanel.Runtime
{
    internal static class CheatPanelSettingsAccess
    {
        public static bool EnablePassiveCheatAdjustments
        {
            get { return ReadBool(delegate(CheatPanelSettings s) { return s.Enabled && s.EnablePassiveCheatAdjustments; }, false); }
        }

        public static int DailyGoldStipend
        {
            get { return Clamp(ReadInt(delegate(CheatPanelSettings s) { return s.DailyGoldStipend; }, 0), 0, 1000000); }
        }

        public static int DailyInfluenceStipend
        {
            get { return Clamp(ReadInt(delegate(CheatPanelSettings s) { return s.DailyInfluenceStipend; }, 0), 0, 5000); }
        }

        public static int DailyRenownStipend
        {
            get { return Clamp(ReadInt(delegate(CheatPanelSettings s) { return s.DailyRenownStipend; }, 0), 0, 10000); }
        }

        public static bool KeepMainHeroHealed
        {
            get { return ReadBool(delegate(CheatPanelSettings s) { return s.KeepMainHeroHealed; }, false); }
        }

        public static bool KeepCompanionsHealed
        {
            get { return ReadBool(delegate(CheatPanelSettings s) { return s.KeepCompanionsHealed; }, false); }
        }

        public static bool RecoverWoundedTroopsDaily
        {
            get { return ReadBool(delegate(CheatPanelSettings s) { return s.RecoverWoundedTroopsDaily; }, false); }
        }

        public static bool KeepPartyMoraleAtMax
        {
            get { return ReadBool(delegate(CheatPanelSettings s) { return s.KeepPartyMoraleAtMax; }, false); }
        }

        public static bool AutoRestockPartyFood
        {
            get { return ReadBool(delegate(CheatPanelSettings s) { return s.AutoRestockPartyFood; }, false); }
        }

        public static bool AutoRestoreSmithingStamina
        {
            get { return ReadBool(delegate(CheatPanelSettings s) { return s.AutoRestoreSmithingStamina; }, false); }
        }

        public static bool StabiliseOwnedSettlements
        {
            get { return ReadBool(delegate(CheatPanelSettings s) { return s.StabiliseOwnedSettlements; }, false); }
        }

        public static int OwnedSettlementLoyaltyFloor
        {
            get { return Clamp(ReadInt(delegate(CheatPanelSettings s) { return s.OwnedSettlementLoyaltyFloor; }, 75), 0, 100); }
        }

        public static int OwnedSettlementSecurityFloor
        {
            get { return Clamp(ReadInt(delegate(CheatPanelSettings s) { return s.OwnedSettlementSecurityFloor; }, 75), 0, 100); }
        }

        public static int OwnedSettlementProsperityFloor
        {
            get { return Clamp(ReadInt(delegate(CheatPanelSettings s) { return s.OwnedSettlementProsperityFloor; }, 0), 0, 20000); }
        }

        public static bool FillOwnedSettlementFood
        {
            get { return ReadBool(delegate(CheatPanelSettings s) { return s.FillOwnedSettlementFood; }, false); }
        }

        public static int DailyOwnedGarrisonReinforcement
        {
            get { return Clamp(ReadInt(delegate(CheatPanelSettings s) { return s.DailyOwnedGarrisonReinforcement; }, 0), 0, 200); }
        }

        private static bool ReadBool(Func<CheatPanelSettings, bool> reader, bool fallback)
        {
            CheatPanelSettings settings = GetSettings();
            return settings == null ? fallback : reader(settings);
        }

        private static int ReadInt(Func<CheatPanelSettings, int> reader, int fallback)
        {
            CheatPanelSettings settings = GetSettings();
            return settings == null ? fallback : reader(settings);
        }

        private static CheatPanelSettings GetSettings()
        {
            try
            {
                return CheatPanelSettings.Instance;
            }
            catch
            {
                return null;
            }
        }

        private static int Clamp(int value, int min, int max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }
    }
}
