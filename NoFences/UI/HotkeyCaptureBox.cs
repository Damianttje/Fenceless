using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace Fenceless.UI
{
    public class HotkeyCaptureBox : TextBox
    {
        private Keys capturedKey = Keys.None;
        private bool ctrl, alt, shift;

        public string HotkeyText => Text;

        public HotkeyCaptureBox()
        {
            ReadOnly = true;
            Font = Theme.Fonts.Body;
            BackColor = Theme.Colors.InputBackground;
            ForeColor = Theme.Colors.InputText;
            BorderStyle = BorderStyle.FixedSingle;
            Text = "";
            Cursor = Cursors.Default;
        }

        protected override void OnGotFocus(EventArgs e)
        {
            base.OnGotFocus(e);
            BackColor = Theme.Colors.InputBorderFocused;
            ForeColor = Theme.Colors.TextBright;
            if (string.IsNullOrEmpty(Text))
                Text = "Press keys...";
        }

        protected override void OnLostFocus(EventArgs e)
        {
            base.OnLostFocus(e);
            BackColor = Theme.Colors.InputBackground;
            ForeColor = Theme.Colors.InputText;
            if (Text == "Press keys...")
                Text = "";
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Escape)
            {
                Text = "";
                ctrl = alt = shift = false;
                capturedKey = Keys.None;
                return true;
            }

            if (keyData == Keys.Tab || keyData == (Keys.Tab | Keys.Shift))
                return base.ProcessCmdKey(ref msg, keyData);

            ctrl = (keyData & Keys.Control) != 0;
            alt = (keyData & Keys.Alt) != 0;
            shift = (keyData & Keys.Shift) != 0;

            Keys cleanKey = keyData & Keys.KeyCode;

            if (cleanKey == Keys.ControlKey || cleanKey == Keys.ShiftKey || cleanKey == Keys.Menu)
            {
                Text = BuildPartialString();
                return true;
            }

            capturedKey = cleanKey;
            Text = BuildHotkeyString();
            return true;
        }

        private string BuildPartialString()
        {
            var parts = new List<string>();
            if (ctrl) parts.Add("Ctrl");
            if (alt) parts.Add("Alt");
            if (shift) parts.Add("Shift");
            return string.Join("+", parts) + "+...";
        }

        private string BuildHotkeyString()
        {
            var parts = new List<string>();
            if (ctrl) parts.Add("Ctrl");
            if (alt) parts.Add("Alt");
            if (shift) parts.Add("Shift");
            parts.Add(KeyToString(capturedKey));
            return string.Join("+", parts);
        }

        private static string KeyToString(Keys key)
        {
            if (key >= Keys.D0 && key <= Keys.D9) return ((char)('0' + (key - Keys.D0))).ToString();
            if (key >= Keys.A && key <= Keys.Z) return ((char)('A' + (key - Keys.A))).ToString();
            if (key >= Keys.F1 && key <= Keys.F12) return $"F{key - Keys.F1 + 1}";
            if (key == Keys.Space) return "Space";
            if (key == Keys.Enter) return "Enter";
            if (key == Keys.Delete) return "Delete";
            if (key == Keys.Back) return "Backspace";
            if (key == Keys.Insert) return "Insert";
            if (key == Keys.Home) return "Home";
            if (key == Keys.End) return "End";
            if (key == Keys.PageUp) return "PageUp";
            if (key == Keys.PageDown) return "PageDown";
            return key.ToString();
        }
    }
}
