using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using TAOM.Compat.Shared;

namespace TAOM_Standalone_Compat_GovernorsHandleIssues
{
    internal static class TargetResolver
    {
        private static readonly string[] PatchTypes =
        {
            "GovernorsHandleIssues.Patches.ChangeRelationActionPatch",
            "GovernorsHandleIssues.Patches.AddRenownPatch",
            "GovernorsHandleIssues.Patches.AlternativeSolutionHeroPatch",
            "GovernorsHandleIssues.Patches.GiveGoldActionPatch",
            "GovernorsHandleIssues.Patches.GainRenownActionPatch",
            "GovernorsHandleIssues.Patches.VillageNeedsToolsPatch",
            "GovernorsHandleIssues.Patches.TraitLevelingHelperPatch",
            "GovernorsHandleIssues.Patches.CriminalRatingActionPatch",
            "GovernorsHandleIssues.Patches.OnSettlementTickPatch"
        };

        private static readonly string[] BehaviorTypes =
        {
            "GovernorsHandleIssues.GovernorsHandleIssuesCampaignBehavior"
        };

        internal static IEnumerable<MethodBase> PatchBoolMethods() => CompatResolver.ResolveMethods(PatchTypes, m => m.ReturnType == typeof(bool) && CompatResolver.IsPrefixOrPostfix(m), "GHI bool patch");
        internal static IEnumerable<MethodBase> PatchVoidMethods() => CompatResolver.ResolveMethods(PatchTypes, m => m.ReturnType == typeof(void) && CompatResolver.IsPrefixOrPostfix(m), "GHI void patch");
        internal static IEnumerable<MethodBase> BehaviorBoolMethods() => CompatResolver.ResolveMethods(BehaviorTypes, m => m.ReturnType == typeof(bool) && IsBoolCallbackName(m.Name), "GHI behavior bool");
        internal static IEnumerable<MethodBase> BehaviorVoidMethods() => CompatResolver.ResolveMethods(BehaviorTypes, m => m.ReturnType == typeof(void) && IsVoidCallbackName(m.Name), "GHI behavior void");

        private static bool IsBoolCallbackName(string methodName)
        {
            var name = methodName.ToLowerInvariant();
            return name.Contains("condition") || name.Contains("meets") || name.Contains("timetoupdate") ||
                   name.StartsWith("can") || name.StartsWith("should") || name.StartsWith("is");
        }

        private static bool IsVoidCallbackName(string methodName)
        {
            var name = methodName.ToLowerInvariant();
            string[] fragments =
            {
                "registerevents", "on", "handle", "do", "custom", "apply", "call",
                "clear", "kill", "complete", "resolve", "success", "failure"
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
    internal static class GhiPatchBoolFinalizer
    {
        [HarmonyTargetMethods]
        internal static IEnumerable<MethodBase> TargetMethods() => TargetResolver.PatchBoolMethods();
        internal static Exception Finalizer(MethodBase __originalMethod, Exception __exception)
        {
            if (__exception != null) CompatLog.Suppressed("GHI bool patch", __originalMethod, __exception);
            return null;
        }
    }

    [HarmonyPatch]
    internal static class GhiPatchVoidFinalizer
    {
        [HarmonyTargetMethods]
        internal static IEnumerable<MethodBase> TargetMethods() => TargetResolver.PatchVoidMethods();
        internal static Exception Finalizer(MethodBase __originalMethod, Exception __exception)
        {
            if (__exception != null) CompatLog.Suppressed("GHI void patch", __originalMethod, __exception);
            return null;
        }
    }

    [HarmonyPatch]
    internal static class GhiBehaviorBoolFinalizer
    {
        [HarmonyTargetMethods]
        internal static IEnumerable<MethodBase> TargetMethods() => TargetResolver.BehaviorBoolMethods();
        internal static Exception Finalizer(MethodBase __originalMethod, Exception __exception, ref bool __result)
        {
            if (__exception != null)
            {
                __result = false;
                CompatLog.Suppressed("GHI behavior bool", __originalMethod, __exception);
            }
            return null;
        }
    }

    [HarmonyPatch]
    internal static class GhiBehaviorVoidFinalizer
    {
        [HarmonyTargetMethods]
        internal static IEnumerable<MethodBase> TargetMethods() => TargetResolver.BehaviorVoidMethods();
        internal static Exception Finalizer(MethodBase __originalMethod, Exception __exception)
        {
            if (__exception != null) CompatLog.Suppressed("GHI behavior void", __originalMethod, __exception);
            return null;
        }
    }
}
