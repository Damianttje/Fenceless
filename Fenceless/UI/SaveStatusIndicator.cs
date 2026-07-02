using System;
using System.Drawing;
using System.Windows.Forms;

namespace Fenceless.UI
{
    /// <summary>
    /// A small footer label reflecting auto-save state:
    /// Idle / Pending / Saved / Error. Wired to the existing debounce logic.
    /// </summary>
    public class SaveStatusIndicator : Label
    {
        public enum State { Idle, Pending, Saved, Error }

        private State _state = State.Idle;
        private DateTime _savedAt;

        public SaveStatusIndicator()
        {
            this.Font = Theme.Fonts.Caption;
            this.ForeColor = Theme.Colors.TextSecondary;
            this.BackColor = Color.Transparent;
            this.AutoSize = false;
            this.TextAlign = ContentAlignment.MiddleLeft;
            this.Padding = new Padding(12, 0, 0, 0);
            this.Height = 20;
            UpdateText();
        }

        public void SetPending() { _state = State.Pending; UpdateText(); }
        public void SetSaved() { _state = State.Saved; _savedAt = DateTime.Now; UpdateText(); }
        public void SetError() { _state = State.Error; UpdateText(); }
        public void SetIdle() { _state = State.Idle; UpdateText(); }

        private void UpdateText()
        {
            switch (_state)
            {
                case State.Pending:
                    this.Text = "\u2022 Saving\u2026";
                    this.ForeColor = Theme.Colors.Warning;
                    break;
                case State.Saved:
                    this.Text = $"\u2713 Saved at {_savedAt:HH:mm:ss}";
                    this.ForeColor = Theme.Colors.Success;
                    break;
                case State.Error:
                    this.Text = "\u26A0 Save failed \u2014 see logs";
                    this.ForeColor = Theme.Colors.Error;
                    break;
                default:
                    this.Text = "All changes saved";
                    this.ForeColor = Theme.Colors.TextSecondary;
                    break;
            }
            this.Invalidate();
        }
    }
}
