using System;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace PlayerJumpHeight
{
    internal static class JumpPolicy
    {
        internal static bool Eligible(bool multiplayer, bool playerCharacter, bool humanoid)
            => !multiplayer && playerCharacter && humanoid;

        internal static float StrengthFactor(bool enabled, float height)
        {
            if (!enabled || float.IsNaN(height) || float.IsInfinity(height)) return 1f;
            return (float)Math.Sqrt(Math.Max(1f, Math.Min(10f, height)));
        }
    }

    internal struct SpawnScope
    {
        internal float Strength;
        internal bool Consumed;
    }

    internal static class JumpSpawnContext
    {
        [ThreadStatic] private static SpawnScope current;

        internal static SpawnScope Enter(float strength)
        {
            var previous = current;
            current = new SpawnScope { Strength = strength };
            return previous;
        }

        internal static void Restore(SpawnScope previous) { current = previous; }

        internal static void Apply(AgentFlag flags, ref AgentSpawnData data)
        {
            if (current.Consumed) return;
            current.Consumed = true;
            if ((flags & AgentFlag.IsHumanoid) == 0 || current.Strength <= 1f) return;
            float original = data.JumpAcceleration;
            float adjusted = original * current.Strength;
            if (original > 0f && !float.IsNaN(adjusted) && !float.IsInfinity(adjusted))
                data.JumpAcceleration = adjusted;
        }
    }
}
