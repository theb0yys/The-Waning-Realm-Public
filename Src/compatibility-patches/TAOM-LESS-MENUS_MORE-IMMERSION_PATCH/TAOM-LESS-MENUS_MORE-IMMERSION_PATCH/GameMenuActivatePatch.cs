using HarmonyLib;
using TWDebug = TaleWorlds.Library.Debug;

namespace LessMenusMoreImmersion
{
    // Logs all campaign menu activations (TAOM will open custom menuIds).
    [HarmonyPatch("TaleWorlds.CampaignSystem.GameMenus.GameMenu", "ActivateGameMenu")]
    internal static class GameMenuActivatePatch
    {
        private static void Prefix(string menuId)
        {
            TWDebug.Print($"[LMMMI][MENU] ActivateGameMenu: '{menuId ?? "null"}'");
        }
    }
}