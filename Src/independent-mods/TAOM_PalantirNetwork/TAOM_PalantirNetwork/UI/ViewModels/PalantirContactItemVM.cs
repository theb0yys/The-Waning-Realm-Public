using TaleWorlds.Library;

namespace TAOM_PalantirNetwork.UI.ViewModels
{
    public sealed class PalantirContactItemVM : ViewModel
    {
        private readonly PalantirContactSnapshot _contact;

        public PalantirContactItemVM(PalantirContactSnapshot contact)
        {
            _contact = contact ?? new PalantirContactSnapshot();
        }

        [DataSourceProperty] public string Name => _contact.Name;
        [DataSourceProperty] public string Severity => _contact.Severity;
        [DataSourceProperty] public string Kind => _contact.Kind;
        [DataSourceProperty] public string FactionLine => _contact.FactionLine;
        [DataSourceProperty] public string PositionLine => _contact.PositionLine;
        [DataSourceProperty] public string CounselLine => _contact.CounselLine;
        [DataSourceProperty] public string DangerText => _contact.DangerText;
        [DataSourceProperty] public string LocationLine => _contact.LocationLine;
        [DataSourceProperty] public string HistoryLine => _contact.HistoryLine;
        [DataSourceProperty] public bool ShowLocation => _contact.ShowLocation;
        [DataSourceProperty] public bool ShowHistory => _contact.ShowHistory;
        [DataSourceProperty] public bool IsCritical => _contact.IsCritical;
        [DataSourceProperty] public bool IsWatch => _contact.IsWatch;
        [DataSourceProperty] public bool IsHostile => _contact.IsHostile;
        [DataSourceProperty] public bool IsInfo => _contact.IsInfo;
    }
}
