using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace Fenceless.Util
{
    public readonly struct ShortcutDefinition
    {
        public ShortcutDefinition(Keys key, bool ctrl, bool alt, bool shift)
        {
            Key = key;
            Ctrl = ctrl;
            Alt = alt;
            Shift = shift;
        }

        public Keys Key { get; }
        public bool Ctrl { get; }
        public bool Alt { get; }
        public bool Shift { get; }
    }

    public static class ShortcutParser
    {
        private static readonly HashSet<Keys> AllowedUnmodifiedKeys = new HashSet<Keys>(
            Enumerable.Range((int)Keys.F1, (int)Keys.F12 - (int)Keys.F1 + 1).Select(v => (Keys)v));

        public static bool TryParse(string shortcut, out ShortcutDefinition definition)
        {
            definition = default;

            if (string.IsNullOrWhiteSpace(shortcut))
                return false;

            var ctrl = false;
            var alt = false;
            var shift = false;
            var hasWindowsModifier = false;
            var key = Keys.None;

            foreach (var rawPart in shortcut.Split('+'))
            {
                var part = rawPart.Trim();
                if (part.Length == 0)
                    return false;

                if (part.Equals("Ctrl", StringComparison.OrdinalIgnoreCase) ||
                    part.Equals("Control", StringComparison.OrdinalIgnoreCase))
                {
                    ctrl = true;
                    continue;
                }

                if (part.Equals("Alt", StringComparison.OrdinalIgnoreCase))
                {
                    alt = true;
                    continue;
                }

                if (part.Equals("Shift", StringComparison.OrdinalIgnoreCase))
                {
                    shift = true;
                    continue;
                }

                if (part.Equals("Windows", StringComparison.OrdinalIgnoreCase) ||
                    part.Equals("Win", StringComparison.OrdinalIgnoreCase))
                {
                    hasWindowsModifier = true;
                    continue;
                }

                if (!Enum.TryParse(part, true, out Keys parsedKey))
                    return false;

                if (key != Keys.None)
                    return false;

                key = parsedKey;
            }

            if (hasWindowsModifier || key == Keys.None || IsModifierOnly(key))
                return false;

            if (!ctrl && !alt && !shift && !AllowedUnmodifiedKeys.Contains(key))
                return false;

            definition = new ShortcutDefinition(key, ctrl, alt, shift);
            return true;
        }

        public static string ValidateOrDefault(string shortcut, string fallback)
        {
            return TryParse(shortcut, out _) ? shortcut : fallback;
        }

        private static bool IsModifierOnly(Keys key)
        {
            return key == Keys.Control ||
                   key == Keys.ControlKey ||
                   key == Keys.LControlKey ||
                   key == Keys.RControlKey ||
                   key == Keys.Alt ||
                   key == Keys.Menu ||
                   key == Keys.LMenu ||
                   key == Keys.RMenu ||
                   key == Keys.Shift ||
                   key == Keys.ShiftKey ||
                   key == Keys.LShiftKey ||
                   key == Keys.RShiftKey ||
                   key == Keys.LWin ||
                   key == Keys.RWin;
        }
    }
}
