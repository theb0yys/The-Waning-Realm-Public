using HarmonyLib;
using TaleWorlds.MountAndBlade;

namespace NoFallDamage
{
    [HarmonyPatch(typeof(Mission), "FallDamageCallback")]
    internal static class FallDamagePatch
    {
        [HarmonyPrefix]
        internal static bool Prefix(ref AttackCollisionData collisionData)
        {
            if (GameNetwork.IsMultiplayerOrReplay) return true;

            // This dedicated callback owns both the fall blow and mount-to-rider
            // propagation. Do not intercept the shared combat/health pathways.
            collisionData.BaseMagnitude = 0f;
            collisionData.InflictedDamage = 0;
            collisionData.SelfInflictedDamage = 0;
            return false;
        }
    }
}
