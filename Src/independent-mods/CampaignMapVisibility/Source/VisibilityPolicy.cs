namespace CampaignMapVisibility
{
    internal static class VisibilityPolicy
    {
        internal static bool IsOnScreen(float depth, float x, float y, float width, float height)
        {
            // Keep projection/viewport culling, remove camera-height and far-depth cutoffs.
            return depth > 0f && !float.IsInfinity(depth) && width > 0f && height > 0f
                && x >= -100f && y >= -30f && x <= width + 100f && y <= height + 30f;
        }

        internal static bool ShowParty(bool active, bool stationary, bool inSettlement, bool spotted, bool reveal)
        {
            return active && !stationary && !inSettlement && (spotted || reveal);
        }

        internal static bool ShowBattle(bool onScreen, bool nativeVisible, bool reveal, bool finalized)
        {
            return onScreen && !finalized && (nativeVisible || reveal);
        }
    }
}
