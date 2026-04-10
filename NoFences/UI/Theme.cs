using System;
using System.Drawing;
using System.Windows.Forms;

namespace Fenceless.UI
{
    public static class Theme
    {
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
    }
}
