using System;

namespace TAOM.EquipmentTestShop
{
    internal static class TestStock
    {
        internal const string SettlementId = "town_EW1";
        internal const int TargetCount = 3;

        // Called only on campaign launch or the player's town entry, never a tick.
        // A negative count means the registered item is unavailable: skip it safely.
        internal static int Apply(string settlementId, bool isTown,
            Func<string, int> count, Action<string, int> add)
        {
            if (!isTown || !string.Equals(settlementId, SettlementId, StringComparison.Ordinal))
                return 0;
            int added = 0;
            foreach (string id in EquipmentIds.All)
            {
                int present = count(id);
                if (present < 0 || present >= TargetCount) continue;
                int missing = TargetCount - present;
                add(id, missing);
                added += missing;
            }
            return added;
        }
    }
}
