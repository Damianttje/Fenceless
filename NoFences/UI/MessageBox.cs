using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Fenceless.Win32;

namespace Fenceless.UI
{
    public class CustomMessageBox : Form
    {
        private Label messageLabel;
        private Panel buttonPanel;
        private Panel headerPanel;
        private Panel contentPanel;
        private Button closeButton;
        private bool isDragging = false;
        private Point lastCursor;
        private Point lastForm;

        public CustomMessageBox(string message, string title = "Fenceless | Message", MessageBoxButtons buttons = MessageBoxButtons.OK, MessageBoxIcon icon = MessageBoxIcon.Information)
        {
            InitializeComponent();
            SetupForm(title);
            SetupHeader(title);
            SetupContent(message, icon);
            SetupButtons(buttons);
            SetupDragging();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            // Form properties
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.None;
            this.ClientSize = new Size(400, 200);
            this.ShowInTaskbar = false;
            this.TopMost = true;
            this.Font = new Font("Segoe UI", 9F);
            this.Padding = new Padding(1);
            this.BackColor = Color.FromArgb(45, 45, 48);
            this.MinimumSize = new Size(300, 150);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
        }

        private void SetupForm(string title)
        {
            this.Text = title;

            // Add a border effect
            this.Paint += (s, e) =>
            {
                using (Pen borderPen = new Pen(Color.FromArgb(85, 85, 85), 1))
                {
                    e.Graphics.DrawRectangle(borderPen, 0, 0, this.Width - 1, this.Height - 1);
                }
            };
        }

        private void SetupHeader(string title)
        {
            // Create header panel
            headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 35,
                BackColor = Color.FromArgb(45, 45, 48)
            };

            // Create title label
            var titleLabel = new Label
            {
                Text = title,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 0, 0, 0),
                BackColor = Color.Transparent
            };

            // Create close button
            closeButton = new Button
            {
                Text = "✕",
                Size = new Size(35, 35),
                Dock = DockStyle.Right,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                BackColor = Color.FromArgb(45, 45, 48),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            closeButton.FlatAppearance.BorderSize = 0;
            closeButton.Click += CloseButton_Click;
            closeButton.MouseEnter += (s, e) => closeButton.BackColor = Color.FromArgb(232, 17, 35);
            closeButton.MouseLeave += (s, e) => closeButton.BackColor = Color.FromArgb(45, 45, 48);

            headerPanel.Controls.Add(titleLabel);
            headerPanel.Controls.Add(closeButton);
            this.Controls.Add(headerPanel);
        }

        private void SetupContent(string message, MessageBoxIcon icon)
        {
            // Create content panel
            contentPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(60, 63, 65),
                Padding = new Padding(20)
            };

            // Create message label
            messageLabel = new Label
            {
                Text = message,
                ForeColor = Color.FromArgb(220, 220, 220),
                Font = new Font("Segoe UI", 9F),
                AutoSize = false,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = Color.Transparent
            };

            // Handle text wrapping
            messageLabel.Paint += (s, e) =>
            {
                var label = s as Label;
                if (label != null)
                {
                    var rect = new Rectangle(0, 0, label.Width, label.Height);
                    var format = new StringFormat
                    {
                        Alignment = StringAlignment.Near,
                        LineAlignment = StringAlignment.Center,
                        FormatFlags = StringFormatFlags.LineLimit
                    };

                    e.Graphics.DrawString(label.Text, label.Font, new SolidBrush(label.ForeColor), rect, format);
                }
            };

            contentPanel.Controls.Add(messageLabel);
            this.Controls.Add(contentPanel);
        }

        private void SetupButtons(MessageBoxButtons buttons)
        {
            // Create button panel
            buttonPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 50,
                BackColor = Color.FromArgb(60, 63, 65),
                Padding = new Padding(10)
            };

            var buttonList = new List<Button>();
            int buttonWidth = 80;
            int buttonHeight = 30;
            int buttonSpacing = 10;

            switch (buttons)
            {
                case MessageBoxButtons.OK:
                    var okButton = CreateButton("OK", DialogResult.OK, buttonWidth, buttonHeight);
                    buttonList.Add(okButton);
                    this.AcceptButton = okButton;
                    break;

                case MessageBoxButtons.OKCancel:
                    var okBtn = CreateButton("OK", DialogResult.OK, buttonWidth, buttonHeight);
                    var cancelBtn = CreateButton("Cancel", DialogResult.Cancel, buttonWidth, buttonHeight);
                    buttonList.AddRange(new[] { cancelBtn, okBtn });
                    this.AcceptButton = okBtn;
                    this.CancelButton = cancelBtn;
                    break;

                case MessageBoxButtons.YesNo:
                    var yesBtn = CreateButton("Yes", DialogResult.Yes, buttonWidth, buttonHeight);
                    var noBtn = CreateButton("No", DialogResult.No, buttonWidth, buttonHeight);
                    buttonList.AddRange(new[] { noBtn, yesBtn });
                    this.AcceptButton = yesBtn;
                    this.CancelButton = noBtn;
                    break;

                case MessageBoxButtons.YesNoCancel:
                    var yesBtnCancel = CreateButton("Yes", DialogResult.Yes, buttonWidth, buttonHeight);
                    var noBtnCancel = CreateButton("No", DialogResult.No, buttonWidth, buttonHeight);
                    var cancelBtnYesNo = CreateButton("Cancel", DialogResult.Cancel, buttonWidth, buttonHeight);
                    buttonList.AddRange(new[] { cancelBtnYesNo, noBtnCancel, yesBtnCancel });
                    this.AcceptButton = yesBtnCancel;
                    this.CancelButton = cancelBtnYesNo;
                    break;

                case MessageBoxButtons.RetryCancel:
                    var retryBtn = CreateButton("Retry", DialogResult.Retry, buttonWidth, buttonHeight);
                    var retryCancelBtn = CreateButton("Cancel", DialogResult.Cancel, buttonWidth, buttonHeight);
                    buttonList.AddRange(new[] { retryCancelBtn, retryBtn });
                    this.AcceptButton = retryBtn;
                    this.CancelButton = retryCancelBtn;
                    break;

                case MessageBoxButtons.AbortRetryIgnore:
                    var abortBtn = CreateButton("Abort", DialogResult.Abort, buttonWidth, buttonHeight);
                    var retryBtnIgnore = CreateButton("Retry", DialogResult.Retry, buttonWidth, buttonHeight);
                    var ignoreBtn = CreateButton("Ignore", DialogResult.Ignore, buttonWidth, buttonHeight);
                    buttonList.AddRange(new[] { ignoreBtn, retryBtnIgnore, abortBtn });
                    this.AcceptButton = retryBtnIgnore;
                    this.CancelButton = abortBtn;
                    break;
            }

            // Position buttons from right to left
            int totalWidth = (buttonList.Count * buttonWidth) + ((buttonList.Count - 1) * buttonSpacing);
            int startX = buttonPanel.Width - totalWidth - 10;

            for (int i = 0; i < buttonList.Count; i++)
            {
                var button = buttonList[i];
                button.Location = new Point(startX + (i * (buttonWidth + buttonSpacing)), (buttonPanel.Height - buttonHeight) / 2);
                buttonPanel.Controls.Add(button);
            }

            this.Controls.Add(buttonPanel);
            this.ResumeLayout(false);
        }

        private Button CreateButton(string text, DialogResult result, int width, int height)
        {
            var button = new Button
            {
                Text = text,
                Size = new Size(width, height),
                DialogResult = result,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(70, 73, 75),
                ForeColor = Color.FromArgb(220, 220, 220)
            };
            button.FlatAppearance.BorderSize = 1;
            button.FlatAppearance.BorderColor = Color.FromArgb(100, 100, 100);

            button.Click += (s, e) =>
            {
                this.DialogResult = result;
                this.Close();
            };

            return button;
        }

        private void SetupDragging()
        {
            // Make header draggable
            headerPanel.MouseDown += HeaderPanel_MouseDown;
            headerPanel.MouseMove += HeaderPanel_MouseMove;
            headerPanel.MouseUp += HeaderPanel_MouseUp;

            // Also make the title label draggable
            var titleLabel = headerPanel.Controls.OfType<Label>().FirstOrDefault();
            if (titleLabel != null)
            {
                titleLabel.MouseDown += HeaderPanel_MouseDown;
                titleLabel.MouseMove += HeaderPanel_MouseMove;
                titleLabel.MouseUp += HeaderPanel_MouseUp;
            }
        }

        private void HeaderPanel_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                isDragging = true;
                lastCursor = Cursor.Position;
                lastForm = this.Location;
            }
        }

        private void HeaderPanel_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDragging)
            {
                Point currentCursor = Cursor.Position;
                Point delta = new Point(currentCursor.X - lastCursor.X, currentCursor.Y - lastCursor.Y);
                this.Location = new Point(lastForm.X + delta.X, lastForm.Y + delta.Y);
            }
        }

        private void HeaderPanel_MouseUp(object sender, MouseEventArgs e)
        {
            isDragging = false;
        }

        private void CloseButton_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
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

        public static DialogResult Show(string message, string title = "Fenceless | Message", MessageBoxButtons buttons = MessageBoxButtons.OK, MessageBoxIcon icon = MessageBoxIcon.Information)
        {
            using (CustomMessageBox msgBox = new CustomMessageBox(message, title, buttons, icon))
            {
                return msgBox.ShowDialog();
            }
        }
    }
}