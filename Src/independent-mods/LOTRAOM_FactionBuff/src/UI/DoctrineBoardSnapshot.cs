using System.Collections.Generic;

namespace LOTRAOM.FactionBuff.UI
{
    public sealed class DoctrineBoardSnapshot
    {
        public string TitleText { get; set; } = string.Empty;
        public string SignalText { get; set; } = string.Empty;
        public string ContextText { get; set; } = string.Empty;
        public string CadenceText { get; set; } = string.Empty;
        public string ModeButtonText { get; set; } = string.Empty;
        public string EffectsButtonText { get; set; } = string.Empty;
        public string ReportsButtonText { get; set; } = string.Empty;
        public string ScoreText { get; set; } = string.Empty;
        public string ReadinessText { get; set; } = string.Empty;
        public string MoraleText { get; set; } = string.Empty;
        public string SummaryLineOne { get; set; } = string.Empty;
        public string SummaryLineTwo { get; set; } = string.Empty;
        public string SummaryLineThree { get; set; } = string.Empty;
        public string DirectiveText { get; set; } = string.Empty;
        public string EmptyText { get; set; } = string.Empty;
        public bool ShowEmptyText { get; set; }
        public bool IsPeak { get; set; }
        public bool IsDire { get; set; }
        public bool IsReady { get; set; }

        public List<DoctrineSignalSnapshot> Signals { get; } =
            new List<DoctrineSignalSnapshot>();
    }

    public sealed class DoctrineSignalSnapshot
    {
        public string Name { get; set; } = string.Empty;
        public string ValueText { get; set; } = string.Empty;
        public string DetailText { get; set; } = string.Empty;
        public string StatusText { get; set; } = string.Empty;
        public bool IsGood { get; set; }
        public bool IsWatch { get; set; }
        public bool IsBad { get; set; }
    }
}
