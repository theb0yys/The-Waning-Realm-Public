namespace LOTRAOM.FactionBuff.Data
{
    public enum FactionDoctrineMode
    {
        Story = 0,
        Balanced = 1,
        Strict = 2
    }

    public static class FactionBuffSettings
    {
        public const int DefaultDoctrineReportIntervalDays = 3;

        public static bool EffectsEnabled { get; set; } = true;
        public static bool DoctrineReportsEnabled { get; set; } = true;
        public static bool DoctrineControlMenusEnabled { get; set; } = true;
        public static int DoctrineReportIntervalDays { get; set; } = DefaultDoctrineReportIntervalDays;
        public static FactionDoctrineMode DoctrineMode { get; set; } = FactionDoctrineMode.Balanced;

        public static float MoraleMultiplier
        {
            get
            {
                if (!EffectsEnabled) return 0f;

                switch (DoctrineMode)
                {
                    case FactionDoctrineMode.Story:
                        return 0.75f;
                    case FactionDoctrineMode.Strict:
                        return 1.25f;
                    default:
                        return 1f;
                }
            }
        }

        public static string ModeLabel
        {
            get
            {
                switch (DoctrineMode)
                {
                    case FactionDoctrineMode.Story:
                        return "Story";
                    case FactionDoctrineMode.Strict:
                        return "Strict";
                    default:
                        return "Balanced";
                }
            }
        }

        public static string EffectsLabel => EffectsEnabled ? "active" : "suspended";

        public static string ReportsLabel => DoctrineReportsEnabled ? "active" : "quiet";

        public static void CycleDoctrineMode()
        {
            int next = ((int)DoctrineMode + 1) % 3;
            DoctrineMode = (FactionDoctrineMode)next;
        }

        public static void ToggleEffects()
        {
            EffectsEnabled = !EffectsEnabled;
        }

        public static void ToggleReports()
        {
            DoctrineReportsEnabled = !DoctrineReportsEnabled;
        }

        public static void ApplyPersisted(int doctrineMode, bool effectsEnabled, bool reportsEnabled)
        {
            if (doctrineMode < (int)FactionDoctrineMode.Story || doctrineMode > (int)FactionDoctrineMode.Strict)
                doctrineMode = (int)FactionDoctrineMode.Balanced;

            DoctrineMode = (FactionDoctrineMode)doctrineMode;
            EffectsEnabled = effectsEnabled;
            DoctrineReportsEnabled = reportsEnabled;
            if (DoctrineReportIntervalDays < 1)
                DoctrineReportIntervalDays = DefaultDoctrineReportIntervalDays;
        }
    }
}
