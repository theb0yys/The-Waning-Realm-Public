using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using TAOM.Compat.Shared;

namespace TAOM_Standalone_Compat_AgricultureEstate
{
    internal static class TargetResolver
    {
        private static readonly string[] PatchTypes =
        {
            "AgricultureEstate.ClanFiancePatch",
            "AgricultureEstate.HearthChange2Patch",
            "AgricultureEstate.HearthChangePatch",
            "AgricultureEstate.HearthLevelPatch",
            "AgricultureEstate.PartyNamePlatePatch",
            "AgricultureEstate.SellPrisonersPatch"
        };

        private static readonly string[] BehaviorTypes =
        {
            "AgricultureEstate.AgricultureEstateBehavior"
        };

        internal static IEnumerable<MethodBase> PatchBoolMethods()
        {
            return CompatResolver.ResolveMethods(PatchTypes, CompatResolver.IsPatchBool, "Agriculture Estate bool patch");
        }

        internal static IEnumerable<MethodBase> PatchVoidMethods()
        {
            return CompatResolver.ResolveMethods(PatchTypes, CompatResolver.IsPatchVoid, "Agriculture Estate void patch");
        }

        internal static IEnumerable<MethodBase> BehaviorVoidMethods()
        {
            return CompatResolver.ResolveMethods(BehaviorTypes, IsBehaviorVoid, "Agriculture Estate behavior void");
        }

        private static bool IsBehaviorVoid(MethodInfo method)
        {
            if (method.ReturnType != typeof(void))
                return false;

            var name = method.Name.ToLowerInvariant();
            string[] fragments =
            {
                "registerevents", "onnewgame", "setadditional", "addperk", "onsettlement",
                "tick", "remove", "start", "project", "collect", "slave", "production",
                "produce", "menuitems", "create", "delete", "syncdata"
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
    internal static class AgriculturePatchBoolFinalizer
    {
        [HarmonyTargetMethods]
        internal static IEnumerable<MethodBase> TargetMethods() => TargetResolver.PatchBoolMethods();

        internal static Exception Finalizer(MethodBase __originalMethod, Exception __exception)
        {
            if (__exception != null)
                CompatLog.Suppressed("Agriculture Estate bool patch", __originalMethod, __exception);
            return null;
        }
    }

    [HarmonyPatch]
    internal static class AgriculturePatchVoidFinalizer
    {
        [HarmonyTargetMethods]
        internal static IEnumerable<MethodBase> TargetMethods() => TargetResolver.PatchVoidMethods();

        internal static Exception Finalizer(MethodBase __originalMethod, Exception __exception)
        {
            if (__exception != null)
                CompatLog.Suppressed("Agriculture Estate void patch", __originalMethod, __exception);
            return null;
        }
    }

    [HarmonyPatch]
    internal static class AgricultureBehaviorVoidFinalizer
    {
        [HarmonyTargetMethods]
        internal static IEnumerable<MethodBase> TargetMethods() => TargetResolver.BehaviorVoidMethods();

        internal static Exception Finalizer(MethodBase __originalMethod, Exception __exception)
        {
            if (__exception != null)
                CompatLog.Suppressed("Agriculture Estate behavior void", __originalMethod, __exception);
            return null;
        }
    }
}
