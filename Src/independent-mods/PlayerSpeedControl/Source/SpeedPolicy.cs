using TaleWorlds.CampaignSystem;
using TaleWorlds.Localization;

namespace PlayerSpeedControl
{
    internal static class SpeedPolicy
    {
        private static readonly TextObject TravelLabel = new TextObject("{=psc_travel}Player Speed Control");

        internal static float Clamp(float multiplier)
        {
            if (float.IsNaN(multiplier) || float.IsInfinity(multiplier)) return 1f;
            return multiplier < 0.5f ? 0.5f : multiplier > 3f ? 3f : multiplier;
        }

        internal static bool CanRun(bool isPlayer, bool active, bool human, bool mounted, bool walking, bool onLand)
        {
            return isPlayer && active && human && !mounted && !walking && onLand;
        }

        internal static bool CanTravel(bool isMainParty, bool atSea, bool attached)
        {
            return isMainParty && !atSea && !attached;
        }

        internal static ExplainedNumber ApplyTravel(ExplainedNumber normal, float multiplier)
        {
            multiplier = Clamp(multiplier);
            if (multiplier == 1f) return normal;
            // Flatten the finished native calculation, including its limits, before adding
            // our delta. AddFactor alone would combine additively with terrain/perk factors.
            var result = new ExplainedNumber(0f, normal.IncludeDescriptions);
            result.AddFromExplainedNumber(normal, null);
            result.Add(normal.ResultNumber * (multiplier - 1f), TravelLabel);
            return result;
        }
    }

    // Tracks only our contribution. A different mod's intervening value is preserved.
    internal sealed class RunningAdjustment
    {
        private float baseline;
        private float applied;
        private bool hasAdjustment;

        internal float Restore(float current)
        {
            float result = hasAdjustment && current == applied ? baseline : current;
            hasAdjustment = false;
            return result;
        }

        internal float Apply(float current, float multiplier)
        {
            baseline = Restore(current);
            multiplier = SpeedPolicy.Clamp(multiplier);
            applied = baseline * multiplier;
            hasAdjustment = multiplier != 1f;
            return applied;
        }
    }
}
