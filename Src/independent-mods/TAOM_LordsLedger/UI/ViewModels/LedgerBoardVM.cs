using System;
using TaleWorlds.Library;

namespace TAOM_LordsLedger.UI.ViewModels
{
    public sealed class LedgerBoardVM : ViewModel
    {
        private readonly LedgerCampaignBehavior _behavior;
        private readonly Action _onClose;

        private string _titleText = string.Empty;
        private string _signalText = string.Empty;
        private string _scopeText = string.Empty;
        private string _memoryText = string.Empty;
        private string _sortButtonText = string.Empty;
        private string _filterButtonText = string.Empty;
        private string _warningsButtonText = string.Empty;
        private string _presetButtonText = string.Empty;
        private string _viewButtonText = string.Empty;
        private string _scopeButtonText = string.Empty;
        private string _summaryLineOne = string.Empty;
        private string _summaryLineTwo = string.Empty;
        private string _summaryLineThree = string.Empty;
        private string _priorityText = string.Empty;
        private string _criticalBandText = string.Empty;
        private string _watchBandText = string.Empty;
        private string _stableBandText = string.Empty;
        private string _reinforceBandText = string.Empty;
        private string _visibleText = string.Empty;
        private string _emptyText = string.Empty;
        private string _footerText = string.Empty;
        private bool _showEmptyText;
        private bool _showFooterText;

        public MBBindingList<LedgerFiefItemVM> Fiefs { get; } =
            new MBBindingList<LedgerFiefItemVM>();

        public LedgerBoardVM(LedgerCampaignBehavior behavior, Action onClose)
        {
            _behavior = behavior ?? throw new ArgumentNullException(nameof(behavior));
            _onClose = onClose;
            Refresh();
        }

        public void Refresh()
        {
            LedgerBoardSnapshot snapshot = _behavior.BuildBoardSnapshot();

            TitleText = snapshot.TitleText;
            SignalText = snapshot.SignalText;
            ScopeText = snapshot.ScopeText;
            MemoryText = snapshot.MemoryText;
            SortButtonText = snapshot.SortButtonText;
            FilterButtonText = snapshot.FilterButtonText;
            WarningsButtonText = snapshot.WarningsButtonText;
            PresetButtonText = snapshot.PresetButtonText;
            ViewButtonText = snapshot.ViewButtonText;
            ScopeButtonText = snapshot.ScopeButtonText;
            SummaryLineOne = snapshot.SummaryLineOne;
            SummaryLineTwo = snapshot.SummaryLineTwo;
            SummaryLineThree = snapshot.SummaryLineThree;
            PriorityText = snapshot.PriorityText;
            CriticalBandText = snapshot.CriticalBandText;
            WatchBandText = snapshot.WatchBandText;
            StableBandText = snapshot.StableBandText;
            ReinforceBandText = snapshot.ReinforceBandText;
            VisibleText = snapshot.VisibleText;
            EmptyText = snapshot.EmptyText;
            FooterText = snapshot.FooterText;
            ShowEmptyText = !string.IsNullOrEmpty(snapshot.EmptyText);
            ShowFooterText = !string.IsNullOrEmpty(snapshot.FooterText);

            Fiefs.Clear();
            for (int i = 0; i < snapshot.Fiefs.Count; i++)
                Fiefs.Add(new LedgerFiefItemVM(snapshot.Fiefs[i]));
        }

        [DataSourceProperty]
        public string TitleText
        {
            get => _titleText;
            set { if (value != _titleText) { _titleText = value ?? string.Empty; OnPropertyChangedWithValue(_titleText); } }
        }

        [DataSourceProperty]
        public string SignalText
        {
            get => _signalText;
            set { if (value != _signalText) { _signalText = value ?? string.Empty; OnPropertyChangedWithValue(_signalText); } }
        }

        [DataSourceProperty]
        public string ScopeText
        {
            get => _scopeText;
            set { if (value != _scopeText) { _scopeText = value ?? string.Empty; OnPropertyChangedWithValue(_scopeText); } }
        }

        [DataSourceProperty]
        public string MemoryText
        {
            get => _memoryText;
            set { if (value != _memoryText) { _memoryText = value ?? string.Empty; OnPropertyChangedWithValue(_memoryText); } }
        }

        [DataSourceProperty]
        public string SortButtonText
        {
            get => _sortButtonText;
            set { if (value != _sortButtonText) { _sortButtonText = value ?? string.Empty; OnPropertyChangedWithValue(_sortButtonText); } }
        }

        [DataSourceProperty]
        public string FilterButtonText
        {
            get => _filterButtonText;
            set { if (value != _filterButtonText) { _filterButtonText = value ?? string.Empty; OnPropertyChangedWithValue(_filterButtonText); } }
        }

        [DataSourceProperty]
        public string WarningsButtonText
        {
            get => _warningsButtonText;
            set { if (value != _warningsButtonText) { _warningsButtonText = value ?? string.Empty; OnPropertyChangedWithValue(_warningsButtonText); } }
        }

        [DataSourceProperty]
        public string PresetButtonText
        {
            get => _presetButtonText;
            set { if (value != _presetButtonText) { _presetButtonText = value ?? string.Empty; OnPropertyChangedWithValue(_presetButtonText); } }
        }

        [DataSourceProperty]
        public string ViewButtonText
        {
            get => _viewButtonText;
            set { if (value != _viewButtonText) { _viewButtonText = value ?? string.Empty; OnPropertyChangedWithValue(_viewButtonText); } }
        }

        [DataSourceProperty]
        public string ScopeButtonText
        {
            get => _scopeButtonText;
            set { if (value != _scopeButtonText) { _scopeButtonText = value ?? string.Empty; OnPropertyChangedWithValue(_scopeButtonText); } }
        }

        [DataSourceProperty]
        public string SummaryLineOne
        {
            get => _summaryLineOne;
            set { if (value != _summaryLineOne) { _summaryLineOne = value ?? string.Empty; OnPropertyChangedWithValue(_summaryLineOne); } }
        }

        [DataSourceProperty]
        public string SummaryLineTwo
        {
            get => _summaryLineTwo;
            set { if (value != _summaryLineTwo) { _summaryLineTwo = value ?? string.Empty; OnPropertyChangedWithValue(_summaryLineTwo); } }
        }

        [DataSourceProperty]
        public string SummaryLineThree
        {
            get => _summaryLineThree;
            set { if (value != _summaryLineThree) { _summaryLineThree = value ?? string.Empty; OnPropertyChangedWithValue(_summaryLineThree); } }
        }

        [DataSourceProperty]
        public string PriorityText
        {
            get => _priorityText;
            set { if (value != _priorityText) { _priorityText = value ?? string.Empty; OnPropertyChangedWithValue(_priorityText); } }
        }

        [DataSourceProperty]
        public string CriticalBandText
        {
            get => _criticalBandText;
            set { if (value != _criticalBandText) { _criticalBandText = value ?? string.Empty; OnPropertyChangedWithValue(_criticalBandText); } }
        }

        [DataSourceProperty]
        public string WatchBandText
        {
            get => _watchBandText;
            set { if (value != _watchBandText) { _watchBandText = value ?? string.Empty; OnPropertyChangedWithValue(_watchBandText); } }
        }

        [DataSourceProperty]
        public string StableBandText
        {
            get => _stableBandText;
            set { if (value != _stableBandText) { _stableBandText = value ?? string.Empty; OnPropertyChangedWithValue(_stableBandText); } }
        }

        [DataSourceProperty]
        public string ReinforceBandText
        {
            get => _reinforceBandText;
            set { if (value != _reinforceBandText) { _reinforceBandText = value ?? string.Empty; OnPropertyChangedWithValue(_reinforceBandText); } }
        }

        [DataSourceProperty]
        public string VisibleText
        {
            get => _visibleText;
            set { if (value != _visibleText) { _visibleText = value ?? string.Empty; OnPropertyChangedWithValue(_visibleText); } }
        }

        [DataSourceProperty]
        public string EmptyText
        {
            get => _emptyText;
            set { if (value != _emptyText) { _emptyText = value ?? string.Empty; OnPropertyChangedWithValue(_emptyText); } }
        }

        [DataSourceProperty]
        public string FooterText
        {
            get => _footerText;
            set { if (value != _footerText) { _footerText = value ?? string.Empty; OnPropertyChangedWithValue(_footerText); } }
        }

        [DataSourceProperty]
        public bool ShowEmptyText
        {
            get => _showEmptyText;
            set { if (value != _showEmptyText) { _showEmptyText = value; OnPropertyChangedWithValue(value); } }
        }

        [DataSourceProperty]
        public bool ShowFooterText
        {
            get => _showFooterText;
            set { if (value != _showFooterText) { _showFooterText = value; OnPropertyChangedWithValue(value); } }
        }

        public void ExecuteCycleSort()
        {
            _behavior.CycleSort();
            Refresh();
        }

        public void ExecuteCycleFilter()
        {
            _behavior.CycleFilter();
            Refresh();
        }

        public void ExecuteToggleWarnings()
        {
            _behavior.ToggleWarningsOnly();
            Refresh();
        }

        public void ExecuteCyclePreset()
        {
            _behavior.CyclePreset();
            Refresh();
        }

        public void ExecuteToggleView()
        {
            _behavior.ToggleCompactView();
            Refresh();
        }

        public void ExecuteToggleScope()
        {
            _behavior.ToggleScope();
            Refresh();
        }

        public void ExecuteExport()
        {
            string message = _behavior.ExportLedger();
            InformationManager.DisplayMessage(new InformationMessage(message, Colors.Gray));
            Refresh();
        }

        public void ExecuteRefresh()
        {
            _behavior.RefreshLedger();
            Refresh();
        }

        public void ExecuteClose()
        {
            _onClose?.Invoke();
        }
    }
}
