using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using TAOM.Compat.Shared;

namespace TAOM_Standalone_Compat_BECompanionExpanded
{
    internal static class TargetResolver
    {
        private static readonly string[] PatchTypes =
        {
            "BannerlordExpanded.CompanionExpanded.AdditionalCompanionSlots.Patches.DefaultClanTierModelPatch_CompanionLimit",
            "BannerlordExpanded.CompanionExpanded.GiveItems.Patches.GiveItemActionPatch",
            "BannerlordExpanded.CompanionExpanded.MoreFollowers.Patches.MoreFollowersInTowns",
            "BannerlordExpanded.CompanionExpanded.SpawnWanderers.Patches.CompanionsCampaignBehaviorDesiredTotalCompanionCountPatch",
            "BannerlordExpanded.CompanionExpanded.SpawnWanderers.Patches.CompanionsCampaignBehaviorGetCompanionTemplateToSpawnPatch",
            "BannerlordExpanded.CompanionExpanded.SpawnWanderers.Patches.CompanionsCampaignBehaviorNewGameCreatedPatch",
            "BannerlordExpanded.CompanionExpanded.SpawnWanderers.Patches.CompanionsCampaignBehaviorTrySpawnNewCompanionPatch",
            "BannerlordExpanded.CompanionExpanded.WanderersInTown.Patches.CompanionsInTownPatch",
            "BannerlordExpanded.SpousesExpanded.PregnancyAge.Patches.TroopLostCountPatch"
        };

        private static readonly string[] BehaviorTypes =
        {
            "BannerlordExpanded.CompanionExpanded.CompanionsRequestToJoin.Behaviors.CompanionRequestToJoinBehavior",
            "BannerlordExpanded.CompanionExpanded.CompanionsToFamily.Behaviors.CompanionToFamilyBehavior",
            "BannerlordExpanded.CompanionExpanded.CompanionsToFamily.Behaviors.CompanionToSiblingBehavior",
            "BannerlordExpanded.CompanionExpanded.CompanionsToFamily.Behaviors.DisownChildToCompanionBehavior",
            "BannerlordExpanded.CompanionExpanded.GiveItems.Behaviors.GiveGiftsBehavior",
            "BannerlordExpanded.CompanionExpanded.ModBaseDialog.Behaviors.BECEBaseDialogBehavior",
            "BannerlordExpanded.CompanionExpanded.SpawnWanderers.Behaviors.SpawnCompanionsBehavior"
        };

        internal static IEnumerable<MethodBase> PatchBoolMethods() => CompatResolver.ResolveMethods(PatchTypes, CompatResolver.IsPatchBool, "BE Companion Expanded bool patch", true);
        internal static IEnumerable<MethodBase> PatchVoidMethods() => CompatResolver.ResolveMethods(PatchTypes, CompatResolver.IsPatchVoid, "BE Companion Expanded void patch", true);
        internal static IEnumerable<MethodBase> BehaviorBoolMethods() => CompatResolver.ResolveMethods(BehaviorTypes, IsBehaviorBool, "BE Companion Expanded behavior bool", true);
        internal static IEnumerable<MethodBase> BehaviorVoidMethods() => CompatResolver.ResolveMethods(BehaviorTypes, IsBehaviorVoid, "BE Companion Expanded behavior void", true);

        private static bool IsBehaviorBool(MethodInfo method)
        {
            if (method.ReturnType != typeof(bool))
                return false;

            var name = method.Name.ToLowerInvariant();
            return name.Contains("condition") || name.Contains("success") || name.Contains("family") ||
                   name.Contains("sibling") || name.Contains("request") ||
                   name.StartsWith("can") || name.StartsWith("is") || name.StartsWith("should");
        }

        private static bool IsBehaviorVoid(MethodInfo method)
        {
            if (method.ReturnType != typeof(void))
                return false;

            var name = method.Name.ToLowerInvariant();
            string[] fragments =
            {
                "registerevents", "syncdata", "weeklytick", "cleanup", "refresh", "adddialogs",
                "open", "convert", "disown", "onmission", "onsettlement", "make", "request", "accepted"
            };

            foreach (var fragment in fragments)
            {
                if (name.Contains(fragment))
                    return true;
            }

            return false;
        }
    }

    [HarmonyPatch]
    internal static class BECEPatchBoolFinalizer
    {
        [HarmonyTargetMethods]
        internal static IEnumerable<MethodBase> TargetMethods() => TargetResolver.PatchBoolMethods();
        internal static Exception Finalizer(MethodBase __originalMethod, Exception __exception)
        {
            if (__exception != null) CompatLog.Suppressed("BE Companion Expanded bool patch", __originalMethod, __exception);
            return null;
        }
    }

    [HarmonyPatch]
    internal static class BECEPatchVoidFinalizer
    {
        [HarmonyTargetMethods]
        internal static IEnumerable<MethodBase> TargetMethods() => TargetResolver.PatchVoidMethods();
        internal static Exception Finalizer(MethodBase __originalMethod, Exception __exception)
        {
            if (__exception != null) CompatLog.Suppressed("BE Companion Expanded void patch", __originalMethod, __exception);
            return null;
        }
    }

    [HarmonyPatch]
    internal static class BECEBehaviorBoolFinalizer
    {
        [HarmonyTargetMethods]
        internal static IEnumerable<MethodBase> TargetMethods() => TargetResolver.BehaviorBoolMethods();
        internal static Exception Finalizer(MethodBase __originalMethod, Exception __exception, ref bool __result)
        {
            if (__exception != null)
            {
                __result = false;
                CompatLog.Suppressed("BE Companion Expanded behavior bool", __originalMethod, __exception);
            }
            return null;
        }
    }

    [HarmonyPatch]
    internal static class BECEBehaviorVoidFinalizer
    {
        [HarmonyTargetMethods]
        internal static IEnumerable<MethodBase> TargetMethods() => TargetResolver.BehaviorVoidMethods();
        internal static Exception Finalizer(MethodBase __originalMethod, Exception __exception)
        {
            if (__exception != null) CompatLog.Suppressed("BE Companion Expanded behavior void", __originalMethod, __exception);
            return null;
        }
    }
}
