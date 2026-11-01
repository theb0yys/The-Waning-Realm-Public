using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using TAOM.Compat.Shared;

namespace TAOM_Standalone_Compat_DistinguishedService
{
    internal static class TargetResolver
    {
        private static readonly string[] TargetTypes =
        {
            "DistinguishedService.DSBattleBehavior",
            "DistinguishedService.DSBattleLogic",
            "DistinguishedService.dsSubModule",
            "DistinguishedService.PromotionManager"
        };

        internal static IEnumerable<MethodBase> BoolMethods() => CompatResolver.ResolveMethods(TargetTypes, IsBool, "Distinguished Service bool");
        internal static IEnumerable<MethodBase> VoidMethods() => CompatResolver.ResolveMethods(TargetTypes, IsVoid, "Distinguished Service void");
        internal static IEnumerable<MethodBase> StringMethods() => CompatResolver.ResolveMethods(TargetTypes, IsString, "Distinguished Service string");

        private static bool IsBool(MethodInfo method)
        {
            if (method.ReturnType != typeof(bool))
                return false;
            var name = method.Name.ToLowerInvariant();
            return name.Contains("condition") || name.Contains("qualified") || name.Contains("captured") ||
                   name.StartsWith("is") || name.StartsWith("can") || name.StartsWith("check");
        }

        private static bool IsVoid(MethodInfo method)
        {
            if (method.ReturnType != typeof(void))
                return false;
            var name = method.Name.ToLowerInvariant();
            string[] fragments =
            {
                "registerevents", "syncdata", "findbattle", "showbattleresults", "battleended",
                "nominee", "adjust", "trait", "promote", "assign", "shuffle", "recruit",
                "upgrade", "mapevent", "adddialogs", "consequence", "transfer", "ongame",
                "deserialize", "serialize"
            };
            foreach (var fragment in fragments)
            {
                if (name.Contains(fragment))
                    return true;
            }
            return false;
        }

        private static bool IsString(MethodInfo method)
        {
            if (method.ReturnType != typeof(string))
                return false;
            var name = method.Name.ToLowerInvariant();
            return name.Contains("name") || name.Contains("cheat");
        }
    }

    [HarmonyPatch]
    internal static class DistinguishedServiceBoolFinalizer
    {
        [HarmonyTargetMethods]
        internal static IEnumerable<MethodBase> TargetMethods() => TargetResolver.BoolMethods();
        internal static Exception Finalizer(MethodBase __originalMethod, Exception __exception, ref bool __result)
        {
            if (__exception != null)
            {
                __result = false;
                CompatLog.Suppressed("Distinguished Service bool", __originalMethod, __exception);
            }
            return null;
        }
    }

    [HarmonyPatch]
    internal static class DistinguishedServiceVoidFinalizer
    {
        [HarmonyTargetMethods]
        internal static IEnumerable<MethodBase> TargetMethods() => TargetResolver.VoidMethods();
        internal static Exception Finalizer(MethodBase __originalMethod, Exception __exception)
        {
            if (__exception != null) CompatLog.Suppressed("Distinguished Service void", __originalMethod, __exception);
            return null;
        }
    }

    [HarmonyPatch]
    internal static class DistinguishedServiceStringFinalizer
    {
        [HarmonyTargetMethods]
        internal static IEnumerable<MethodBase> TargetMethods() => TargetResolver.StringMethods();
        internal static Exception Finalizer(MethodBase __originalMethod, Exception __exception, ref string __result)
        {
            if (__exception != null)
            {
                __result = string.Empty;
                CompatLog.Suppressed("Distinguished Service string", __originalMethod, __exception);
            }
            return null;
        }
    }
}
