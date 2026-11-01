using SandBox.View.Map;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.ViewModelCollection;
using TaleWorlds.Core;
using TaleWorlds.InputSystem;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ScreenSystem;
using TAOM_CheatPanel.Settings;

namespace TAOM_CheatPanel.UI
{
    internal static class CheatPanelHotkey
    {
        internal static void Tick()
        {
            var mapState = Game.Current?.GameStateManager?.ActiveState as MapState;
            var screen = ScreenManager.TopScreen as MapScreen;
            if (mapState == null || screen == null || !screen.IsReady || Mission.Current != null
                || Campaign.Current == null || Hero.MainHero == null || MobileParty.MainParty == null
                || mapState.MapConversationActive || mapState.IsSimulationActive
                || screen.IsEscapeMenuOpened || screen.IsInCampaignOptions
                || screen.IsInArmyManagement || screen.IsInTownManagement || screen.IsInRecruitment
                || screen.IsInHideoutTroopManage || screen.IsMarriageOfferPopupActive
                || screen.IsHeirSelectionPopupActive || screen.IsMapCheatsActive
                || screen.IsMapIncidentActive || screen.IsOverlayContextMenuEnabled)
                return;

            var input = screen.SceneLayer?.Input;
            var settings = CheatPanelSettings.Instance;
            var key = CheatPanelHotkeyOptions.Resolve(settings.OpenPanelKey);
            if (input == null || !input.IsKeysAllowed || ScreenManager.FocusedLayer?.IsFocusedOnInput() == true
                || key == InputKey.Invalid
                || !input.IsKeyPressed(key)
                || !CheatPanelHotkeyOptions.ModifiersMatch(settings.OpenPanelRequireControl,
                    input.IsKeyDown(InputKey.LeftControl) || input.IsKeyDown(InputKey.RightControl),
                    input.IsKeyDown(InputKey.LeftAlt) || input.IsKeyDown(InputKey.RightAlt),
                    input.IsKeyDown(InputKey.LeftShift) || input.IsKeyDown(InputKey.RightShift)))
                return;

            if (!CheatPanelHotkeyOptions.CanOpenFrom(mapState.GameMenuId)
                || !CampaignUIHelper.GetMapScreenActionIsEnabledWithReason(out _))
                return;
            Campaign.Current.GetCampaignBehavior<CheatPanelMenuBehavior>()?.OpenFromHotkey();
        }
    }
}
