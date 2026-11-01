using System;
using System.Collections.Generic;
using TaleWorlds.InputSystem;

namespace TAOM_CheatPanel.UI
{
    internal static class CheatPanelHotkeyOptions
    {
        internal static readonly string[] Names = {
            "Disabled", "F1", "F2", "F3", "F4", "F5", "F6", "F7", "F8", "F9", "F10", "F11", "F12",
            "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M",
            "N", "O", "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z",
            "D0", "D1", "D2", "D3", "D4", "D5", "D6", "D7", "D8", "D9",
            "Insert", "Delete", "Home", "End", "PageUp", "PageDown"
        };
        private static readonly Dictionary<string, InputKey> Keys = CreateKeys();

        private static Dictionary<string, InputKey> CreateKeys()
        {
            var keys = new Dictionary<string, InputKey>(StringComparer.Ordinal);
            foreach (var name in Names)
                keys.Add(name, Enum.TryParse(name, out InputKey key) ? key : InputKey.Invalid);
            return keys;
        }

        internal static InputKey Resolve(string name)
        {
            return name != null && Keys.TryGetValue(name, out var key) ? key : InputKey.Invalid;
        }

        internal static bool ModifiersMatch(bool requireControl, bool control, bool alt, bool shift)
        {
            return control == requireControl && !alt && !shift;
        }

        internal static bool CanOpenFrom(string menu)
        {
            return string.IsNullOrEmpty(menu) || menu == "town" || menu == "castle"
                || menu == "castle_outside" || menu == "village";
        }
    }
}
