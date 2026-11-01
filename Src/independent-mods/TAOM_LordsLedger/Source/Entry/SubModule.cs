using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace TAOM_LordsLedger
{
    public sealed class SubModule : MBSubModuleBase
    {
        protected override void OnGameStart(Game game, IGameStarter gameStarterObject)
        {
            base.OnGameStart(game, gameStarterObject);

            if (gameStarterObject is CampaignGameStarter cgs)
            {
                try
                {
                    cgs.AddBehavior(new LedgerCampaignBehavior());
                }
                catch (Exception ex)
                {
                    InformationManager.DisplayMessage(
                        new InformationMessage("[Lord's Ledger] Init failed: " + ex.Message, Colors.Red));
                }
            }
        }
    }
}
