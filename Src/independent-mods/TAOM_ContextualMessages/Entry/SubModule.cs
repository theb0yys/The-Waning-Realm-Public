using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.CampaignSystem;
using TaleWorlds.InputSystem;

namespace ContextualMessages
{
    public sealed class SubModule : MBSubModuleBase
    {
        private static ContextualMessageBehavior _behavior;

        protected override void OnGameStart(Game game, IGameStarter gameStarterObject)
        {
            base.OnGameStart(game, gameStarterObject);
            if (!(game.GameType is Campaign)) return;
            var starter = gameStarterObject as CampaignGameStarter;
            if (starter == null) return;

            _behavior = new ContextualMessageBehavior();
            starter.AddBehavior(_behavior);
        }

        public override void OnGameEnd(Game game)
        {
            base.OnGameEnd(game);
            _behavior = null;
        }

        protected override void OnApplicationTick(float dt)
        {
            base.OnApplicationTick(dt);

            if (Campaign.Current == null || _behavior == null)
                return;

            try
            {
                if (Input.IsKeyPressed(InputKey.F9))
                    _behavior.OpenSignalBoardOrFallback();
            }
            catch { }
        }
    }
}
