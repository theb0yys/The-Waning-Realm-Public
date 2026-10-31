using System;
using TaleWorlds.Library;

namespace ContextualMessages.UI.ViewModels
{
    public sealed class ContextualSignalBoardVM : ViewModel
    {
        private readonly ContextualMessageBehavior _behavior;
        private readonly Action _onClose;

        private string _titleText = string.Empty;
        private string _headerText = string.Empty;
        private string _cadenceText = string.Empty;
        private string _stanceButtonText = string.Empty;
        private string _focusButtonText = string.Empty;
        private string _moodText = string.Empty;
        private string _tensionText = string.Empty;
        private string _momentumText = string.Empty;
        private string _realmScoreText = string.Empty;
        private string _supplyScoreText = string.Empty;
        private string _adviceText = string.Empty;
        private string _realmText = string.Empty;
        private string _partyText = string.Empty;
        private string _categoryText = string.Empty;
        private string _bridgeText = string.Empty;
        private string _emptyText = string.Empty;
        private bool _showEmptyText;
        private bool _isAlert;
        private bool _isHopeful;
        private bool _isQuiet;

        public MBBindingList<ContextualSignalItemVM> Signals { get; private set; }

        public ContextualSignalBoardVM(ContextualMessageBehavior behavior, Action onClose)
        {
            if (behavior == null)
                throw new ArgumentNullException("behavior");

            _behavior = behavior;
            _onClose = onClose;
            Signals = new MBBindingList<ContextualSignalItemVM>();
            Refresh();
        }

        public void Refresh()
        {
            ContextualSignalBoardSnapshot snapshot = _behavior.BuildSignalBoardSnapshot();

            TitleText = snapshot.TitleText;
            HeaderText = snapshot.HeaderText;
            CadenceText = snapshot.CadenceText;
            StanceButtonText = snapshot.StanceButtonText;
            FocusButtonText = snapshot.FocusButtonText;
            MoodText = snapshot.MoodText;
            TensionText = snapshot.TensionText;
            MomentumText = snapshot.MomentumText;
            RealmScoreText = snapshot.RealmScoreText;
            SupplyScoreText = snapshot.SupplyScoreText;
            AdviceText = snapshot.AdviceText;
            RealmText = snapshot.RealmText;
            PartyText = snapshot.PartyText;
            CategoryText = snapshot.CategoryText;
            BridgeText = snapshot.BridgeText;
            EmptyText = snapshot.EmptyText;
            ShowEmptyText = snapshot.ShowEmptyText;
            IsAlert = snapshot.IsAlert;
            IsHopeful = snapshot.IsHopeful;
            IsQuiet = snapshot.IsQuiet;

            Signals.Clear();
            for (int i = 0; i < snapshot.Signals.Count; i++)
                Signals.Add(new ContextualSignalItemVM(snapshot.Signals[i]));
        }

        [DataSourceProperty]
        public string TitleText
        {
            get { return _titleText; }
            set { if (value != _titleText) { _titleText = value ?? string.Empty; OnPropertyChangedWithValue(_titleText); } }
        }

        [DataSourceProperty]
        public string HeaderText
        {
            get { return _headerText; }
            set { if (value != _headerText) { _headerText = value ?? string.Empty; OnPropertyChangedWithValue(_headerText); } }
        }

        [DataSourceProperty]
        public string CadenceText
        {
            get { return _cadenceText; }
            set { if (value != _cadenceText) { _cadenceText = value ?? string.Empty; OnPropertyChangedWithValue(_cadenceText); } }
        }

        [DataSourceProperty]
        public string StanceButtonText
        {
            get { return _stanceButtonText; }
            set { if (value != _stanceButtonText) { _stanceButtonText = value ?? string.Empty; OnPropertyChangedWithValue(_stanceButtonText); } }
        }

        [DataSourceProperty]
        public string FocusButtonText
        {
            get { return _focusButtonText; }
            set { if (value != _focusButtonText) { _focusButtonText = value ?? string.Empty; OnPropertyChangedWithValue(_focusButtonText); } }
        }

        [DataSourceProperty]
        public string MoodText
        {
            get { return _moodText; }
            set { if (value != _moodText) { _moodText = value ?? string.Empty; OnPropertyChangedWithValue(_moodText); } }
        }

        [DataSourceProperty]
        public string TensionText
        {
            get { return _tensionText; }
            set { if (value != _tensionText) { _tensionText = value ?? string.Empty; OnPropertyChangedWithValue(_tensionText); } }
        }

        [DataSourceProperty]
        public string MomentumText
        {
            get { return _momentumText; }
            set { if (value != _momentumText) { _momentumText = value ?? string.Empty; OnPropertyChangedWithValue(_momentumText); } }
        }

        [DataSourceProperty]
        public string RealmScoreText
        {
            get { return _realmScoreText; }
            set { if (value != _realmScoreText) { _realmScoreText = value ?? string.Empty; OnPropertyChangedWithValue(_realmScoreText); } }
        }

        [DataSourceProperty]
        public string SupplyScoreText
        {
            get { return _supplyScoreText; }
            set { if (value != _supplyScoreText) { _supplyScoreText = value ?? string.Empty; OnPropertyChangedWithValue(_supplyScoreText); } }
        }

        [DataSourceProperty]
        public string AdviceText
        {
            get { return _adviceText; }
            set { if (value != _adviceText) { _adviceText = value ?? string.Empty; OnPropertyChangedWithValue(_adviceText); } }
        }

        [DataSourceProperty]
        public string RealmText
        {
            get { return _realmText; }
            set { if (value != _realmText) { _realmText = value ?? string.Empty; OnPropertyChangedWithValue(_realmText); } }
        }

        [DataSourceProperty]
        public string PartyText
        {
            get { return _partyText; }
            set { if (value != _partyText) { _partyText = value ?? string.Empty; OnPropertyChangedWithValue(_partyText); } }
        }

        [DataSourceProperty]
        public string CategoryText
        {
            get { return _categoryText; }
            set { if (value != _categoryText) { _categoryText = value ?? string.Empty; OnPropertyChangedWithValue(_categoryText); } }
        }

        [DataSourceProperty]
        public string BridgeText
        {
            get { return _bridgeText; }
            set { if (value != _bridgeText) { _bridgeText = value ?? string.Empty; OnPropertyChangedWithValue(_bridgeText); } }
        }

        [DataSourceProperty]
        public string EmptyText
        {
            get { return _emptyText; }
            set { if (value != _emptyText) { _emptyText = value ?? string.Empty; OnPropertyChangedWithValue(_emptyText); } }
        }

        [DataSourceProperty]
        public bool ShowEmptyText
        {
            get { return _showEmptyText; }
            set { if (value != _showEmptyText) { _showEmptyText = value; OnPropertyChangedWithValue(value); } }
        }

        [DataSourceProperty]
        public bool IsAlert
        {
            get { return _isAlert; }
            set { if (value != _isAlert) { _isAlert = value; OnPropertyChangedWithValue(value); } }
        }

        [DataSourceProperty]
        public bool IsHopeful
        {
            get { return _isHopeful; }
            set { if (value != _isHopeful) { _isHopeful = value; OnPropertyChangedWithValue(value); } }
        }

        [DataSourceProperty]
        public bool IsQuiet
        {
            get { return _isQuiet; }
            set { if (value != _isQuiet) { _isQuiet = value; OnPropertyChangedWithValue(value); } }
        }

        public void ExecuteCycleStance()
        {
            _behavior.CycleSignalStanceFromBoard();
            Refresh();
        }

        public void ExecuteCycleFocus()
        {
            _behavior.CycleSignalFocusFromBoard();
            Refresh();
        }

        public void ExecuteShowReport()
        {
            _behavior.ShowStatusPanelFromBoard();
            Refresh();
        }

        public void ExecuteRefresh()
        {
            Refresh();
        }

        public void ExecuteClose()
        {
            if (_onClose != null)
                _onClose();
        }
    }
}
