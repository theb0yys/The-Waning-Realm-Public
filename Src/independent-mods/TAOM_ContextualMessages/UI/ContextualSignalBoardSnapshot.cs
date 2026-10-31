using System.Collections.Generic;

namespace ContextualMessages.UI
{
    public sealed class ContextualSignalBoardSnapshot
    {
        public ContextualSignalBoardSnapshot()
        {
            Signals = new List<ContextualSignalSnapshot>();
        }

        public string TitleText { get; set; }
        public string HeaderText { get; set; }
        public string CadenceText { get; set; }
        public string StanceButtonText { get; set; }
        public string FocusButtonText { get; set; }
        public string MoodText { get; set; }
        public string TensionText { get; set; }
        public string MomentumText { get; set; }
        public string RealmScoreText { get; set; }
        public string SupplyScoreText { get; set; }
        public string AdviceText { get; set; }
        public string RealmText { get; set; }
        public string PartyText { get; set; }
        public string CategoryText { get; set; }
        public string BridgeText { get; set; }
        public string EmptyText { get; set; }
        public bool ShowEmptyText { get; set; }
        public bool IsAlert { get; set; }
        public bool IsHopeful { get; set; }
        public bool IsQuiet { get; set; }
        public List<ContextualSignalSnapshot> Signals { get; private set; }
    }

    public sealed class ContextualSignalSnapshot
    {
        public string Name { get; set; }
        public string ValueText { get; set; }
        public string DetailText { get; set; }
        public string StatusText { get; set; }
        public bool IsGood { get; set; }
        public bool IsWatch { get; set; }
        public bool IsBad { get; set; }
    }
}
