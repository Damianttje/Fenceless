using System;
using System.Drawing;
using System.Windows.Forms;

namespace Fenceless.UI
{
    public static class Theme
    {
        #region Color Palette

        public static class Colors
        {
            // Backgrounds
            public static readonly Color BackgroundDark = Color.FromArgb(30, 30, 30);
            public static readonly Color BackgroundMid = Color.FromArgb(40, 40, 40);
            public static readonly Color BackgroundLight = Color.FromArgb(50, 50, 50);
            public static readonly Color Surface = Color.FromArgb(45, 45, 48);
            public static readonly Color SurfaceHover = Color.FromArgb(58, 58, 62);
            public static readonly Color SurfaceSelected = Color.FromArgb(63, 63, 70);
            public static readonly Color SurfaceBorder = Color.FromArgb(67, 67, 70);

            // Text
            public static readonly Color TextPrimary = Color.FromArgb(220, 220, 220);
            public static readonly Color TextSecondary = Color.FromArgb(153, 153, 153);
            public static readonly Color TextDisabled = Color.FromArgb(110, 110, 110);
            public static readonly Color TextBright = Color.FromArgb(255, 255, 255);

            // Accent
            public static readonly Color Accent = Color.FromArgb(0, 122, 204);
            public static readonly Color AccentHover = Color.FromArgb(0, 103, 175);
            public static readonly Color AccentPressed = Color.FromArgb(0, 90, 158);
            public static readonly Color AccentText = Color.FromArgb(255, 255, 255);

            // Buttons
            public static readonly Color ButtonBackground = Color.FromArgb(63, 63, 70);
            public static readonly Color ButtonHover = Color.FromArgb(77, 77, 82);
            public static readonly Color ButtonPressed = Color.FromArgb(89, 89, 95);
            public static readonly Color ButtonBorder = Color.FromArgb(90, 90, 90);
            public static readonly Color ButtonText = Color.FromArgb(220, 220, 220);

            // Inputs
            public static readonly Color InputBackground = Color.FromArgb(40, 40, 40);
            public static readonly Color InputBorder = Color.FromArgb(67, 67, 70);
            public static readonly Color InputBorderFocused = Color.FromArgb(0, 122, 204);
            public static readonly Color InputText = Color.FromArgb(220, 220, 220);

            // Feedback
            public static readonly Color Error = Color.FromArgb(204, 52, 51);
            public static readonly Color ErrorBackground = Color.FromArgb(60, 30, 30);
            public static readonly Color Warning = Color.FromArgb(204, 163, 0);
            public static readonly Color Success = Color.FromArgb(87, 166, 74);
            public static readonly Color Info = Color.FromArgb(0, 122, 204);

            // Title Bar
            public static readonly Color TitleBarBackground = Color.FromArgb(30, 30, 30);
            public static readonly Color TitleBarInactive = Color.FromArgb(40, 40, 40);
            public static readonly Color TitleBarButtonHover = Color.FromArgb(63, 63, 70);
            public static readonly Color TitleBarButtonClose = Color.FromArgb(232, 17, 35);
            public static readonly Color TitleBarButtonCloseHover = Color.FromArgb(255, 50, 50);

            // Sidebar
            public static readonly Color SidebarBackground = Color.FromArgb(37, 37, 38);
            public static readonly Color SidebarItemHover = Color.FromArgb(50, 50, 55);
            public static readonly Color SidebarItemSelected = Color.FromArgb(0, 122, 204);
            public static readonly Color SidebarItemText = Color.FromArgb(180, 180, 180);
            public static readonly Color SidebarItemTextSelected = Color.FromArgb(255, 255, 255);

            // Section
            public static readonly Color SectionBorder = Color.FromArgb(0, 122, 204);
        }

        #endregion

        #region Fonts

        public static class Fonts
        {
            public static readonly Font Title = new Font("Segoe UI", 12F, FontStyle.Bold);
            public static readonly Font TitleBar = new Font("Segoe UI", 9F);
            public static readonly Font Header = new Font("Segoe UI", 11F, FontStyle.Bold);
            public static readonly Font SectionHeader = new Font("Segoe UI", 10F, FontStyle.Bold);
            public static readonly Font Body = new Font("Segoe UI", 9F);
            public static readonly Font BodyBold = new Font("Segoe UI", 9F, FontStyle.Bold);
            public static readonly Font Small = new Font("Segoe UI", 8F);
            public static readonly Font Monospace = new Font("Consolas", 9F);
            public static readonly Font Button = new Font("Segoe UI", 9F);
        }

        #endregion

        #region Sizes

        public static class Sizes
        {
            public const int TitleBarHeight = 32;
            public const int ButtonHeight = 28;
            public const int ButtonWidth = 80;
            public const int InputHeight = 26;
            public const int SidebarWidth = 180;
            public const int SectionSpacing = 16;
            public const int ItemSpacing = 8;
            public const int PanelPadding = 16;
        }

        #endregion

        #region Button Helper

        public enum ButtonRole
        {
            Default,
            Accent,
            Danger
        }

        public static Button CreateFlatButton(string text, ButtonRole role = ButtonRole.Default)
        {
            var btn = new Button
            {
                Text = text,
                Font = Fonts.Button,
                FlatStyle = FlatStyle.Flat,
                UseVisualStyleBackColor = false,
                Height = Sizes.ButtonHeight,
                Cursor = Cursors.Hand
            };

            ApplyButtonStyle(btn, role);
            btn.MouseEnter += (s, e) => ApplyButtonHover(btn, role);
            btn.MouseLeave += (s, e) => ApplyButtonStyle(btn, role);
            btn.MouseDown += (s, e) => ApplyButtonPressed(btn, role);
            btn.MouseUp += (s, e) => ApplyButtonHover(btn, role);

            return btn;
        }

        private static void ApplyButtonStyle(Button btn, ButtonRole role)
        {
            switch (role)
            {
                case ButtonRole.Accent:
                    btn.BackColor = Colors.Accent;
                    btn.ForeColor = Colors.AccentText;
                    btn.FlatAppearance.BorderColor = Colors.AccentHover;
                    break;
                case ButtonRole.Danger:
                    btn.BackColor = Colors.ButtonBackground;
                    btn.ForeColor = Colors.Error;
                    btn.FlatAppearance.BorderColor = Colors.Error;
                    break;
                default:
                    btn.BackColor = Colors.ButtonBackground;
                    btn.ForeColor = Colors.ButtonText;
                    btn.FlatAppearance.BorderColor = Colors.ButtonBorder;
                    break;
            }
            btn.FlatAppearance.BorderSize = 1;
        }

        private static void ApplyButtonHover(Button btn, ButtonRole role)
        {
            switch (role)
            {
                case ButtonRole.Accent:
                    btn.BackColor = Colors.AccentHover;
                    break;
                case ButtonRole.Danger:
                    btn.BackColor = Color.FromArgb(80, 30, 30);
                    break;
                default:
                    btn.BackColor = Colors.ButtonHover;
                    break;
            }
        }

        private static void ApplyButtonPressed(Button btn, ButtonRole role)
        {
            switch (role)
            {
                case ButtonRole.Accent:
                    btn.BackColor = Colors.AccentPressed;
                    break;
                default:
                    btn.BackColor = Colors.ButtonPressed;
                    break;
            }
        }

        #endregion

        #region Label Helpers

        public static Label CreateLabel(string text, Font font = null, Color? foreColor = null)
        {
            return new Label
            {
                Text = text,
                Font = font ?? Fonts.Body,
                ForeColor = foreColor ?? Colors.TextPrimary,
                BackColor = Color.Transparent,
                AutoSize = true
            };
        }

        public static Label CreateSectionHeader(string text)
        {
            return new Label
            {
                Text = text,
                Font = Fonts.SectionHeader,
                ForeColor = Colors.TextBright,
                BackColor = Color.Transparent,
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 4)
            };
        }

        #endregion

        #region Input Helpers

        public static TextBox CreateTextBox()
        {
            return new TextBox
            {
                Font = Fonts.Body,
                BackColor = Colors.InputBackground,
                ForeColor = Colors.InputText,
                BorderStyle = BorderStyle.FixedSingle,
                Height = Sizes.InputHeight
            };
        }

        public static NumericUpDown CreateNumericUpDown(decimal min = 0, decimal max = 100, decimal value = 0)
        {
            return new NumericUpDown
            {
                Minimum = min,
                Maximum = max,
                Value = value,
                Font = Fonts.Body,
                BackColor = Colors.InputBackground,
                ForeColor = Colors.InputText,
                BorderStyle = BorderStyle.FixedSingle
            };
        }

        public static ComboBox CreateComboBox(object[] items = null)
        {
            var cmb = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                FlatStyle = FlatStyle.Flat,
                Font = Fonts.Body,
                BackColor = Colors.InputBackground,
                ForeColor = Colors.InputText
            };
            if (items != null)
                cmb.Items.AddRange(items);
            return cmb;
        }

        public static CheckBox CreateCheckBox(string text)
        {
            return new CheckBox
            {
                Text = text,
                Font = Fonts.Body,
                ForeColor = Colors.TextPrimary,
                BackColor = Color.Transparent,
                AutoSize = true,
                FlatStyle = FlatStyle.Standard
            };
        }

        #endregion

        #region Color Swatch Button

        public static Button CreateColorSwatchButton()
        {
            var btn = new Button
            {
                Size = new Size(120, 26),
                FlatStyle = FlatStyle.Flat,
                UseVisualStyleBackColor = false,
                Font = Fonts.Small,
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 1;
            btn.FlatAppearance.BorderColor = Colors.ButtonBorder;
            return btn;
        }

        public static void UpdateColorSwatch(Button btn, Color color)
        {
            btn.BackColor = color;
            btn.ForeColor = Color.FromArgb(color.R * 0.299 + color.G * 0.587 + color.B * 0.114 > 149
                ? 0 : 255,
                Color.FromArgb(0, 0, 0));
            btn.Text = $"#{color.R:X2}{color.G:X2}{color.B:X2}";
        }

        #endregion

        #region Themed Section Panel

        public static Panel CreateSection(string title, int width)
        {
            var panel = new Panel
            {
                Width = width,
                BackColor = Colors.Surface,
                Padding = new Padding(12, 28, 12, 12),
                Margin = new Padding(0, 0, 0, Sizes.SectionSpacing)
            };

            var accentLine = new Panel
            {
                Height = 2,
                Dock = DockStyle.Top,
                BackColor = Colors.SectionBorder
            };

            var header = new Label
            {
                Text = title,
                Font = Fonts.SectionHeader,
                ForeColor = Colors.TextBright,
                BackColor = Color.Transparent,
                AutoSize = true,
                Location = new Point(12, 8)
            };

            panel.Controls.Add(header);
            panel.Controls.Add(accentLine);

            return panel;
        }

        #endregion

        #region TrackBar + Label pair for transparency

        public static (TrackBar trackBar, Label valueLabel) CreateTransparencySlider(int value = 100)
        {
            var trackBar = new TrackBar
            {
                Minimum = 0,
                Maximum = 100,
                Value = value,
                TickStyle = TickStyle.None,
                Height = 30,
                BackColor = Colors.Surface
            };

            var valueLabel = new Label
            {
                Text = $"{value}%",
                Font = Fonts.Small,
                ForeColor = Colors.TextSecondary,
                AutoSize = true,
                TextAlign = System.Drawing.ContentAlignment.MiddleCenter
            };

            trackBar.ValueChanged += (s, e) => valueLabel.Text = $"{trackBar.Value}%";

            return (trackBar, valueLabel);
        }

        #endregion
    }

    #region CustomTitleBar UserControl

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

    #endregion

    #region HotkeyCaptureBox

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
            var parts = new System.Collections.Generic.List<string>();
            if (ctrl) parts.Add("Ctrl");
            if (alt) parts.Add("Alt");
            if (shift) parts.Add("Shift");
            return string.Join("+", parts) + "+...";
        }

        private string BuildHotkeyString()
        {
            var parts = new System.Collections.Generic.List<string>();
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

    #endregion

    #region ThemedForm Base

    public class ThemedForm : Form
    {
        private CustomTitleBar titleBar;
        private Panel bottomBorder;
        private bool chromeAdded;

        protected CustomTitleBar TitleBar => titleBar;

        public ThemedForm() : base()
        {
        }

        protected void SetupThemedForm(string title, bool showMinimize = true, bool showMaximize = true, bool sizable = true)
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.ControlBox = false;
            this.MaximizeBox = showMaximize;
            this.MinimizeBox = showMinimize;
            this.ShowInTaskbar = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Theme.Colors.BackgroundMid;
            this.Font = Theme.Fonts.Body;

            titleBar = new CustomTitleBar(this, title, showMinimize, showMaximize);

            bottomBorder = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 1,
                BackColor = Theme.Colors.SurfaceBorder
            };
        }

        protected void BringChromeToFront()
        {
            if (!chromeAdded)
            {
                this.Controls.Add(titleBar);
                this.Controls.Add(bottomBorder);
                chromeAdded = true;
            }
        }

        protected override void WndProc(ref Message m)
        {
            const int WM_NCHITTEST = 0x84;
            const int HTCLIENT = 1;

            if (m.Msg == WM_NCHITTEST && WindowState != FormWindowState.Maximized)
            {
                base.WndProc(ref m);
                if ((int)m.Result == HTCLIENT)
                {
                    Point screenPoint = new Point(m.LParam.ToInt32());
                    Point clientPoint = PointToClient(screenPoint);

                    if (titleBar != null && clientPoint.Y < titleBar.Height)
                        return;

                    int resizeBorder = 6;
                    int rightEdge = ClientRectangle.Right;
                    int bottomEdge = ClientRectangle.Bottom;

                    if (clientPoint.Y >= bottomEdge - resizeBorder && clientPoint.X >= rightEdge - resizeBorder)
                        m.Result = (IntPtr)0x11; // HTBOTTOMRIGHT
                    else if (clientPoint.Y >= bottomEdge - resizeBorder && clientPoint.X <= resizeBorder)
                        m.Result = (IntPtr)0x10; // HTBOTTOMLEFT
                    else if (clientPoint.Y <= resizeBorder && clientPoint.X >= rightEdge - resizeBorder)
                        m.Result = (IntPtr)0x0E; // HTTOPRIGHT
                    else if (clientPoint.Y <= resizeBorder && clientPoint.X <= resizeBorder)
                        m.Result = (IntPtr)0x0D; // HTTOPLEFT
                    else if (clientPoint.Y >= bottomEdge - resizeBorder)
                        m.Result = (IntPtr)0x0F; // HTBOTTOM
                    else if (clientPoint.X >= rightEdge - resizeBorder)
                        m.Result = (IntPtr)0x0B; // HTRIGHT
                    else if (clientPoint.X <= resizeBorder)
                        m.Result = (IntPtr)0x0A; // HTLEFT
                }
                return;
            }
            base.WndProc(ref m);
        }
    }

    #endregion

    #region SidebarNavigation UserControl

    public class SidebarNavigation : UserControl
    {
        private struct SidebarItem
        {
            public string Text;
            public string Icon;
            public Panel ItemPanel;
            public Label IconLabel;
            public Label TextLabel;
        }

        private System.Collections.Generic.List<SidebarItem> items = new System.Collections.Generic.List<SidebarItem>();
        private int selectedIndex = 0;
        private Timer hoverTimer;
        private int hoverIndex = -1;
        private Color hoverOriginalColor;

        public event EventHandler<int> PageChanged;

        public SidebarNavigation()
        {
            this.Dock = DockStyle.Left;
            this.Width = Theme.Sizes.SidebarWidth;
            this.BackColor = Theme.Colors.SidebarBackground;
            this.Padding = new Padding(0, 8, 0, 8);

            hoverTimer = new Timer { Interval = 16 };
            hoverTimer.Tick += HoverTimer_Tick;
        }

        public void AddItem(string text, string icon)
        {
            var item = new SidebarItem { Text = text, Icon = icon };

            item.ItemPanel = new Panel
            {
                Height = 36,
                Dock = DockStyle.Top,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 2, 0, 2),
                Cursor = Cursors.Hand
            };

            var accentBar = new Panel
            {
                Width = 3,
                Dock = DockStyle.Left,
                BackColor = Color.Transparent
            };

            item.IconLabel = new Label
            {
                Text = icon,
                Font = new Font("Segoe MDL2 Assets", 12F),
                ForeColor = Theme.Colors.SidebarItemText,
                BackColor = Color.Transparent,
                AutoSize = false,
                Size = new Size(32, 36),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Left,
                Margin = new Padding(0, 0, 0, 0)
            };

            item.TextLabel = new Label
            {
                Text = text,
                Font = Theme.Fonts.Body,
                ForeColor = Theme.Colors.SidebarItemText,
                BackColor = Color.Transparent,
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleLeft,
                Dock = DockStyle.Fill,
                Padding = new Padding(4, 0, 0, 0),
                Margin = new Padding(0)
            };

            item.ItemPanel.Controls.Add(item.TextLabel);
            item.ItemPanel.Controls.Add(item.IconLabel);
            item.ItemPanel.Controls.Add(accentBar);

            int index = items.Count;
            AttachItemHandlers(item.ItemPanel, index);

            items.Add(item);
            this.Controls.Add(item.ItemPanel);

            if (items.Count == 1)
                UpdateSelection();
        }

        private void AttachItemHandlers(Panel itemPanel, int index)
        {
            itemPanel.Click += (s, e) => SelectPage(index);
            itemPanel.MouseEnter += (s, e) => OnItemHover(index);
            itemPanel.MouseLeave += (s, e) => OnItemLeave(index);

            foreach (Control child in itemPanel.Controls)
            {
                child.Click += (s, e) => SelectPage(index);
                child.MouseEnter += (s, e) => OnItemHover(index);
                child.MouseLeave += (s, e) => OnItemLeave(index);
            }
        }

        public void SelectPage(int index)
        {
            if (index < 0 || index >= items.Count) return;
            selectedIndex = index;
            UpdateSelection();
            PageChanged?.Invoke(this, index);
        }

        private void UpdateSelection()
        {
            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                bool isSelected = i == selectedIndex;

                item.ItemPanel.BackColor = isSelected ? Theme.Colors.SurfaceSelected : Color.Transparent;
                item.IconLabel.ForeColor = isSelected ? Theme.Colors.SidebarItemTextSelected : Theme.Colors.SidebarItemText;
                item.TextLabel.ForeColor = isSelected ? Theme.Colors.SidebarItemTextSelected : Theme.Colors.SidebarItemText;

                var accentBar = item.ItemPanel.Controls[0] as Panel;
                if (accentBar != null)
                    accentBar.BackColor = isSelected ? Theme.Colors.SidebarItemSelected : Color.Transparent;
            }
        }

        private void OnItemHover(int index)
        {
            if (index == selectedIndex) return;
            hoverIndex = index;
            hoverOriginalColor = items[index].ItemPanel.BackColor;
            hoverTimer.Start();
        }

        private void OnItemLeave(int index)
        {
            if (hoverIndex == index)
            {
                hoverTimer.Stop();
                if (index != selectedIndex)
                    items[index].ItemPanel.BackColor = Color.Transparent;
                hoverIndex = -1;
            }
        }

        private void HoverTimer_Tick(object sender, EventArgs e)
        {
            if (hoverIndex < 0 || hoverIndex >= items.Count)
            {
                hoverTimer.Stop();
                return;
            }

            var target = Theme.Colors.SidebarItemHover;
            var current = items[hoverIndex].ItemPanel.BackColor;

            int r = Math.Min(current.R + 4, target.R);
            int g = Math.Min(current.G + 4, target.G);
            int b = Math.Min(current.B + 4, target.B);

            items[hoverIndex].ItemPanel.BackColor = Color.FromArgb(r, g, b);

            if (r >= target.R && g >= target.G && b >= target.B)
                hoverTimer.Stop();
        }

        protected override void OnFontChanged(EventArgs e)
        {
            base.OnFontChanged(e);
            foreach (var item in items)
            {
                item.TextLabel.Font = Theme.Fonts.Body;
            }
        }
    }

    #endregion

    #region AnimatedPagePanel UserControl

    public class AnimatedPagePanel : Panel
    {
        private System.Collections.Generic.Dictionary<string, Control> pages = new System.Collections.Generic.Dictionary<string, Control>();
        private string currentPageKey;

        public event EventHandler PageSwitched;

        public void AddPage(string key, Control page)
        {
            page.Dock = DockStyle.Fill;
            page.Visible = false;
            pages[key] = page;
            this.Controls.Add(page);

            if (pages.Count == 1)
            {
                currentPageKey = key;
                page.Visible = true;
            }
        }

        public void SwitchTo(string key)
        {
            if (key == currentPageKey || !pages.ContainsKey(key)) return;

            var oldPage = pages[currentPageKey];
            var newPage = pages[key];

            oldPage.Visible = false;
            newPage.Visible = true;
            currentPageKey = key;

            PageSwitched?.Invoke(this, EventArgs.Empty);
        }

        public Control GetCurrentPage()
        {
            if (currentPageKey != null && pages.ContainsKey(currentPageKey))
                return pages[currentPageKey];
            return null;
        }
    }

    #endregion

    #region ToggleSwitch UserControl

    public class ToggleSwitch : UserControl
    {
        private bool checkedValue;
        private Timer animationTimer;
        private double thumbPosition;
        private double targetThumbPosition;

        public event EventHandler CheckedChanged;

        public bool Checked
        {
            get => checkedValue;
            set
            {
                if (checkedValue != value)
                {
                    checkedValue = value;
                    targetThumbPosition = value ? 1.0 : 0.0;
                    StartAnimation();
                    CheckedChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public ToggleSwitch()
        {
            this.Size = new Size(44, 22);
            this.Cursor = Cursors.Hand;
            this.BackColor = Color.Transparent;

            animationTimer = new Timer { Interval = 16 };
            animationTimer.Tick += AnimationTimer_Tick;

            this.Click += (s, e) => Checked = !Checked;
            this.Paint += ToggleSwitch_Paint;
        }

        private void ToggleSwitch_Paint(object sender, PaintEventArgs e)
        {
            int trackHeight = 22;
            int trackWidth = 44;
            int cornerRadius = trackHeight / 2;

            using (var trackBrush = new SolidBrush(checkedValue ? Theme.Colors.Accent : Theme.Colors.Surface))
            using (var trackBorderPen = new Pen(checkedValue ? Theme.Colors.AccentHover : Theme.Colors.SurfaceBorder))
            using (var thumbBrush = new SolidBrush(Color.White))
            {
                var trackRect = new Rectangle(0, 0, trackWidth, trackHeight);
                using (var trackPath = CreateRoundedRectPath(trackRect, cornerRadius))
                {
                    e.Graphics.FillPath(trackBrush, trackPath);
                    e.Graphics.DrawPath(trackBorderPen, trackPath);
                }

                int thumbDiameter = 16;
                int thumbX = (int)(4 + thumbPosition * (trackWidth - thumbDiameter - 8));
                int thumbY = (trackHeight - thumbDiameter) / 2;

                var thumbRect = new Rectangle(thumbX, thumbY, thumbDiameter, thumbDiameter);
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                e.Graphics.FillEllipse(thumbBrush, thumbRect);
            }
        }

        private void StartAnimation()
        {
            if (!animationTimer.Enabled)
                animationTimer.Start();
        }

        private void AnimationTimer_Tick(object sender, EventArgs e)
        {
            double speed = 0.15;
            double diff = targetThumbPosition - thumbPosition;

            if (Math.Abs(diff) < 0.01)
            {
                thumbPosition = targetThumbPosition;
                animationTimer.Stop();
            }
            else
            {
                thumbPosition += diff * speed;
            }

            this.Invalidate();
        }

        private static System.Drawing.Drawing2D.GraphicsPath CreateRoundedRectPath(Rectangle rect, int radius)
        {
            var path = new System.Drawing.Drawing2D.GraphicsPath();
            int diameter = radius * 2;
            path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
            path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
            path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
        }

        protected override void OnEnabledChanged(EventArgs e)
        {
            base.OnEnabledChanged(e);
            this.Invalidate();
        }
    }

    #endregion
}
