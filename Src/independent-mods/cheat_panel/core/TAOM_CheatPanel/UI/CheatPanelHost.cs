using System;
using TaleWorlds.Library;
using TAOM_CheatPanel.Settings;

namespace TAOM_CheatPanel.UI
{
    internal static class CheatPanelHost
    {
        public static void RequestConfirmation(string message, Action onConfirm)
        {
            if (onConfirm == null) return;
            CheatPanelSettings settings = null;
            try { settings = CheatPanelSettings.Instance; } catch { }
            if (settings != null && !settings.Enabled)
            {
                InformationManager.DisplayMessage(new InformationMessage("Enable cheats in Cheat Panel Settings first."));
                return;
            }
            if (settings != null && !settings.ShowConfirmations)
            {
                onConfirm();
                return;
            }

            InformationManager.ShowInquiry(new InquiryData(
                "TAOM Cheat Panel", message, true, true, "Confirm", "Cancel", onConfirm, null));
        }
    }
}
