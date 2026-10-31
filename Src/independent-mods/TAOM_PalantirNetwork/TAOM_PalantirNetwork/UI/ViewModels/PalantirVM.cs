using System;
using TaleWorlds.Library;

namespace TAOM_PalantirNetwork.UI.ViewModels
{
    public sealed class PalantirVM : ViewModel
    {
        private readonly PalantirCampaignBehavior _behavior;
        private readonly Action _onClose;

        private string _titleText = string.Empty;
        private string _signalText = string.Empty;
        private string _filterText = string.Empty;
        private string _memoryText = string.Empty;
        private string _hostilesButtonText = string.Empty;
        private string _categoryButtonText = string.Empty;
        private string _sortButtonText = string.Empty;
        private string _historyButtonText = string.Empty;
        private string _summaryLineOne = string.Empty;
        private string _summaryLineTwo = string.Empty;
        private string _summaryLineThree = string.Empty;
        private string _priorityText = string.Empty;
        private string _criticalBandText = string.Empty;
        private string _watchBandText = string.Empty;
        private string _hostileBandText = string.Empty;
        private string _infoBandText = string.Empty;
        private string _visibleText = string.Empty;
        private string _emptyText = string.Empty;
        private string _footerText = string.Empty;
        private bool _showEmptyText;
        private bool _showFooterText;

        public MBBindingList<PalantirContactItemVM> Contacts { get; } =
            new MBBindingList<PalantirContactItemVM>();

        public PalantirVM(PalantirCampaignBehavior behavior, Action onClose)
        {
            _behavior = behavior ?? throw new ArgumentNullException(nameof(behavior));
            _onClose = onClose;
            Refresh();
        }

        public void Refresh()
        {
            PalantirBoardSnapshot snapshot = _behavior.BuildBoardSnapshot();

            TitleText = snapshot.TitleText;
            SignalText = snapshot.SignalText;
            FilterText = snapshot.FilterText;
            MemoryText = snapshot.MemoryText;
            HostilesButtonText = snapshot.HostilesButtonText;
            CategoryButtonText = snapshot.CategoryButtonText;
            SortButtonText = snapshot.SortButtonText;
            HistoryButtonText = snapshot.HistoryButtonText;
            SummaryLineOne = snapshot.SummaryLineOne;
            SummaryLineTwo = snapshot.SummaryLineTwo;
            SummaryLineThree = snapshot.SummaryLineThree;
            PriorityText = snapshot.PriorityText;
            CriticalBandText = snapshot.CriticalBandText;
            WatchBandText = snapshot.WatchBandText;
            HostileBandText = snapshot.HostileBandText;
            InfoBandText = snapshot.InfoBandText;
            VisibleText = snapshot.VisibleText;
            EmptyText = snapshot.EmptyText;
            FooterText = snapshot.FooterText;
            ShowEmptyText = !string.IsNullOrEmpty(snapshot.EmptyText);
            ShowFooterText = !string.IsNullOrEmpty(snapshot.FooterText);

            Contacts.Clear();
            for (int i = 0; i < snapshot.Contacts.Count; i++)
                Contacts.Add(new PalantirContactItemVM(snapshot.Contacts[i]));
        }

        [DataSourceProperty]
        public string TitleText
        {
            get => _titleText;
            set
            {
                if (value != _titleText)
                {
                    _titleText = value ?? string.Empty;
                    OnPropertyChangedWithValue(_titleText);
                }
            }
        }

        [DataSourceProperty]
        public string SignalText
        {
            get => _signalText;
            set
            {
                if (value != _signalText)
                {
                    _signalText = value ?? string.Empty;
                    OnPropertyChangedWithValue(_signalText);
                }
            }
        }

        [DataSourceProperty]
        public string FilterText
        {
            get => _filterText;
            set
            {
                if (value != _filterText)
                {
                    _filterText = value ?? string.Empty;
                    OnPropertyChangedWithValue(_filterText);
                }
            }
        }

        [DataSourceProperty]
        public string MemoryText
        {
            get => _memoryText;
            set
            {
                if (value != _memoryText)
                {
                    _memoryText = value ?? string.Empty;
                    OnPropertyChangedWithValue(_memoryText);
                }
            }
        }

        [DataSourceProperty]
        public string HostilesButtonText
        {
            get => _hostilesButtonText;
            set
            {
                if (value != _hostilesButtonText)
                {
                    _hostilesButtonText = value ?? string.Empty;
                    OnPropertyChangedWithValue(_hostilesButtonText);
                }
            }
        }

        [DataSourceProperty]
        public string CategoryButtonText
        {
            get => _categoryButtonText;
            set
            {
                if (value != _categoryButtonText)
                {
                    _categoryButtonText = value ?? string.Empty;
                    OnPropertyChangedWithValue(_categoryButtonText);
                }
            }
        }

        [DataSourceProperty]
        public string SortButtonText
        {
            get => _sortButtonText;
            set
            {
                if (value != _sortButtonText)
                {
                    _sortButtonText = value ?? string.Empty;
                    OnPropertyChangedWithValue(_sortButtonText);
                }
            }
        }

        [DataSourceProperty]
        public string HistoryButtonText
        {
            get => _historyButtonText;
            set
            {
                if (value != _historyButtonText)
                {
                    _historyButtonText = value ?? string.Empty;
                    OnPropertyChangedWithValue(_historyButtonText);
                }
            }
        }

        [DataSourceProperty]
        public string SummaryLineOne
        {
            get => _summaryLineOne;
            set
            {
                if (value != _summaryLineOne)
                {
                    _summaryLineOne = value ?? string.Empty;
                    OnPropertyChangedWithValue(_summaryLineOne);
                }
            }
        }

        [DataSourceProperty]
        public string SummaryLineTwo
        {
            get => _summaryLineTwo;
            set
            {
                if (value != _summaryLineTwo)
                {
                    _summaryLineTwo = value ?? string.Empty;
                    OnPropertyChangedWithValue(_summaryLineTwo);
                }
            }
        }

        [DataSourceProperty]
        public string SummaryLineThree
        {
            get => _summaryLineThree;
            set
            {
                if (value != _summaryLineThree)
                {
                    _summaryLineThree = value ?? string.Empty;
                    OnPropertyChangedWithValue(_summaryLineThree);
                }
            }
        }

        [DataSourceProperty]
        public string PriorityText
        {
            get => _priorityText;
            set
            {
                if (value != _priorityText)
                {
                    _priorityText = value ?? string.Empty;
                    OnPropertyChangedWithValue(_priorityText);
                }
            }
        }

        [DataSourceProperty]
        public string CriticalBandText
        {
            get => _criticalBandText;
            set
            {
                if (value != _criticalBandText)
                {
                    _criticalBandText = value ?? string.Empty;
                    OnPropertyChangedWithValue(_criticalBandText);
                }
            }
        }

        [DataSourceProperty]
        public string WatchBandText
        {
            get => _watchBandText;
            set
            {
                if (value != _watchBandText)
                {
                    _watchBandText = value ?? string.Empty;
                    OnPropertyChangedWithValue(_watchBandText);
                }
            }
        }

        [DataSourceProperty]
        public string HostileBandText
        {
            get => _hostileBandText;
            set
            {
                if (value != _hostileBandText)
                {
                    _hostileBandText = value ?? string.Empty;
                    OnPropertyChangedWithValue(_hostileBandText);
                }
            }
        }

        [DataSourceProperty]
        public string InfoBandText
        {
            get => _infoBandText;
            set
            {
                if (value != _infoBandText)
                {
                    _infoBandText = value ?? string.Empty;
                    OnPropertyChangedWithValue(_infoBandText);
                }
            }
        }

        [DataSourceProperty]
        public string VisibleText
        {
            get => _visibleText;
            set
            {
                if (value != _visibleText)
                {
                    _visibleText = value ?? string.Empty;
                    OnPropertyChangedWithValue(_visibleText);
                }
            }
        }

        [DataSourceProperty]
        public string EmptyText
        {
            get => _emptyText;
            set
            {
                if (value != _emptyText)
                {
                    _emptyText = value ?? string.Empty;
                    OnPropertyChangedWithValue(_emptyText);
                }
            }
        }

        [DataSourceProperty]
        public string FooterText
        {
            get => _footerText;
            set
            {
                if (value != _footerText)
                {
                    _footerText = value ?? string.Empty;
                    OnPropertyChangedWithValue(_footerText);
                }
            }
        }

        [DataSourceProperty]
        public bool ShowEmptyText
        {
            get => _showEmptyText;
            set
            {
                if (value != _showEmptyText)
                {
                    _showEmptyText = value;
                    OnPropertyChangedWithValue(value);
                }
            }
        }

        [DataSourceProperty]
        public bool ShowFooterText
        {
            get => _showFooterText;
            set
            {
                if (value != _showFooterText)
                {
                    _showFooterText = value;
                    OnPropertyChangedWithValue(value);
                }
            }
        }

        public void ExecuteToggleHostiles()
        {
            _behavior.ToggleHostileFilter();
            Refresh();
        }

        public void ExecuteCycleCategory()
        {
            _behavior.CycleCategory();
            Refresh();
        }

        public void ExecuteCycleSort()
        {
            _behavior.CycleSortMode();
            Refresh();
        }

        public void ExecuteToggleHistory()
        {
            _behavior.ToggleHistoryDetails();
            Refresh();
        }

        public void ExecuteRefresh()
        {
            _behavior.RefreshIntel();
            Refresh();
        }

        public void ExecuteClose()
        {
            _onClose?.Invoke();
        }
    }
}
