using System.Collections.Generic;

namespace TAOM_PalantirNetwork
{
    public sealed class PalantirBoardSnapshot
    {
        public string TitleText { get; set; } = string.Empty;
        public string SignalText { get; set; } = string.Empty;
        public string FilterText { get; set; } = string.Empty;
        public string MemoryText { get; set; } = string.Empty;
        public string HostilesButtonText { get; set; } = string.Empty;
        public string CategoryButtonText { get; set; } = string.Empty;
        public string SortButtonText { get; set; } = string.Empty;
        public string HistoryButtonText { get; set; } = string.Empty;
        public string SummaryLineOne { get; set; } = string.Empty;
        public string SummaryLineTwo { get; set; } = string.Empty;
        public string SummaryLineThree { get; set; } = string.Empty;
        public string PriorityText { get; set; } = string.Empty;
        public string CriticalBandText { get; set; } = string.Empty;
        public string WatchBandText { get; set; } = string.Empty;
        public string HostileBandText { get; set; } = string.Empty;
        public string InfoBandText { get; set; } = string.Empty;
        public string VisibleText { get; set; } = string.Empty;
        public string EmptyText { get; set; } = string.Empty;
        public string FooterText { get; set; } = string.Empty;

        public List<PalantirContactSnapshot> Contacts { get; } =
            new List<PalantirContactSnapshot>();
    }

    public sealed class PalantirContactSnapshot
    {
        public string Name { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
        public string Kind { get; set; } = string.Empty;
        public string FactionLine { get; set; } = string.Empty;
        public string PositionLine { get; set; } = string.Empty;
        public string CounselLine { get; set; } = string.Empty;
        public string DangerText { get; set; } = string.Empty;
        public string LocationLine { get; set; } = string.Empty;
        public string HistoryLine { get; set; } = string.Empty;
        public bool ShowLocation { get; set; }
        public bool ShowHistory { get; set; }
        public bool IsCritical { get; set; }
        public bool IsWatch { get; set; }
        public bool IsHostile { get; set; }
        public bool IsInfo { get; set; }
    }
}
