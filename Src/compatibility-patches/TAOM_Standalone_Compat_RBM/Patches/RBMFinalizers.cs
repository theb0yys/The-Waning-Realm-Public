using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using TAOM.Compat.Shared;
using TaleWorlds.MountAndBlade;

namespace TAOM_Standalone_Compat_RBM
{
    internal static class TargetResolver
    {
        private static readonly string[] PatchTypes =
        {
            "RBM.XmlLoadingPatches", "RBMCombat.ModifyArmorPatch", "RBMCombat.ModifyModifyDamagePatch",
            "RBMCombat.GetAttackCollisionResultsPatch", "RBMCombat.OverrideRangedSiegeWeapon",
            "RBMCombat.OverrideMangonel", "RBMCombat.HandleMissileCollisionReactionPatch",
            "RBMCombat.MeleeHitCallbackPatch", "RBMCombat.MissileHitCallbackPatch",
            "RBMCombat.RealisticWeaponCollision", "RBMAI.OverrideBehaviorCautiousAdvance",
            "RBMAI.OverrideBehaviorMountedSkirmish", "RBMAI.OverrideBehaviorProtectFlank",
            "RBMAI.AdjustSpeedLimitPatch", "RBMAI.OverrideBehaviorHorseArcherSkirmish",
            "RBMAI.OverrideBehaviorPullBack", "RBMAI.OverrideBehaviorVanguard",
            "RBMAI.OverrideHumanAIComponent", "RBMAI.OverrideMovementOrder", "RBMAI.OverrideAgent",
            "RBMAI.OverrideUpdateFormationOrders", "RBMAI.SetPositioningPatch",
            "RBMAI.OverrideSetMovementOrder", "RBMAI.OverrideBehaviorRegroup",
            "RBMAI.OverrideBehaviorAdvance", "RBMAI.OverrideLineFormation", "RBMAI.OverrideFormation",
            "RBMAI.OverrideFormationMovementComponent", "RBMAI.TacticDefensiveEngagementPatch",
            "RBMAI.AiModule.SiegePatches", "RBMAI.AiModule.SpawningPatches",
            "RBMAI.AiModule.RbmTactics.TacticDefensiveLinePatch",
            "RBMAI.AiModule.RbmTactics.TacticFullScaleAttackPatch",
            "RBMAI.AiModule.RbmBehaviors.OverrideBehaviorCharge"
        };

        internal static IEnumerable<MethodBase> PatchBoolMethods() => CompatResolver.ResolveMethods(PatchTypes, m => m.ReturnType == typeof(bool) && CompatResolver.IsPrefixOrPostfix(m), "RBM bool patch");
        internal static IEnumerable<MethodBase> PatchVoidMethods() => CompatResolver.ResolveMethods(PatchTypes, m => m.ReturnType == typeof(void) && CompatResolver.IsPrefixOrPostfix(m), "RBM void patch");

        internal static IEnumerable<MethodBase> RbmBlowMethods()
        {
            var results = new List<MethodBase>();
            var method = AccessTools.Method("RBMCombat.RealisticWeaponCollision:CreateMeleeBlowPostFix");
            if (method != null && method.ReturnType.FullName == "TaleWorlds.MountAndBlade.Blow")
                results.Add(method);
            CompatLog.Info("Resolved RBM Blow targets: " + results.Count);
            return results;
        }

        internal static IEnumerable<MethodBase> TournamentTierMethods()
        {
            var results = new List<MethodBase>();
            var method = AccessTools.Method("RBMTournament.RBMTournament:calculatePlayerTournamentTier");
            if (method != null && method.ReturnType == typeof(int))
                results.Add(method);
            CompatLog.Info("Resolved RBM tournament tier targets: " + results.Count);
            return results;
        }
    }

    [HarmonyPatch]
    internal static class RbmPatchBoolFinalizer
    {
        [HarmonyTargetMethods]
        internal static IEnumerable<MethodBase> TargetMethods() => TargetResolver.PatchBoolMethods();
        internal static Exception Finalizer(MethodBase __originalMethod, Exception __exception)
        {
            if (__exception != null) CompatLog.Suppressed("RBM bool patch", __originalMethod, __exception);
            return null;
        }
    }

    [HarmonyPatch]
    internal static class RbmPatchVoidFinalizer
    {
        [HarmonyTargetMethods]
        internal static IEnumerable<MethodBase> TargetMethods() => TargetResolver.PatchVoidMethods();
        internal static Exception Finalizer(MethodBase __originalMethod, Exception __exception)
        {
            if (__exception != null) CompatLog.Suppressed("RBM void patch", __originalMethod, __exception);
            return null;
        }
    }

    [HarmonyPatch]
    internal static class RbmBlowFinalizer
    {
        [HarmonyTargetMethods]
        internal static IEnumerable<MethodBase> TargetMethods() => TargetResolver.RbmBlowMethods();

        internal static Exception Finalizer(MethodBase __originalMethod, object[] __args, Exception __exception, ref Blow __result)
        {
            if (__exception != null)
            {
                if (__args != null && __args.Length > 0 && __args[0] is Blow)
                    __result = (Blow)__args[0];
                CompatLog.Suppressed("RBM Blow patch", __originalMethod, __exception);
            }
            return null;
        }
    }

    [HarmonyPatch]
    internal static class RbmTournamentTierFinalizer
    {
        [HarmonyTargetMethods]
        internal static IEnumerable<MethodBase> TargetMethods() => TargetResolver.TournamentTierMethods();

        internal static Exception Finalizer(MethodBase __originalMethod, Exception __exception, ref int __result)
        {
            if (__exception != null)
            {
                __result = 1;
                CompatLog.Suppressed("RBM tournament tier", __originalMethod, __exception);
            }
            return null;
        }
    }
}
