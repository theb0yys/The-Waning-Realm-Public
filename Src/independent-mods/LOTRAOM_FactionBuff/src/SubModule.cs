// ═══════════════════════════════════════════════════════════════════════════
//  LOTRAOM_FactionBuff  ·  SubModule.cs  (v2)
// ═══════════════════════════════════════════════════════════════════════════
using HarmonyLib;
using LOTRAOM.FactionBuff.Behaviors;
using LOTRAOM.FactionBuff.Data;
using LOTRAOM.FactionBuff.Evaluation;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;


namespace LOTRAOM.FactionBuff
{
    public sealed class SubModule : MBSubModuleBase
    {
        private Harmony? _harmony;

        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();
            FactionBuffDataPackLoader.LoadAll();
            _harmony = new Harmony("LOTRAOM.FactionBuff");
            _harmony.PatchAll(typeof(SubModule).Assembly);
        }

        // ── Game start (covers both NEW game and LOAD game paths) ─────────────
        protected override void OnGameStart(Game game, IGameStarter gameStarter)
        {
            base.OnGameStart(game, gameStarter);

            if (gameStarter is CampaignGameStarter campaignStarter)
            {
                FactionBuffPublisher.Clear();
                campaignStarter.AddBehavior(new FactionBuffMessageBehavior()); // v2 master — replaces FactionFoodBehavior + PrisonerMoraleBehavior
            }
        }

        protected override void OnSubModuleUnloaded()
        {
            base.OnSubModuleUnloaded();
            _harmony?.UnpatchAll(_harmony.Id);
        }
    }
}
