using System;

namespace TroopLimitExpander
{
    public static class TroopLimitSettings
    {
        private const int DefaultCustomBattleTroopsPerSide = 25000;
        private const int DefaultFieldBattleTroopCap = 1600;
        private const int DefaultSiegeBattleTroopCap = 1200;
        private const int DefaultLandActiveAgentSafetyCap = 1800;
        private const int DefaultInitialDeploymentMaxSidePercent = 50;
        private const int DefaultLandReinforcementWaves = 50;
        private const int DefaultLandWaveBatchPercent = 4;
        private const int DefaultLandReinforcementIntervalSeconds = 6;
        private const bool DefaultCustomBattleCrashGuard = true;
        private const bool DefaultExpandedActiveBattleSize = false;
        private const bool DefaultBattleOverlayEnabled = true;

        private static TroopLimitMcmSettings? Mcm
        {
            get { return TroopLimitMcmSettings.Instance; }
        }

        public static int EffectiveCustomBattleTroopsPerSideCap
        {
            get { return Clamp(Mcm?.CustomBattleTroopsPerSideCap ?? DefaultCustomBattleTroopsPerSide, 200, 25000); }
        }

        public static int EffectiveFieldBattleTroopCap
        {
            get
            {
                return Math.Min(
                    Clamp(Mcm?.FieldBattleTroopCap ?? DefaultFieldBattleTroopCap, 200, 25000),
                    EffectiveLandActiveAgentSafetyCap);
            }
        }

        public static int EffectiveSiegeBattleTroopCap
        {
            get
            {
                return Math.Min(
                    Clamp(Mcm?.SiegeBattleTroopCap ?? DefaultSiegeBattleTroopCap, 200, 16000),
                    EffectiveLandActiveAgentSafetyCap);
            }
        }

        public static int EffectiveLandActiveAgentSafetyCap
        {
            get
            {
                int requested = Clamp(Mcm?.ActiveAgentSafetyCap ?? DefaultLandActiveAgentSafetyCap, 400, 8000);
                return IsCustomBattleCrashGuardEnabled ? Math.Min(requested, 2400) : requested;
            }
        }

        public static float EffectiveInitialDeploymentMaxSideRatio
        {
            get
            {
                int max = IsCustomBattleCrashGuardEnabled ? 60 : 75;
                return Clamp(Mcm?.InitialDeploymentMaxSidePercent ?? DefaultInitialDeploymentMaxSidePercent, 45, max) / 100f;
            }
        }

        public static bool IsCustomBattleCrashGuardEnabled
        {
            get { return Mcm?.CustomBattleCrashGuard ?? DefaultCustomBattleCrashGuard; }
        }

        public static bool IsExpandedActiveBattleSizeEnabled
        {
            get { return Mcm?.ExpandActiveBattleSize ?? DefaultExpandedActiveBattleSize; }
        }

        public static bool IsBattleOverlayEnabled
        {
            get { return Mcm?.ShowBattleOverlay ?? DefaultBattleOverlayEnabled; }
        }

        public static int EffectiveMissionBattleSizeHardCap
        {
            get { return Math.Max(EffectiveFieldBattleTroopCap, EffectiveSiegeBattleTroopCap); }
        }

        public static int EffectiveLandReinforcementWaves
        {
            get { return Clamp(Mcm?.LandReinforcementWaves ?? DefaultLandReinforcementWaves, 0, 96); }
        }

        public static int EffectiveLandWaveBatchPercent
        {
            get { return Clamp(Mcm?.LandWaveBatchPercent ?? DefaultLandWaveBatchPercent, 1, 50); }
        }

        public static float EffectiveLandWaveBatchRatio
        {
            get { return EffectiveLandWaveBatchPercent / 100f; }
        }

        public static int EffectiveLandReinforcementIntervalSeconds
        {
            get { return Clamp(Mcm?.LandReinforcementIntervalSeconds ?? DefaultLandReinforcementIntervalSeconds, 1, 30); }
        }

        public static bool ShowRuntimeDiagnostics
        {
            get { return Mcm?.ShowRuntimeDiagnostics ?? true; }
        }

        public static string BuildRuntimeSummary()
        {
            return "Custom/side: " + EffectiveCustomBattleTroopsPerSideCap +
                   " | Field active: " + EffectiveFieldBattleTroopCap +
                   " | Siege active: " + EffectiveSiegeBattleTroopCap +
                   " | Active expansion: " + (IsExpandedActiveBattleSizeEnabled ? "on" : "off") +
                   " | Safety: " + EffectiveLandActiveAgentSafetyCap +
                   " | Crash guard: " + (IsCustomBattleCrashGuardEnabled ? "on" : "off") +
                   " | Waves: " + EffectiveLandReinforcementWaves +
                   "@" + EffectiveLandWaveBatchPercent + "%/" + EffectiveLandReinforcementIntervalSeconds + "s" +
                   " | Deploy side: " + (int)(EffectiveInitialDeploymentMaxSideRatio * 100f) + "%" +
                   " | Overlay: " + (IsBattleOverlayEnabled ? "on" : "off");
        }

        public static string BuildSafetyNote()
        {
            if (EffectiveLandActiveAgentSafetyCap >= 6000)
                return "Expert active-agent cap selected; lower the safety cap first if deployment or reinforcement spawn crashes.";

            if (EffectiveLandActiveAgentSafetyCap >= 3000)
                return "High active-agent profile selected; test field battles before pushing sieges.";

            return IsCustomBattleCrashGuardEnabled
                ? "Crash guard enabled; five-digit custom battle rosters are staged through conservative active waves for the first-battle gate."
                : "Crash guard disabled; raise active caps slowly and test before saving the profile.";
        }

        public static void ApplyToSpawnSettings(ref TaleWorlds.MountAndBlade.MissionSpawnSettings settings)
        {
            if (settings.MaximumBattleSideRatio > EffectiveInitialDeploymentMaxSideRatio)
                settings.MaximumBattleSideRatio = EffectiveInitialDeploymentMaxSideRatio;

            settings.MaximumReinforcementWaveCount = EffectiveLandReinforcementWaves;
            settings.ReinforcementBatchPercentage = EffectiveLandWaveBatchRatio;
            settings.AttackerReinforcementBatchPercentage = EffectiveLandWaveBatchRatio;
            settings.DefenderReinforcementBatchPercentage = EffectiveLandWaveBatchRatio;
            settings.DesiredReinforcementPercentage = Clamp(
                settings.DesiredReinforcementPercentage,
                EffectiveLandWaveBatchRatio,
                Math.Max(EffectiveLandWaveBatchRatio, 0.12f));
            settings.GlobalReinforcementInterval = EffectiveLandReinforcementIntervalSeconds;
        }

        private static int Clamp(int value, int min, int max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        private static float Clamp(float value, float min, float max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }
    }
}
