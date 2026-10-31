using System;
using HarmonyLib;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace PlayerJumpHeight
{
    [HarmonyPatch(typeof(Mission), "CreateAgent")]
    internal static class PlayerSpawnPatch
    {
        [HarmonyPrefix]
        private static void Prefix(Monster monster, BasicCharacterObject characterObject, out SpawnScope __state)
        {
            // Capture restoration state before any settings/provider code can throw.
            __state = JumpSpawnContext.Enter(1f);
            var player = Game.Current?.PlayerTroop;
            float factor = 1f;
            if (JumpPolicy.Eligible(GameNetwork.IsMultiplayer,
                player != null && ReferenceEquals(player, characterObject),
                monster != null && (monster.Flags & AgentFlag.IsHumanoid) != 0))
            {
                var settings = JumpSettings.Instance;
                // Missing MCM registration fails to native behavior, not an invisible boost.
                if (settings != null) factor = JumpPolicy.StrengthFactor(settings.Enabled, settings.HeightMultiplier);
            }
            JumpSpawnContext.Enter(factor);
        }

        [HarmonyFinalizer]
        private static Exception Finalizer(Exception __exception, SpawnScope __state)
        {
            JumpSpawnContext.Restore(__state);
            return __exception;
        }
    }

    [HarmonyPatch(typeof(Mission), "CreateAgentInternal")]
    internal static class NativeSpawnPatch
    {
        [HarmonyPrefix]
        private static void Prefix(AgentFlag agentFlags, ref AgentSpawnData spawnData)
        {
            JumpSpawnContext.Apply(agentFlags, ref spawnData);
        }
    }
}
