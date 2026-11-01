using System.Collections.Generic;

namespace TAOM_LordsLedger
{
    public sealed class LedgerBoardSnapshot
    {
        public string TitleText { get; set; } = string.Empty;
        public string SignalText { get; set; } = string.Empty;
        public string ScopeText { get; set; } = string.Empty;
        public string MemoryText { get; set; } = string.Empty;
        public string SortButtonText { get; set; } = string.Empty;
        public string FilterButtonText { get; set; } = string.Empty;
        public string WarningsButtonText { get; set; } = string.Empty;
        public string PresetButtonText { get; set; } = string.Empty;
        public string ViewButtonText { get; set; } = string.Empty;
        public string ScopeButtonText { get; set; } = string.Empty;
        public string SummaryLineOne { get; set; } = string.Empty;
        public string SummaryLineTwo { get; set; } = string.Empty;
        public string SummaryLineThree { get; set; } = string.Empty;
        public string PriorityText { get; set; } = string.Empty;
        public string CriticalBandText { get; set; } = string.Empty;
        public string WatchBandText { get; set; } = string.Empty;
        public string StableBandText { get; set; } = string.Empty;
        public string ReinforceBandText { get; set; } = string.Empty;
        public string VisibleText { get; set; } = string.Empty;
        public string EmptyText { get; set; } = string.Empty;
        public string FooterText { get; set; } = string.Empty;

        public List<LedgerFiefSnapshot> Fiefs { get; } =
            new List<LedgerFiefSnapshot>();
    }

    public sealed class LedgerFiefSnapshot
    {
        public string Name { get; set; } = string.Empty;
        public string TypeText { get; set; } = string.Empty;
        public string StatusText { get; set; } = string.Empty;
        public string RiskText { get; set; } = string.Empty;
        public string ReadinessText { get; set; } = string.Empty;
        public string MetricsLine { get; set; } = string.Empty;
        public string MilitaryLine { get; set; } = string.Empty;
        public string ActionLine { get; set; } = string.Empty;
        public string GovernorLine { get; set; } = string.Empty;
        public string ConstructionLine { get; set; } = string.Empty;
        public string PrisonersLine { get; set; } = string.Empty;
        public bool ShowGovernor { get; set; }
        public bool ShowConstruction { get; set; }
        public bool ShowPrisoners { get; set; }
        public bool IsCritical { get; set; }
        public bool IsWarning { get; set; }
        public bool IsStable { get; set; }
        public bool IsVillage { get; set; }
    }
}
