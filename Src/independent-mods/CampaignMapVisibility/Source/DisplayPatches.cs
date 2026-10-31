using HarmonyLib;
using SandBox.ViewModelCollection.Map;
using SandBox.ViewModelCollection.Nameplate;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.GauntletUI.Widgets.Nameplate;

namespace CampaignMapVisibility
{
    [HarmonyPatch(typeof(SettlementNameplateWidget), "UpdateNameplateTransparencyAndBrightness")]
    internal static class SettlementScoutingEyePatch
    {
        private static void Postfix(SettlementNameplateWidget __instance)
        {
            if (!MapOptions.KeepMarkers) return;
            // Native code restores this alpha on every UI update while IsInRange is true.
            // Suppress the decorative eye after that update, without changing scouting truth.
            var eye = __instance.NameplateItem?.InspectedIconWidget;
            if (eye != null) eye.AlphaFactor = 0f;
        }
    }

    [HarmonyPatch(typeof(PartyNameplateVM), nameof(PartyNameplateVM.DetermineIsVisibleOnMap))]
    internal static class PartyZoomPatch
    {
        private static void Postfix(float ____latestW, float ____latestX, float ____latestY,
            ref bool ____isVisibleOnMapBind)
        {
            if (MapOptions.KeepMarkers)
                ____isVisibleOnMapBind = VisibilityPolicy.IsOnScreen(____latestW, ____latestX, ____latestY,
                    Screen.RealScreenResolutionWidth, Screen.RealScreenResolutionHeight);
        }
    }

    [HarmonyPatch(typeof(SettlementNameplateVM), "IsVisible")]
    internal static class SettlementZoomPatch
    {
        private static void Postfix(SettlementNameplateVM __instance, bool ____latestIsInsideWindow, ref bool __result)
        {
            if (MapOptions.KeepMarkers)
                __result = __instance.IsTracked || (__instance.WPos >= 0f && ____latestIsInsideWindow);
        }
    }

    [HarmonyPatch(typeof(MapEventVisualItemVM), nameof(MapEventVisualItemVM.DetermineIsVisibleOnMap))]
    internal static class BattleZoomPatch
    {
        private static void Postfix(MapEventVisualItemVM __instance, float ____latestW,
            float ____latestX, float ____latestY, bool ____isAVisibleEvent,
            TaleWorlds.Engine.Camera ____mapCamera, ref bool ____bindIsVisibleOnMap)
        {
            if (!MapOptions.KeepMarkers && !MapOptions.Reveal) return;
            bool withinZoom = MapOptions.KeepMarkers || ____mapCamera.Position.z < 200f;
            ____bindIsVisibleOnMap = VisibilityPolicy.ShowBattle(withinZoom &&
                VisibilityPolicy.IsOnScreen(____latestW, ____latestX, ____latestY,
                    Screen.RealScreenResolutionWidth, Screen.RealScreenResolutionHeight),
                ____isAVisibleEvent, MapOptions.Reveal, __instance.MapEvent.IsFinalized);
        }
    }

    [HarmonyPatch(typeof(MapEventVisualsVM), nameof(MapEventVisualsVM.Update))]
    internal static class BattleOptionsPatch
    {
        private static void Prefix() => MapOptions.Refresh();
    }
}
