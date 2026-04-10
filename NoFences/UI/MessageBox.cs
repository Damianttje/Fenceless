using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Fenceless.Util;

namespace Fenceless.UI
{
    public class CustomMessageBox : ThemedForm
    {
        private Label messageLabel;
        private Panel iconPanel;

        private static readonly Color IconInfo = Color.FromArgb(0, 122, 204);
        private static readonly Color IconWarning = Color.FromArgb(204, 163, 0);
        private static readonly Color IconError = Color.FromArgb(204, 52, 51);
        private static readonly Color IconSuccess = Color.FromArgb(87, 166, 74);
        private static readonly Color IconQuestion = Color.FromArgb(0, 122, 204);

        private static readonly string IconInfoSymbol = "\u2139";
        private static readonly string IconWarningSymbol = "\u26A0";
        private static readonly string IconErrorSymbol = "\u2715";
        private static readonly string IconQuestionSymbol = "?";

        public CustomMessageBox(string message, string title = "Fenceless", MessageBoxButtons buttons = MessageBoxButtons.OK, MessageBoxIcon icon = MessageBoxIcon.Information)
        {
            SetupThemedForm(title, showMinimize: false, showMaximize: false, sizable: false);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.ClientSize = new Size(420, 200);
            this.MinimumSize = new Size(300, 150);
            this.TopMost = true;

            CreateContent(message, icon);
            CreateButtons(buttons);
        }

        private void CreateContent(string message, MessageBoxIcon icon)
        {
            var contentPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.Colors.BackgroundMid,
                Padding = new Padding(20, 12, 20, 0)
            };

            iconPanel = new Panel
            {
                Size = new Size(36, 36),
                Location = new Point(0, 0),
                BackColor = Color.Transparent
            };

            var iconColor = GetIconColor(icon);
            var iconSymbol = GetIconSymbol(icon);

            var iconCircle = new Panel
            {
                Size = new Size(36, 36),
                BackColor = iconColor
            };

            var iconLabel = new Label
            {
                Text = iconSymbol,
                Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                AutoSize = false,
                Size = new Size(36, 36),
                TextAlign = ContentAlignment.MiddleCenter
            };

            iconCircle.Controls.Add(iconLabel);
            iconPanel.Controls.Add(iconCircle);

            messageLabel = new Label
            {
                Text = message,
                Font = Theme.Fonts.Body,
                ForeColor = Theme.Colors.TextPrimary,
                BackColor = Color.Transparent,
                AutoSize = false,
                Location = new Point(48, 0),
                Size = new Size(contentPanel.Width - 68, 60),
                TextAlign = ContentAlignment.TopLeft
            };

            messageLabel.Paint += (s, e) =>
            {
                var label = s as Label;
                if (label != null)
                {
                    var rect = new Rectangle(0, 0, label.Width, label.Height);
                    var format = new StringFormat
                    {
                        Alignment = StringAlignment.Near,
                        LineAlignment = StringAlignment.Near,
                        FormatFlags = StringFormatFlags.LineLimit
                    };
                    e.Graphics.DrawString(label.Text, label.Font, new SolidBrush(label.ForeColor), rect, format);
                }
            };

            contentPanel.Controls.Add(messageLabel);
            contentPanel.Controls.Add(iconPanel);

            var footerPanel = new Panel
            {
                Height = 48,
                Dock = DockStyle.Bottom,
                BackColor = Theme.Colors.BackgroundDark,
                Padding = new Padding(0, 0, 12, 0),
                Name = "footerPanel"
            };

            this.Controls.Add(contentPanel);
            this.Controls.Add(footerPanel);

            this.Paint += (s, e) =>
            {
                using (Pen borderPen = new Pen(Theme.Colors.SurfaceBorder, 1))
                {
                    e.Graphics.DrawRectangle(borderPen, 0, 0, this.Width - 1, this.Height - 1);
                }
            };
        }

        private void CreateButtons(MessageBoxButtons buttons)
        {
            Panel footerPanel = null;
            foreach (Control c in this.Controls)
            {
                if (c.Name == "footerPanel")
                {
                    footerPanel = c as Panel;
                    break;
                }
            }
            if (footerPanel == null) return;

            var buttonList = new List<Button>();

            switch (buttons)
            {
                case MessageBoxButtons.OK:
                    buttonList.Add(CreateDialogButton("OK", DialogResult.OK, Theme.ButtonRole.Accent));
                    break;

                case MessageBoxButtons.OKCancel:
                    buttonList.Add(CreateDialogButton("OK", DialogResult.OK, Theme.ButtonRole.Accent));
                    buttonList.Add(CreateDialogButton("Cancel", DialogResult.Cancel));
                    break;

                case MessageBoxButtons.YesNo:
                    buttonList.Add(CreateDialogButton("Yes", DialogResult.Yes, Theme.ButtonRole.Accent));
                    buttonList.Add(CreateDialogButton("No", DialogResult.No));
                    break;

                case MessageBoxButtons.YesNoCancel:
                    buttonList.Add(CreateDialogButton("Yes", DialogResult.Yes, Theme.ButtonRole.Accent));
                    buttonList.Add(CreateDialogButton("No", DialogResult.No));
                    buttonList.Add(CreateDialogButton("Cancel", DialogResult.Cancel));
                    break;

                case MessageBoxButtons.RetryCancel:
                    buttonList.Add(CreateDialogButton("Retry", DialogResult.Retry, Theme.ButtonRole.Accent));
                    buttonList.Add(CreateDialogButton("Cancel", DialogResult.Cancel));
                    break;

                case MessageBoxButtons.AbortRetryIgnore:
                    buttonList.Add(CreateDialogButton("Abort", DialogResult.Abort));
                    buttonList.Add(CreateDialogButton("Retry", DialogResult.Retry, Theme.ButtonRole.Accent));
                    buttonList.Add(CreateDialogButton("Ignore", DialogResult.Ignore));
                    break;
            }

            var buttonFlow = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.RightToLeft,
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                Padding = new Padding(0, 8, 0, 0),
                WrapContents = false
            };

            for (int i = 0; i < buttonList.Count; i++)
            {
                var button = buttonList[i];
                button.Size = new Size(Theme.Sizes.ButtonWidth, Theme.Sizes.ButtonHeight);
                buttonFlow.Controls.Add(button);
            }

            footerPanel.Controls.Add(buttonFlow);

            if (buttonList.Count > 0)
            {
                this.AcceptButton = buttonList[0];
                this.CancelButton = buttonList[buttonList.Count - 1];
            }

            BringChromeToFront();

            this.Shown += (s, e) => AnimationHelper.FadeIn(this, 200);
        }

        private Button CreateDialogButton(string text, DialogResult result, Theme.ButtonRole role = Theme.ButtonRole.Default)
        {
            var button = Theme.CreateFlatButton(text, role);
            button.DialogResult = result;
            button.Click += (s, e) =>
            {
                this.DialogResult = result;
                this.Close();
            };
            return button;
        }

        private Color GetIconColor(MessageBoxIcon icon)
        {
            switch (icon)
            {
                case MessageBoxIcon.Warning: return IconWarning;
                case MessageBoxIcon.Error: return IconError;
                case MessageBoxIcon.Question: return IconQuestion;
                case MessageBoxIcon.None: return Theme.Colors.SurfaceBorder;
                default: return IconInfo;
            }
        }

        private string GetIconSymbol(MessageBoxIcon icon)
        {
            switch (icon)
            {
                case MessageBoxIcon.Warning: return IconWarningSymbol;
                case MessageBoxIcon.Error: return IconErrorSymbol;
                case MessageBoxIcon.Question: return IconQuestionSymbol;
                case MessageBoxIcon.None: return "";
                default: return IconInfoSymbol;
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                this.DialogResult = DialogResult.Cancel;
                this.Close();
            }
            base.OnKeyDown(e);
        }

        public static DialogResult Show(string message, string title = "Fenceless", MessageBoxButtons buttons = MessageBoxButtons.OK, MessageBoxIcon icon = MessageBoxIcon.Information)
        {
            using (CustomMessageBox msgBox = new CustomMessageBox(message, title, buttons, icon))
            {
                return msgBox.ShowDialog();
            }
        }
    }
}
