using System.Collections.Generic;

namespace TAOM_LordsLedger
{
    /// <summary>
    /// Snapshot of a single fief at the time of a Ledger refresh.
    /// Populated by LedgerDataReader; consumed by LedgerRenderer.
    /// </summary>
    public struct FiefInfo
    {
        // ── Identity ──────────────────────────────────────────────────────
        public string Name;
        public string TypeLabel;        // e.g. "Gondorian Town", "Dwarven Stronghold"
        public bool   IsVillage;
        public bool   IsVassal;         // owned by a vassal clan, not the player

        // ── Core stats (towns and castles; 0f default for villages) ──────
        public float Loyalty;
        public float LoyaltyDelta;      // change since last recorded; float.NaN = no history
        public float Security;
        public float Prosperity;
        public float FoodChange;

        // ── Military ──────────────────────────────────────────────────────
        public int GarrisonCount;
        public int MilitiaCount;

        // ── Status ────────────────────────────────────────────────────────
        public bool IsUnderSiege;
        public bool IsRaidedOrLooted;
        public bool IsDanger;           // thin garrison AND hostile army within danger radius
        public bool HasWarning;
        public int  RiskScore;          // 0–100 composite urgency score
        public int    GovernanceScore;  // 0-100 readiness score, higher is healthier
        public string PrimaryIssue;     // top current problem, "Stable" if none
        public string ActionHint;       // direct next step for the player
        public int    ReinforcementNeed;// troops needed to reach current threshold
        public string TrendLabel;       // loyalty trend label for scanning

        // ── Governance ────────────────────────────────────────────────────
        public string GovernorName;
        public int    WallLevel;
        public int    BoundVillageCount;

        // ── Construction & prisoners ──────────────────────────────────────
        public string       BuildingInProgress; // null = nothing being built
        public List<string> NotablePrisoners;   // null or empty = none
    }
}
