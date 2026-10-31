using System;
using LOTRAOM.FactionBuff.Behaviors;
using TaleWorlds.Library;

namespace LOTRAOM.FactionBuff.UI.ViewModels
{
    public sealed class DoctrineBoardVM : ViewModel
    {
        private readonly FactionBuffMessageBehavior _behavior;
        private readonly Action _onClose;

        private string _titleText = string.Empty;
        private string _signalText = string.Empty;
        private string _contextText = string.Empty;
        private string _cadenceText = string.Empty;
        private string _modeButtonText = string.Empty;
        private string _effectsButtonText = string.Empty;
        private string _reportsButtonText = string.Empty;
        private string _scoreText = string.Empty;
        private string _readinessText = string.Empty;
        private string _moraleText = string.Empty;
        private string _summaryLineOne = string.Empty;
        private string _summaryLineTwo = string.Empty;
        private string _summaryLineThree = string.Empty;
        private string _directiveText = string.Empty;
        private string _emptyText = string.Empty;
        private bool _showEmptyText;
        private bool _isPeak;
        private bool _isDire;
        private bool _isReady;

        public MBBindingList<DoctrineSignalItemVM> Signals { get; } =
            new MBBindingList<DoctrineSignalItemVM>();

        public DoctrineBoardVM(FactionBuffMessageBehavior behavior, Action onClose)
        {
            _behavior = behavior ?? throw new ArgumentNullException(nameof(behavior));
            _onClose = onClose;
            Refresh();
        }

        public void Refresh()
        {
            DoctrineBoardSnapshot snapshot = _behavior.BuildDoctrineBoardSnapshot();

            TitleText = snapshot.TitleText;
            SignalText = snapshot.SignalText;
            ContextText = snapshot.ContextText;
            CadenceText = snapshot.CadenceText;
            ModeButtonText = snapshot.ModeButtonText;
            EffectsButtonText = snapshot.EffectsButtonText;
            ReportsButtonText = snapshot.ReportsButtonText;
            ScoreText = snapshot.ScoreText;
            ReadinessText = snapshot.ReadinessText;
            MoraleText = snapshot.MoraleText;
            SummaryLineOne = snapshot.SummaryLineOne;
            SummaryLineTwo = snapshot.SummaryLineTwo;
            SummaryLineThree = snapshot.SummaryLineThree;
            DirectiveText = snapshot.DirectiveText;
            EmptyText = snapshot.EmptyText;
            ShowEmptyText = snapshot.ShowEmptyText;
            IsPeak = snapshot.IsPeak;
            IsDire = snapshot.IsDire;
            IsReady = snapshot.IsReady;

            Signals.Clear();
            for (int i = 0; i < snapshot.Signals.Count; i++)
                Signals.Add(new DoctrineSignalItemVM(snapshot.Signals[i]));
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
        public string ContextText
        {
            get => _contextText;
            set { if (value != _contextText) { _contextText = value ?? string.Empty; OnPropertyChangedWithValue(_contextText); } }
        }

        [DataSourceProperty]
        public string CadenceText
        {
            get => _cadenceText;
            set { if (value != _cadenceText) { _cadenceText = value ?? string.Empty; OnPropertyChangedWithValue(_cadenceText); } }
        }

        [DataSourceProperty]
        public string ModeButtonText
        {
            get => _modeButtonText;
            set { if (value != _modeButtonText) { _modeButtonText = value ?? string.Empty; OnPropertyChangedWithValue(_modeButtonText); } }
        }

        [DataSourceProperty]
        public string EffectsButtonText
        {
            get => _effectsButtonText;
            set { if (value != _effectsButtonText) { _effectsButtonText = value ?? string.Empty; OnPropertyChangedWithValue(_effectsButtonText); } }
        }

        [DataSourceProperty]
        public string ReportsButtonText
        {
            get => _reportsButtonText;
            set { if (value != _reportsButtonText) { _reportsButtonText = value ?? string.Empty; OnPropertyChangedWithValue(_reportsButtonText); } }
        }

        [DataSourceProperty]
        public string ScoreText
        {
            get => _scoreText;
            set { if (value != _scoreText) { _scoreText = value ?? string.Empty; OnPropertyChangedWithValue(_scoreText); } }
        }

        [DataSourceProperty]
        public string ReadinessText
        {
            get => _readinessText;
            set { if (value != _readinessText) { _readinessText = value ?? string.Empty; OnPropertyChangedWithValue(_readinessText); } }
        }

        [DataSourceProperty]
        public string MoraleText
        {
            get => _moraleText;
            set { if (value != _moraleText) { _moraleText = value ?? string.Empty; OnPropertyChangedWithValue(_moraleText); } }
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
        public string DirectiveText
        {
            get => _directiveText;
            set { if (value != _directiveText) { _directiveText = value ?? string.Empty; OnPropertyChangedWithValue(_directiveText); } }
        }

        [DataSourceProperty]
        public string EmptyText
        {
            get => _emptyText;
            set { if (value != _emptyText) { _emptyText = value ?? string.Empty; OnPropertyChangedWithValue(_emptyText); } }
        }

        [DataSourceProperty]
        public bool ShowEmptyText
        {
            get => _showEmptyText;
            set { if (value != _showEmptyText) { _showEmptyText = value; OnPropertyChangedWithValue(value); } }
        }

        [DataSourceProperty]
        public bool IsPeak
        {
            get => _isPeak;
            set { if (value != _isPeak) { _isPeak = value; OnPropertyChangedWithValue(value); } }
        }

        [DataSourceProperty]
        public bool IsDire
        {
            get => _isDire;
            set { if (value != _isDire) { _isDire = value; OnPropertyChangedWithValue(value); } }
        }

        [DataSourceProperty]
        public bool IsReady
        {
            get => _isReady;
            set { if (value != _isReady) { _isReady = value; OnPropertyChangedWithValue(value); } }
        }

        public void ExecuteCycleMode()
        {
            _behavior.CycleDoctrineModeFromBoard();
            Refresh();
        }

        public void ExecuteToggleEffects()
        {
            _behavior.ToggleDoctrineEffectsFromBoard();
            Refresh();
        }

        public void ExecuteToggleReports()
        {
            _behavior.ToggleDoctrineReportsFromBoard();
            Refresh();
        }

        public void ExecuteStrategicSummary()
        {
            _behavior.ShowStrategicSummaryFromBoard();
            Refresh();
        }

        public void ExecuteRefresh()
        {
            Refresh();
        }

        public void ExecuteClose()
        {
            _onClose?.Invoke();
        }
    }
}
