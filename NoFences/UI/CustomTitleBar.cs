using System;
using System.Drawing;
using System.Windows.Forms;

namespace Fenceless.UI
{
    public class CustomTitleBar : UserControl
    {
        private Panel leftPanel;
        private Label titleLabel;
        private Button minimizeButton;
        private Button maximizeButton;
        private Button closeButton;
        private bool isDragging;
        private Point dragStart;
        private Form parentForm;
        private bool showMaximize = true;
        private bool showMinimize = true;

        public string Title
        {
            get => titleLabel?.Text ?? "";
            set { if (titleLabel != null) titleLabel.Text = value; }
        }

        public CustomTitleBar(Form parent, string title = "", bool showMin = true, bool showMax = true)
        {
            parentForm = parent;
            showMinimize = showMin;
            showMaximize = showMax;
            Initialize(title);
        }

        private void Initialize(string title)
        {
            this.Dock = DockStyle.Top;
            this.Height = Theme.Sizes.TitleBarHeight;
            this.BackColor = Theme.Colors.TitleBarBackground;

            leftPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent
            };

            titleLabel = new Label
            {
                Text = title,
                Dock = DockStyle.Fill,
                Font = Theme.Fonts.TitleBar,
                ForeColor = Theme.Colors.TextPrimary,
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 0, 0, 0)
            };

            closeButton = CreateTitleButton("\u2715");
            closeButton.Click += (s, e) => parentForm.Close();
            closeButton.MouseEnter += (s, e) => closeButton.BackColor = Theme.Colors.TitleBarButtonCloseHover;
            closeButton.MouseLeave += (s, e) => closeButton.BackColor = Theme.Colors.TitleBarButtonHover;

            maximizeButton = CreateTitleButton("\u25A1");
            maximizeButton.Visible = showMaximize;
            maximizeButton.Click += (s, e) =>
            {
                if (parentForm.WindowState == FormWindowState.Maximized)
                    parentForm.WindowState = FormWindowState.Normal;
                else
                    parentForm.WindowState = FormWindowState.Maximized;
            };

            minimizeButton = CreateTitleButton("\u2500");
            minimizeButton.Visible = showMinimize;
            minimizeButton.Click += (s, e) => parentForm.WindowState = FormWindowState.Minimized;

            // Layout: close | maximize | minimize on the right, title fills the rest
            var buttonPanel = new Panel
            {
                Width = 46 * (1 + (showMaximize ? 1 : 0) + (showMinimize ? 1 : 0)),
                Dock = DockStyle.Right,
                BackColor = Color.Transparent
            };

            if (showMinimize) { minimizeButton.Dock = DockStyle.Right; buttonPanel.Controls.Add(minimizeButton); }
            if (showMaximize) { maximizeButton.Dock = DockStyle.Right; buttonPanel.Controls.Add(maximizeButton); }
            closeButton.Dock = DockStyle.Right;
            buttonPanel.Controls.Add(closeButton);

            leftPanel.Controls.Add(titleLabel);
            this.Controls.Add(buttonPanel);
            this.Controls.Add(leftPanel);

            // Drag handlers
            titleLabel.MouseDown += OnMouseDown;
            titleLabel.MouseMove += OnMouseMove;
            titleLabel.MouseUp += OnMouseUp;
            leftPanel.MouseDown += OnMouseDown;
            leftPanel.MouseMove += OnMouseMove;
            leftPanel.MouseUp += OnMouseUp;

            // Double-click to maximize
            titleLabel.DoubleClick += (s, e) =>
            {
                if (showMaximize)
                {
                    if (parentForm.WindowState == FormWindowState.Maximized)
                        parentForm.WindowState = FormWindowState.Normal;
                    else
                        parentForm.WindowState = FormWindowState.Maximized;
                }
            };

            parentForm.Activated += (s, e) => this.BackColor = Theme.Colors.TitleBarBackground;
            parentForm.Deactivate += (s, e) => this.BackColor = Theme.Colors.TitleBarInactive;
        }

        private Button CreateTitleButton(string text)
        {
            var btn = new Button
            {
                Text = text,
                Width = 46,
                Height = Theme.Sizes.TitleBarHeight,
                FlatStyle = FlatStyle.Flat,
                BackColor = Theme.Colors.TitleBarBackground,
                ForeColor = Theme.Colors.TextPrimary,
                Font = new Font("Segoe UI", 10F),
                Cursor = Cursors.Default
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = Theme.Colors.TitleBarButtonHover;
            btn.FlatAppearance.MouseDownBackColor = Theme.Colors.ButtonPressed;
            return btn;
        }

        private void OnMouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && parentForm.WindowState != FormWindowState.Maximized)
            {
                isDragging = true;
                var ctrl = sender as Control;
                var formOrigin = parentForm.PointToScreen(Point.Empty);
                var mouseScreen = ctrl.PointToScreen(e.Location);
                dragStart = new Point(mouseScreen.X - formOrigin.X, mouseScreen.Y - formOrigin.Y);
            }
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (isDragging)
            {
                var ctrl = sender as Control;
                var mouseScreen = ctrl.PointToScreen(e.Location);
                parentForm.Location = new Point(mouseScreen.X - dragStart.X, mouseScreen.Y - dragStart.Y);
            }
        }

        private void OnMouseUp(object sender, MouseEventArgs e)
        {
            isDragging = false;
        }
    }
}
