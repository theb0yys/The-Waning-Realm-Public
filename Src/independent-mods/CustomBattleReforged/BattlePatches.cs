using System.Collections.Generic;
using HarmonyLib;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.CustomBattle;
using TaleWorlds.MountAndBlade.CustomBattle.CustomBattle;

namespace CustomBattleReforged
{
    [HarmonyPatch(typeof(CustomBattleVM), MethodType.Constructor, new[] { typeof(CustomBattleState) })]
    internal static class PrepareMenuPatch
    {
        [HarmonyPriority(Priority.First)]
        private static void Prefix() => RuntimeCatalog.Prepare();
    }

    [HarmonyPatch(typeof(CustomGame), nameof(CustomGame.OnDestroy))]
    internal static class ClearGamePatch
    {
        private static void Postfix() => RuntimeCatalog.Clear();
    }

    [HarmonyPatch(typeof(CustomBattleData), nameof(CustomBattleData.Factions), MethodType.Getter)]
    internal static class FactionsPatch
    {
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(ref IEnumerable<BasicCultureObject> __result)
        {
            if (RuntimeCatalog.Active) __result = RuntimeCatalog.Factions;
        }
    }

    [HarmonyPatch(typeof(CustomBattleData), nameof(CustomBattleData.Characters), MethodType.Getter)]
    internal static class CommandersPatch
    {
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(ref IEnumerable<BasicCharacterObject> __result)
        {
            if (RuntimeCatalog.Active) __result = RuntimeCatalog.Commanders;
        }
    }

    [HarmonyPatch(typeof(BasicCultureObject), nameof(BasicCultureObject.Name), MethodType.Getter)]
    internal static class CultureNamePatch
    {
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(BasicCultureObject __instance, ref TextObject __result)
        {
            if (RuntimeCatalog.Active && RuntimeCatalog.Names.TryGetValue(__instance.StringId, out var name)) __result = name;
        }
    }

    [HarmonyPatch(typeof(BasicCharacterObject), nameof(BasicCharacterObject.Equipment), MethodType.Getter)]
    internal static class BattleEquipmentPatch
    {
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(BasicCharacterObject __instance, ref Equipment __result)
        {
            if (RuntimeCatalog.Active && RuntimeCatalog.BattleEquipment.TryGetValue(__instance, out var battleEquipment))
                __result = battleEquipment;
        }
    }

    [HarmonyPatch(typeof(ArmyCompositionGroupVM), MethodType.Constructor, new[] { typeof(TroopTypeSelectionPopUpVM) })]
    internal static class ArmyCompositionPatch
    {
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(ArmyCompositionGroupVM __instance, List<BasicCharacterObject> ____allCharacterObjects)
        {
            if (!RuntimeCatalog.Active) return;
            __instance.MaxArmySize = CatalogDefinition.ArmyLimit;
            ____allCharacterObjects.RemoveAll(character => !RuntimeCatalog.Soldiers.Contains(character));
        }
    }

    [HarmonyPatch(typeof(ArmyCompositionItemVM), "GetDefaultCharacters")]
    internal static class DefaultTroopPatch
    {
        [HarmonyPriority(Priority.First)]
        private static bool Prefix(ref MBReadOnlyList<BasicCharacterObject> __result)
        {
            if (!RuntimeCatalog.Active) return true;
            // FullRosterDefaultsPatch selects every eligible member after population.
            // Do not call the vanilla culture-to-troop fallback for aliased cultures.
            __result = new MBList<BasicCharacterObject>();
            return false;
        }
    }

    internal static class TroopVariety
    {
        internal static void SelectAll(IEnumerable<CustomBattleTroopTypeVM> troops)
        {
            // Native population already filters by faction, formation and our TAOM
            // allowlist. Run on population/randomize only, never at battle start:
            // a player's subsequent manual deselections remain authoritative.
            foreach (CustomBattleTroopTypeVM troop in troops)
            {
                troop.IsDefault = true;
                troop.IsSelected = true;
            }
        }
    }

    [HarmonyPatch(typeof(ArmyCompositionItemVM), "PopulateTroopTypes")]
    internal static class FullRosterDefaultsPatch
    {
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(ArmyCompositionItemVM __instance)
        {
            if (RuntimeCatalog.Active) TroopVariety.SelectAll(__instance.TroopTypes);
        }
    }

    [HarmonyPatch(typeof(ArmyCompositionItemVM), nameof(ArmyCompositionItemVM.ExecuteRandomize))]
    internal static class FullRosterRandomizePatch
    {
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(ArmyCompositionItemVM __instance)
        {
            if (RuntimeCatalog.Active) TroopVariety.SelectAll(__instance.TroopTypes);
        }
    }
}
