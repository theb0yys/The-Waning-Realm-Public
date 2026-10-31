using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace PlayerSpeedControl
{
    [HarmonyPatch(typeof(AgentDrivenProperties), "UpdateDrivenProperties")]
    internal static class RunningSpeedPatch
    {
        private static Agent tracked;
        private static readonly RunningAdjustment Adjustment = new RunningAdjustment();

        internal static float Factor(Agent agent)
        {
            if (agent == null || !ReferenceEquals(agent, Mission.Current?.MainAgent)) return 1f;
            return SpeedPolicy.CanRun(true, agent.IsActive(), agent.IsHuman, agent.MountAgent != null,
                agent.WalkMode, (agent.MovementMode & AgentMovementMode.Land) != 0) ? SpeedOptions.Running : 1f;
        }

        [HarmonyPrefix]
        private static void Prefix(AgentDrivenProperties __instance, Agent agent)
        {
            if (ReferenceEquals(agent, tracked))
                __instance.MaxSpeedMultiplier = Adjustment.Restore(__instance.MaxSpeedMultiplier);
        }

        [HarmonyPostfix]
        private static void Postfix(AgentDrivenProperties __instance, Agent agent)
        {
            float factor = Factor(agent);
            if (factor == 1f) return;
            if (!ReferenceEquals(agent, tracked))
            {
                Release();
                tracked = agent;
            }
            // The returned array is this object's backing array; Agent pushes it to native
            // immediately after this method returns. No second native update is needed.
            __instance.MaxSpeedMultiplier = Adjustment.Apply(__instance.MaxSpeedMultiplier, factor);
        }

        internal static void Release(bool pushNative = true)
        {
            if (tracked != null && tracked.AgentDrivenProperties != null)
            {
                var properties = tracked.AgentDrivenProperties;
                properties.MaxSpeedMultiplier = Adjustment.Restore(properties.MaxSpeedMultiplier);
                if (pushNative && tracked.IsActive()) tracked.UpdateCustomDrivenProperties();
            }
            tracked = null;
        }
    }

    [HarmonyPatch(typeof(DefaultPartySpeedCalculatingModel), nameof(DefaultPartySpeedCalculatingModel.CalculateFinalSpeed))]
    internal static class TravelSpeedPatch
    {
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(MobileParty mobileParty, ref ExplainedNumber __result)
        {
            if (SpeedOptions.Travel == 1f || mobileParty == null) return;
            if (SpeedPolicy.CanTravel(ReferenceEquals(mobileParty, MobileParty.MainParty),
                mobileParty.IsCurrentlyAtSea, mobileParty.AttachedTo != null))
                __result = SpeedPolicy.ApplyTravel(__result, SpeedOptions.Travel);
        }
    }
}
