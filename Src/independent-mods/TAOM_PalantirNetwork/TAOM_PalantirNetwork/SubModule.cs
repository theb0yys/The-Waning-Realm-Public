using System;
using Bannerlord.UIExtenderEx;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace TAOM_PalantirNetwork
{
    public sealed class SubModule : MBSubModuleBase
    {
        private UIExtender _extender;

        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();

            try
            {
                _extender = UIExtender.Create("TAOM_PalantirNetwork");
                _extender.Register(typeof(SubModule).Assembly);
                _extender.Enable();
            }
            catch (Exception ex)
            {
                LogError($"UIExtender registration failed: {ex.Message}");
            }
        }

        protected override void OnGameStart(Game game, IGameStarter gameStarterObject)
        {
            base.OnGameStart(game, gameStarterObject);

            try
            {
                if (gameStarterObject is CampaignGameStarter cgs)
                    cgs.AddBehavior(new PalantirCampaignBehavior());
            }
            catch (Exception ex)
            {
                LogError($"Failed to add PalantirCampaignBehavior: {ex.Message}");
            }
        }

        private static void LogError(string message)
        {
            try
            {
                InformationManager.DisplayMessage(
                    new InformationMessage($"[Palantir] ERROR: {message}", Colors.Red));
            }
            catch { }
        }
    }
}
