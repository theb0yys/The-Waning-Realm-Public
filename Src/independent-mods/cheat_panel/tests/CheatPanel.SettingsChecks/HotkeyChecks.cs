using System;
using System.Diagnostics;
using System.IO;
using System.Xml.Serialization;
using TaleWorlds.InputSystem;
using TAOM_CheatPanel.Settings;
using TAOM_CheatPanel.UI;

internal static class HotkeyChecks
{
    internal static void Run(Action<bool, string> check)
    {
        var serializer = new XmlSerializer(typeof(CheatPanelSettings));
        var old = (CheatPanelSettings)serializer.Deserialize(new StringReader(
            "<CheatPanelSettings><DefaultGold>1234</DefaultGold></CheatPanelSettings>"));
        check(old.OpenPanelKey == "F10" && old.OpenPanelRequireControl && old.DefaultGold == 1234,
            "Old XML retains values and defaults the shortcut to Ctrl+F10");
        old.OpenPanelKey = "K";
        old.OpenPanelRequireControl = false;
        var xml = new StringWriter();
        serializer.Serialize(xml, old);
        var restored = (CheatPanelSettings)serializer.Deserialize(new StringReader(xml.ToString()));
        check(restored.OpenPanelKey == "K" && !restored.OpenPanelRequireControl, "Rebound shortcut XML roundtrip");
        foreach (var key in CheatPanelHotkeyOptions.Names)
            check(key == "Disabled" || CheatPanelHotkeyOptions.Resolve(key) != InputKey.Invalid, "Supported key " + key);
        foreach (var key in new[] { null, "", "Disabled", "bogus", "999", "Escape", "LeftControl", "LeftMouseButton" })
            check(CheatPanelHotkeyOptions.Resolve(key) == InputKey.Invalid, "Unknown/disabled/reserved key fails closed: " + key);
        for (int bits = 0; bits < 16; bits++)
        {
            bool required = (bits & 1) != 0, ctrl = (bits & 2) != 0;
            bool alt = (bits & 4) != 0, shift = (bits & 8) != 0;
            check(CheatPanelHotkeyOptions.ModifiersMatch(required, ctrl, alt, shift)
                == (required == ctrl && !alt && !shift), "Modifier combination " + bits);
        }
        foreach (var menu in new[] { null, "", "town", "castle", "castle_outside", "village" })
            check(CheatPanelHotkeyOptions.CanOpenFrom(menu), "Accepted origin " + menu);
        foreach (var menu in new[] { "encounter", "siege", "town_arena", "taom_cheat_panel_root", "taom_cheat_panel_settings", "unknown_mod_menu" })
            check(!CheatPanelHotkeyOptions.CanOpenFrom(menu), "Do not interrupt " + menu);
        var timer = Stopwatch.StartNew();
        int hits = 0;
        for (int i = 0; i < 1000000; i++)
            if (CheatPanelHotkeyOptions.Resolve("F10") == InputKey.F10 && CheatPanelHotkeyOptions.CanOpenFrom(null)) hits++;
        timer.Stop();
        check(hits == 1000000 && timer.ElapsedMilliseconds < 5000, "Bounded hotkey lookup budget");
        Console.WriteLine("Hotkey lookup: 1000000 calls, " + timer.ElapsedMilliseconds + "ms; budget <5000ms; managed policy only.");
    }
}
