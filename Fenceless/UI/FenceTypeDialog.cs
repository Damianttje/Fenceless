using System;
using System.Drawing;
using System.Windows.Forms;
using Fenceless.Model;

namespace Fenceless.UI
{
    public sealed class FenceTypeDialog : ThemedForm
    {
        private ComboBox typeComboBox;

        public FenceType SelectedFenceType
        {
            get
            {
                return typeComboBox.SelectedIndex >= 0
                    ? (FenceType)typeComboBox.SelectedIndex
                    : FenceType.Standard;
            }
        }

        public FenceTypeDialog()
        {
            SetupThemedForm("Fence Type", showMinimize: false, showMaximize: false, sizable: false);
            Size = new Size(440, 220);
            MinimumSize = Size;
            MaximumSize = Size;
            StartPosition = FormStartPosition.CenterParent;
            CreateControls();
        }

        private void CreateControls()
        {
            var contentPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.Colors.BackgroundMid,
                Padding = new Padding(24, 16, 24, 8)
            };

            var label = Theme.CreateLabel("Choose the kind of fence to create.", Theme.Fonts.Body, Theme.Colors.TextPrimary);
            label.AutoSize = false;
            label.Dock = DockStyle.Top;
            label.Height = 28;

            typeComboBox = Theme.CreateComboBox(new[] { "Standard", "Live Folder", "Running Tasks", "Clipboard History" });
            typeComboBox.Dock = DockStyle.Top;
            typeComboBox.SelectedIndex = 0;

            contentPanel.Controls.Add(typeComboBox);
            contentPanel.Controls.Add(label);

            var footerPanel = new Panel
            {
                Height = 52,
                Dock = DockStyle.Bottom,
                BackColor = Theme.Colors.BackgroundDark,
                Padding = new Padding(16, 8, 12, 8)
            };

            var okButton = Theme.CreateFlatButton("OK", Theme.ButtonRole.Accent);
            okButton.Size = new Size(Theme.Sizes.ButtonWidth, Theme.Sizes.ButtonHeight);
            okButton.DialogResult = DialogResult.OK;

            var cancelButton = Theme.CreateFlatButton("Cancel");
            cancelButton.Size = new Size(Theme.Sizes.ButtonWidth, Theme.Sizes.ButtonHeight);
            cancelButton.DialogResult = DialogResult.Cancel;

            var buttonFlow = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.RightToLeft,
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                WrapContents = false
            };
            buttonFlow.Controls.Add(cancelButton);
            buttonFlow.Controls.Add(okButton);
            footerPanel.Controls.Add(buttonFlow);

            Controls.Add(footerPanel);
            Controls.Add(contentPanel);
            BringChromeToFront();

            AcceptButton = okButton;
            CancelButton = cancelButton;
        }
    }
}
