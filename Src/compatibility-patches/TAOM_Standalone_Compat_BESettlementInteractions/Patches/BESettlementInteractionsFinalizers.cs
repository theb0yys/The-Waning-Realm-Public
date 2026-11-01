using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using TAOM.Compat.Shared;

namespace TAOM_Standalone_Compat_BESettlementInteractions
{
    internal static class TargetResolver
    {
        private static readonly string[] PatchTypes =
        {
            "BannerlordExpanded.SettlementInteractions.HostPartyForSoldiers.Patches.SettlementMusiciansCampaignBehaviorPatch_HostPartyForSoldiers",
            "BannerlordExpanded.SettlementInteractions.HostPartyForSoldiers.Patches.SettlementMusiciansCampaignBehaviorPatch_Inn",
            "BannerlordExpanded.SettlementInteractions.Inns.Patches.VillageEncounterPatch"
        };

        private static readonly string[] BehaviorTypes =
        {
            "BannerlordExpanded.SettlementInteractions.HostPartyForSoldiers.Behaviors.HostPartyForSoldiersBehavior",
            "BannerlordExpanded.SettlementInteractions.HostPartyForSoldiers.Behaviors.HostPartyRoomBehavior",
            "BannerlordExpanded.SettlementInteractions.Inns.Behaviors.InnBehavior",
            "BannerlordExpanded.SettlementInteractions.Inns.Behaviors.InnWanderersBehavior",
            "BannerlordExpanded.SettlementInteractions.TrainCompanions.Behaviors.TrainCompanionsBehavior",
            "BannerlordExpanded.SettlementInteractions.TrainCompanions.MissionControllers.CompanionDuelMissionController",
            "BannerlordExpanded.StartYourOwnTournaments.Behaviors.StartYourOwnTournamentsBehavior"
        };

        internal static IEnumerable<MethodBase> PatchBoolMethods() => CompatResolver.ResolveMethods(PatchTypes, CompatResolver.IsPatchBool, "BE Settlement Interactions bool patch", true);
        internal static IEnumerable<MethodBase> PatchVoidMethods() => CompatResolver.ResolveMethods(PatchTypes, CompatResolver.IsPatchVoid, "BE Settlement Interactions void patch", true);
        internal static IEnumerable<MethodBase> BehaviorBoolMethods() => CompatResolver.ResolveMethods(BehaviorTypes, IsBehaviorBool, "BE Settlement Interactions behavior bool", true);
        internal static IEnumerable<MethodBase> BehaviorVoidMethods() => CompatResolver.ResolveMethods(BehaviorTypes, IsBehaviorVoid, "BE Settlement Interactions behavior void", true);

        private static bool IsBehaviorBool(MethodInfo method)
        {
            if (method.ReturnType != typeof(bool))
                return false;

            var name = method.Name.ToLowerInvariant();
            return name.Contains("condition") || name.Contains("missionended") || name.Contains("readytospawn") ||
                   name.StartsWith("can") || name.StartsWith("is") || name.StartsWith("back");
        }

        private static bool IsBehaviorVoid(MethodInfo method)
        {
            if (method.ReturnType != typeof(void))
                return false;

            var name = method.Name.ToLowerInvariant();
            string[] fragments =
            {
                "registerevents", "syncdata", "add", "request", "start", "stop", "on", "open",
                "setup", "cleanup", "create", "activate", "deactivate", "refresh", "delete",
                "adjust", "enter", "populate", "spawn", "tick"
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
    internal static class BESIPatchBoolFinalizer
    {
        [HarmonyTargetMethods]
        internal static IEnumerable<MethodBase> TargetMethods() => TargetResolver.PatchBoolMethods();
        internal static Exception Finalizer(MethodBase __originalMethod, Exception __exception)
        {
            if (__exception != null) CompatLog.Suppressed("BE Settlement Interactions bool patch", __originalMethod, __exception);
            return null;
        }
    }

    [HarmonyPatch]
    internal static class BESIPatchVoidFinalizer
    {
        [HarmonyTargetMethods]
        internal static IEnumerable<MethodBase> TargetMethods() => TargetResolver.PatchVoidMethods();
        internal static Exception Finalizer(MethodBase __originalMethod, Exception __exception)
        {
            if (__exception != null) CompatLog.Suppressed("BE Settlement Interactions void patch", __originalMethod, __exception);
            return null;
        }
    }

    [HarmonyPatch]
    internal static class BESIBehaviorBoolFinalizer
    {
        [HarmonyTargetMethods]
        internal static IEnumerable<MethodBase> TargetMethods() => TargetResolver.BehaviorBoolMethods();
        internal static Exception Finalizer(MethodBase __originalMethod, Exception __exception, ref bool __result)
        {
            if (__exception != null)
            {
                __result = false;
                CompatLog.Suppressed("BE Settlement Interactions behavior bool", __originalMethod, __exception);
            }
            return null;
        }
    }

    [HarmonyPatch]
    internal static class BESIBehaviorVoidFinalizer
    {
        [HarmonyTargetMethods]
        internal static IEnumerable<MethodBase> TargetMethods() => TargetResolver.BehaviorVoidMethods();
        internal static Exception Finalizer(MethodBase __originalMethod, Exception __exception)
        {
            if (__exception != null) CompatLog.Suppressed("BE Settlement Interactions behavior void", __originalMethod, __exception);
            return null;
        }
    }
}
