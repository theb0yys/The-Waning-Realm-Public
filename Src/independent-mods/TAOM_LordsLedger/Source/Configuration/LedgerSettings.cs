using System;

namespace TAOM_LordsLedger
{
    public enum SortMode
    {
        ByName       = 0,
        ByLoyalty    = 1,
        ByProsperity = 2,
        ByFood       = 3,
        ByGarrison   = 4,
        ByRisk       = 5,
    }

    public enum FiefFilter
    {
        All      = 0,
        Towns    = 1,
        Castles  = 2,
        Villages = 3,
    }

    public enum ThresholdPreset
    {
        Normal  = 0,   // balanced defaults
        Strict  = 1,   // early-warning, fires sooner
        Relaxed = 2,   // only fires on severe problems
    }

    /// <summary>
    /// All user-configurable Ledger state.
    /// Primitive fields are written and read via SyncData in LedgerCampaignBehavior.
    /// Threshold values are looked up from a table — no allocations.
    /// </summary>
    public sealed class LedgerSettings
    {
        // ── UI state — persisted ──────────────────────────────────────────
        public SortMode        Sort           = SortMode.ByName;
        public FiefFilter      Filter         = FiefFilter.All;
        public ThresholdPreset Preset         = ThresholdPreset.Normal;
        public bool            WarningsOnly   = false;
        public bool            CompactView    = false;
        public bool            IncludeVassals = false;
        public bool            ShowGovernancePulse = true;
        public int             GovernancePulseIntervalDays = 7;

        // ── Threshold table ───────────────────────────────────────────────
        // Index matches ThresholdPreset enum value.
        // Columns: Loyalty, Security, Food, Garrison, DangerRadius (map units)
        private static readonly (float Loyalty, float Security, float Food, int Garrison, float DangerRadius)[] _presets =
        {
            (30f, 20f,   0f, 40, 20f),   // Normal
            (45f, 35f,   5f, 60, 30f),   // Strict
            (15f, 10f, -10f, 20, 15f),   // Relaxed
        };

        // Clamped index — guards against an out-of-range enum value produced by
        // a corrupted or manually edited save file, which would otherwise cause
        // _presets[(int)Preset] to throw IndexOutOfRangeException.
        private int PresetIndex => Math.Max(0, Math.Min(_presets.Length - 1, (int)Preset));

        public float LowLoyalty   => _presets[PresetIndex].Loyalty;
        public float LowSecurity  => _presets[PresetIndex].Security;
        public float LowFood      => _presets[PresetIndex].Food;
        public int   LowGarrison  => _presets[PresetIndex].Garrison;
        public float DangerRadius => _presets[PresetIndex].DangerRadius;

        // ── Cycle helpers ─────────────────────────────────────────────────
        public void CycleSort()   => Sort   = (SortMode)(   ((int)Sort   + 1) % 6);
        public void CycleFilter() => Filter = (FiefFilter)( ((int)Filter + 1) % 4);
        public void CyclePreset() => Preset = (ThresholdPreset)(((int)Preset + 1) % 3);

        // ── Display labels ────────────────────────────────────────────────
        public string SortLabel()
        {
            switch (Sort)
            {
                case SortMode.ByName:       return "Name";
                case SortMode.ByLoyalty:    return "Loyalty (lowest first)";
                case SortMode.ByProsperity: return "Prosperity (highest first)";
                case SortMode.ByFood:       return "Food (lowest first)";
                case SortMode.ByGarrison:   return "Garrison (lowest first)";
                case SortMode.ByRisk:       return "Risk (highest first)";
                default:                    return Sort.ToString();
            }
        }

        public string FilterLabel()
        {
            switch (Filter)
            {
                case FiefFilter.All:      return IncludeVassals ? "All  +Vassals" : "All";
                case FiefFilter.Towns:    return "Towns";
                case FiefFilter.Castles:  return "Castles";
                case FiefFilter.Villages: return "Villages";
                default:                  return Filter.ToString();
            }
        }

        public string PresetLabel()
        {
            switch (Preset)
            {
                case ThresholdPreset.Normal:  return "Normal";
                case ThresholdPreset.Strict:  return "Strict";
                case ThresholdPreset.Relaxed: return "Relaxed";
                default:                      return Preset.ToString();
            }
        }

        public string ViewLabel()     => CompactView    ? "Compact"  : "Detailed";
        public string ScopeLabel()    => IncludeVassals ? "Kingdom"  : "My Clan";
        public string WarningsLabel() => WarningsOnly   ? "ON"       : "OFF";
    }
}
