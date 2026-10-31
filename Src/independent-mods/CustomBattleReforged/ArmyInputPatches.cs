using HarmonyLib;
using TaleWorlds.GauntletUI.BaseTypes;

namespace CustomBattleReforged
{
    internal static class ArmyInput
    {
        internal static bool Matches(Widget widget)
        {
            if (!RuntimeCatalog.Active || !(widget is IntegerInputTextWidget input) || input.MaxInt <= 100) return false;
            for (int depth = 0; widget != null && depth < 32; depth++, widget = widget.ParentWidget)
                if (widget.Id == "CompositionGroup") return true;
            return false;
        }

        internal static void Apply(IntegerInputTextWidget widget)
        {
            if (!Matches(widget)) return;
            // Idempotence also prevents recursive setter patches.
            if (widget.MaxLength < 4) widget.MaxLength = 4;
            if (widget.SuggestedWidth < 148f) widget.SuggestedWidth = 148f;
            if (widget.MinWidth < 148f) widget.MinWidth = 148f;
            if (widget.ClipContents) widget.ClipContents = false;
            if (widget.ClipHorizontalContent) widget.ClipHorizontalContent = false;
        }
    }

    [HarmonyPatch(typeof(IntegerInputTextWidget), "set_MaxInt")]
    internal static class ArmyInputMaximumPatch
    {
        private static void Postfix(IntegerInputTextWidget __instance) => ArmyInput.Apply(__instance);
    }

    [HarmonyPatch(typeof(IntegerInputTextWidget), "set_IntText")]
    internal static class ArmyInputValuePatch
    {
        private static void Postfix(IntegerInputTextWidget __instance) => ArmyInput.Apply(__instance);
    }

    [HarmonyPatch(typeof(EditableTextWidget), "set_MaxLength")]
    internal static class ArmyInputLengthPatch
    {
        private static void Prefix(EditableTextWidget __instance, ref int value)
        {
            if (value < 4 && ArmyInput.Matches(__instance)) value = 4;
        }
    }

    [HarmonyPatch(typeof(Widget), "set_SuggestedWidth")]
    internal static class ArmyInputWidthPatch
    {
        private static void Prefix(Widget __instance, ref float value)
        {
            if (value < 148f && ArmyInput.Matches(__instance)) value = 148f;
        }
    }
}
