using System;
using Bannerlord.UIExtenderEx.Attributes;
using Bannerlord.UIExtenderEx.ViewModels;
using TaleWorlds.CampaignSystem.ViewModelCollection.GameMenu;
using TaleWorlds.Library;

namespace TAOM_PalantirNetwork.UI
{
    /// <summary>
    /// Injects a Palantir-flavoured native icon into the game-menu item view model.
    /// The icon is shown only on the "Close the Palantir" (leave) option so
    /// the whole menu is visually distinct from generic menus.
    /// </summary>
    [ViewModelMixin(nameof(GameMenuItemVM.RefreshValues))]
    internal sealed class GameMenuItemPalantirIconMixin : BaseViewModelMixin<GameMenuItemVM>
    {
        private const string PalantirSprite  = @"SPGeneral\MapNotification\notification_illustration_conspiracy_quest";
        private const string CLOSE_OPTION_ID = "taom_palantir_close";

        private bool   _palantirIconVisible;
        private bool   _palantirLeaveTypeIconVisible = true;
        private string _palantirIconSprite           = PalantirSprite;

        [DataSourceProperty]
        public bool PalantirIconVisible
        {
            get => _palantirIconVisible;
            set => SetField(ref _palantirIconVisible, value, nameof(PalantirIconVisible));
        }

        [DataSourceProperty]
        public bool PalantirLeaveTypeIconVisible
        {
            get => _palantirLeaveTypeIconVisible;
            set => SetField(ref _palantirLeaveTypeIconVisible, value, nameof(PalantirLeaveTypeIconVisible));
        }

        [DataSourceProperty]
        public string PalantirIconSprite
        {
            get => _palantirIconSprite;
            set => SetField(ref _palantirIconSprite, value, nameof(PalantirIconSprite));
        }

        public GameMenuItemPalantirIconMixin(GameMenuItemVM vm) : base(vm)
        {
            Update();
        }

        public override void OnRefresh()
        {
            try { Update(); }
            catch { /* Defensive: never crash the UI refresh loop */ }
        }

        private void Update()
        {
            var vm = ViewModel;
            if (vm == null)
            {
                PalantirIconVisible          = false;
                PalantirLeaveTypeIconVisible = true;
                return;
            }

            bool isPalantirClose = string.Equals(
                vm.OptionID, CLOSE_OPTION_ID, StringComparison.Ordinal);

            PalantirIconVisible          = isPalantirClose;
            PalantirLeaveTypeIconVisible = !isPalantirClose;

            if (isPalantirClose)
                PalantirIconSprite = PalantirSprite;
        }
    }
}
