using System;
using TAOM_CheatPanel.Settings;

namespace TAOM_CheatPanel.Runtime
{
    internal static class ProgressionRules
    {
        internal static int Clamp(int value, int minimum, int maximum)
        {
            return Math.Max(minimum, Math.Min(maximum, value));
        }

        internal static float ScaleXp(float rawXp, bool isMainHero, bool normalGain, CheatPanelSettings settings)
        {
            if (!isMainHero || !normalGain || settings == null || !settings.Enabled ||
                rawXp <= 0 || float.IsNaN(rawXp) || float.IsInfinity(rawXp)) return rawXp;
            int multiplier = Clamp(settings.PlayerXpMultiplier, 1, 10);
            return rawXp <= float.MaxValue / multiplier ? rawXp * multiplier : rawXp;
        }

        internal static int AddBonusPoints(int current, int bonus)
        {
            return (int)Math.Min(int.MaxValue, (long)current + Clamp(bonus, 0, 10));
        }
    }
}
