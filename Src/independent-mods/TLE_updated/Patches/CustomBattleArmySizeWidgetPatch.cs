using HarmonyLib;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TroopLimitExpander.Patches
{
    [HarmonyPatch(typeof(IntegerInputTextWidget), "set_MaxInt")]
    public static class CustomBattleArmySizeInputMaxIntPatch
    {
        private static void Postfix(IntegerInputTextWidget __instance)
        {
            CustomBattleArmySizeInputPatchHelper.Apply(__instance);
        }
    }

    [HarmonyPatch(typeof(IntegerInputTextWidget), "set_IntText")]
    public static class CustomBattleArmySizeInputIntTextPatch
    {
        private static void Postfix(IntegerInputTextWidget __instance)
        {
            CustomBattleArmySizeInputPatchHelper.Apply(__instance, forceRefresh: true);
        }
    }

    [HarmonyPatch(typeof(EditableTextWidget), "set_MaxLength")]
    public static class CustomBattleArmySizeInputMaxLengthPatch
    {
        private static void Postfix(EditableTextWidget __instance)
        {
            CustomBattleArmySizeInputPatchHelper.ApplyIfIntegerInput(__instance, forceRefresh: true);
        }
    }

    [HarmonyPatch(typeof(Widget), "set_SuggestedWidth")]
    public static class CustomBattleArmySizeInputSuggestedWidthPatch
    {
        private static void Postfix(Widget __instance)
        {
            CustomBattleArmySizeInputPatchHelper.ApplyIfIntegerInput(__instance);
        }
    }

    internal static class CustomBattleArmySizeInputPatchHelper
    {
        private const int RequiredMaxLength = 5;
        private const float RequiredSuggestedWidth = 148f;

        public static void ApplyIfIntegerInput(Widget widget, bool forceRefresh = false)
        {
            if (widget is IntegerInputTextWidget input)
                Apply(input, forceRefresh);
        }

        public static void Apply(IntegerInputTextWidget widget, bool forceRefresh = false)
        {
            try
            {
                ApplyCore(widget, forceRefresh);
            }
            catch
            {
                // Never let a cosmetic input fix break the Custom Battle screen.
            }
        }

        private static void ApplyCore(IntegerInputTextWidget widget, bool forceRefresh)
        {
            if (widget == null)
                return;

            if (!HasCompositionGroupAncestor(widget))
                return;

            bool changed = false;

            if (widget.MaxLength < RequiredMaxLength)
            {
                widget.MaxLength = RequiredMaxLength;
                changed = true;
            }

            if (widget.SuggestedWidth < RequiredSuggestedWidth)
            {
                widget.SuggestedWidth = RequiredSuggestedWidth;
                changed = true;
            }

            if (widget.MinWidth < RequiredSuggestedWidth)
            {
                widget.MinWidth = RequiredSuggestedWidth;
                changed = true;
            }

            if (widget.ClipContents)
            {
                widget.ClipContents = false;
                changed = true;
            }

            if (widget.ClipHorizontalContent)
            {
                widget.ClipHorizontalContent = false;
                changed = true;
            }

            if (changed && forceRefresh)
            {
                int currentValue = widget.IntText;
                widget.IntText = currentValue;
            }
        }

        private static bool HasCompositionGroupAncestor(Widget widget)
        {
            for (Widget? current = widget; current != null; current = current.ParentWidget)
            {
                if (current.Id == "CompositionGroup")
                    return true;
            }

            return false;
        }
    }
}
