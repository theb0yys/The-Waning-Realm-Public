using ContextualMessages.UI;
using TaleWorlds.Library;

namespace ContextualMessages.UI.ViewModels
{
    public sealed class ContextualSignalItemVM : ViewModel
    {
        private readonly ContextualSignalSnapshot _signal;

        public ContextualSignalItemVM(ContextualSignalSnapshot signal)
        {
            _signal = signal ?? new ContextualSignalSnapshot();
        }

        [DataSourceProperty] public string Name { get { return _signal.Name ?? string.Empty; } }
        [DataSourceProperty] public string ValueText { get { return _signal.ValueText ?? string.Empty; } }
        [DataSourceProperty] public string DetailText { get { return _signal.DetailText ?? string.Empty; } }
        [DataSourceProperty] public string StatusText { get { return _signal.StatusText ?? string.Empty; } }
        [DataSourceProperty] public bool IsGood { get { return _signal.IsGood; } }
        [DataSourceProperty] public bool IsWatch { get { return _signal.IsWatch; } }
        [DataSourceProperty] public bool IsBad { get { return _signal.IsBad; } }
    }
}
