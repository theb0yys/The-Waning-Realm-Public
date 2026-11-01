using TaleWorlds.Library;

namespace TAOM_LordsLedger.UI.ViewModels
{
    public sealed class LedgerFiefItemVM : ViewModel
    {
        private readonly LedgerFiefSnapshot _fief;

        public LedgerFiefItemVM(LedgerFiefSnapshot fief)
        {
            _fief = fief ?? new LedgerFiefSnapshot();
        }

        [DataSourceProperty] public string Name => _fief.Name;
        [DataSourceProperty] public string TypeText => _fief.TypeText;
        [DataSourceProperty] public string StatusText => _fief.StatusText;
        [DataSourceProperty] public string RiskText => _fief.RiskText;
        [DataSourceProperty] public string ReadinessText => _fief.ReadinessText;
        [DataSourceProperty] public string MetricsLine => _fief.MetricsLine;
        [DataSourceProperty] public string MilitaryLine => _fief.MilitaryLine;
        [DataSourceProperty] public string ActionLine => _fief.ActionLine;
        [DataSourceProperty] public string GovernorLine => _fief.GovernorLine;
        [DataSourceProperty] public string ConstructionLine => _fief.ConstructionLine;
        [DataSourceProperty] public string PrisonersLine => _fief.PrisonersLine;
        [DataSourceProperty] public bool ShowGovernor => _fief.ShowGovernor;
        [DataSourceProperty] public bool ShowConstruction => _fief.ShowConstruction;
        [DataSourceProperty] public bool ShowPrisoners => _fief.ShowPrisoners;
        [DataSourceProperty] public bool IsCritical => _fief.IsCritical;
        [DataSourceProperty] public bool IsWarning => _fief.IsWarning;
        [DataSourceProperty] public bool IsStable => _fief.IsStable;
        [DataSourceProperty] public bool IsVillage => _fief.IsVillage;
    }
}
