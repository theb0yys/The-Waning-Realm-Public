using TaleWorlds.Library;

namespace LOTRAOM.FactionBuff.UI.ViewModels
{
    public sealed class DoctrineSignalItemVM : ViewModel
    {
        private readonly DoctrineSignalSnapshot _signal;

        public DoctrineSignalItemVM(DoctrineSignalSnapshot signal)
        {
            _signal = signal ?? new DoctrineSignalSnapshot();
        }

        [DataSourceProperty] public string Name => _signal.Name;
        [DataSourceProperty] public string ValueText => _signal.ValueText;
        [DataSourceProperty] public string DetailText => _signal.DetailText;
        [DataSourceProperty] public string StatusText => _signal.StatusText;
        [DataSourceProperty] public bool IsGood => _signal.IsGood;
        [DataSourceProperty] public bool IsWatch => _signal.IsWatch;
        [DataSourceProperty] public bool IsBad => _signal.IsBad;
    }
}
