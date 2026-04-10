using System;
using System.Drawing;
using System.Windows.Forms;
using Fenceless.Util;

namespace Fenceless.UI
{
    public class TextDialog : ThemedForm
    {
        private TextBox inputTextBox;
        private Button okButton;

        public string InputText => inputTextBox?.Text ?? string.Empty;

        public TextDialog(string title, string prompt, string defaultValue = "", string validationMessage = "Please enter a valid value.")
        {
            defaultValue = defaultValue ?? string.Empty;
            validationMessage = validationMessage ?? "Please enter a valid value.";

            SetupThemedForm(title, showMinimize: false, showMaximize: false, sizable: false);
            this.Size = new Size(420, 200);
            this.StartPosition = FormStartPosition.CenterParent;
            this.MinimumSize = new Size(350, 180);
            this.MaximumSize = new Size(600, 300);

            CreateControls(title, prompt, defaultValue);
            SetupEventHandlers(validationMessage);
        }

        private void CreateControls(string title, string prompt, string defaultValue)
        {
            var contentPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.Colors.BackgroundMid,
                Padding = new Padding(20, 12, 20, 0)
            };

            var titleLabel = Theme.CreateLabel(title, Theme.Fonts.Header, Theme.Colors.TextBright);
            titleLabel.Location = new Point(0, 0);
            titleLabel.AutoSize = true;

            var promptLabel = Theme.CreateLabel(prompt);
            promptLabel.Location = new Point(0, 36);
            promptLabel.AutoSize = true;

            inputTextBox = Theme.CreateTextBox();
            inputTextBox.Text = defaultValue;
            inputTextBox.Location = new Point(0, 60);
            inputTextBox.Width = contentPanel.Width - 40;
            inputTextBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            contentPanel.Controls.Add(inputTextBox);
            contentPanel.Controls.Add(promptLabel);
            contentPanel.Controls.Add(titleLabel);

            var footerPanel = new Panel
            {
                Height = 48,
                Dock = DockStyle.Bottom,
                BackColor = Theme.Colors.BackgroundDark,
                Padding = new Padding(0, 0, 12, 0)
            };

            okButton = Theme.CreateFlatButton("OK", Theme.ButtonRole.Accent);
            okButton.Size = new Size(Theme.Sizes.ButtonWidth, Theme.Sizes.ButtonHeight);
            okButton.DialogResult = DialogResult.OK;

            var btnCancel = Theme.CreateFlatButton("Cancel");
            btnCancel.Size = new Size(Theme.Sizes.ButtonWidth, Theme.Sizes.ButtonHeight);
            btnCancel.DialogResult = DialogResult.Cancel;

            var buttonFlow = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.RightToLeft,
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                Padding = new Padding(0, 8, 0, 0),
                WrapContents = false
            };
            buttonFlow.Controls.Add(btnCancel);
            buttonFlow.Controls.Add(okButton);
            footerPanel.Controls.Add(buttonFlow);

            this.Controls.Add(footerPanel);
            this.Controls.Add(contentPanel);

            BringChromeToFront();

            this.AcceptButton = okButton;
            this.CancelButton = btnCancel;
        }

        private void SetupEventHandlers(string validationMessage)
        {
            okButton.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(inputTextBox.Text))
                {
                    CustomMessageBox.Show(validationMessage, "Invalid Input", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    inputTextBox.Focus();
                    inputTextBox.SelectAll();
                    this.DialogResult = DialogResult.None;
                }
                else
                {
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
            };

            inputTextBox.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter && !e.Shift)
                {
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    okButton.PerformClick();
                }
                else if (e.KeyCode == Keys.Escape)
                {
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    this.DialogResult = DialogResult.Cancel;
                    this.Close();
                }
            };

            this.Shown += (s, e) =>
            {
                inputTextBox.Focus();
                inputTextBox.SelectAll();
                AnimationHelper.FadeIn(this, 200);
            };
        }

        public static string ShowInputDialog(string title, string prompt, string defaultValue = "")
        {
            try
            {
                using (var dialog = new TextDialog(title, prompt, defaultValue))
                {
                    var result = dialog.ShowDialog();
                    return result == DialogResult.OK ? dialog.InputText : string.Empty;
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Error($"Error showing input dialog: {ex.Message}", "TextDialog.ShowDialog");
                return string.Empty;
            }
        }

        public static string ShowInputDialog(IWin32Window owner, string title, string prompt, string defaultValue = "")
        {
            try
            {
                using (var dialog = new TextDialog(title, prompt, defaultValue))
                {
                    var result = dialog.ShowDialog(owner);
                    return result == DialogResult.OK ? dialog.InputText : string.Empty;
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Error($"Error showing input dialog with owner: {ex.Message}", "TextDialog.ShowDialog");
                return string.Empty;
            }
        }

        public static string ShowEditDialog(string title, string currentName = "", string prompt = "Name:")
        {
            try
            {
                using (var dialog = new TextDialog(title, prompt, currentName, "Please enter a valid name."))
                {
                    var result = dialog.ShowDialog();
                    return result == DialogResult.OK ? dialog.InputText : currentName;
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Error($"Error showing edit dialog: {ex.Message}", "TextDialog.ShowEditDialog");
                return currentName;
            }
        }

        public static string ShowEditDialog(IWin32Window owner, string title, string currentName = "", string prompt = "Name:")
        {
            try
            {
                using (var dialog = new TextDialog(title, prompt, currentName, "Please enter a valid name."))
                {
                    var result = dialog.ShowDialog(owner);
                    return result == DialogResult.OK ? dialog.InputText : currentName;
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Error($"Error showing edit dialog with owner: {ex.Message}", "TextDialog.ShowEditDialog");
                return currentName;
            }
        }
    }
}
