using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Fenceless.UI
{
    public static class Theme
    {
        // ---- Capability detection (Segoe Fluent Icons + Segoe UI Variable) ----
        private static readonly bool _hasFluentIcons = FontExists("Segoe Fluent Icons");
        private static readonly bool _hasSegoeVariable = FontExists("Segoe UI Variable");
        private static readonly bool _hasMdl2 = FontExists("Segoe MDL2 Assets");

        public static string IconFontName => _hasFluentIcons ? "Segoe Fluent Icons" : (_hasMdl2 ? "Segoe MDL2 Assets" : "Segoe UI Symbol");
        public static string TextFontName => _hasSegoeVariable ? "Segoe UI Variable" : "Segoe UI";

        private static bool FontExists(string name)
        {
            try
            {
                foreach (var f in FontFamily.Families)
                    if (string.Equals(f.Name, name, StringComparison.OrdinalIgnoreCase))
                        return true;
                return false;
            }
            catch { return false; }
        }

        public static class Colors
        {
            // Backgrounds (Fluent-inspired dark mica tones)
            public static readonly Color BackgroundDark = Color.FromArgb(20, 20, 20);       // mica base
            public static readonly Color BackgroundMid = Color.FromArgb(32, 32, 32);        // layer 1
            public static readonly Color BackgroundLight = Color.FromArgb(44, 44, 44);
            public static readonly Color Surface = Color.FromArgb(43, 43, 43);              // card
            public static readonly Color SurfaceHover = Color.FromArgb(56, 56, 56);
            public static readonly Color SurfaceSelected = Color.FromArgb(60, 60, 60);
            public static readonly Color SurfaceBorder = Color.FromArgb(67, 67, 70);
            public static readonly Color StrokeControl = Color.FromArgb(72, 72, 72);        // Fluent control stroke
            public static readonly Color StrokeControlStrong = Color.FromArgb(90, 90, 90);

            // Text
            public static readonly Color TextPrimary = Color.FromArgb(225, 225, 225);
            public static readonly Color TextSecondary = Color.FromArgb(166, 166, 166);
            public static readonly Color TextDisabled = Color.FromArgb(110, 110, 110);
            public static readonly Color TextBright = Color.FromArgb(255, 255, 255);

            // Accent (Fluent accent blue)
            public static readonly Color Accent = Color.FromArgb(0, 120, 212);
            public static readonly Color AccentHover = Color.FromArgb(0, 103, 192);
            public static readonly Color AccentPressed = Color.FromArgb(0, 90, 170);
            public static readonly Color AccentText = Color.FromArgb(255, 255, 255);
            public static readonly Color AccentSoft = Color.FromArgb(40, 60, 90);

            // Buttons
            public static readonly Color ButtonBackground = Color.FromArgb(60, 60, 60);
            public static readonly Color ButtonHover = Color.FromArgb(72, 72, 72);
            public static readonly Color ButtonPressed = Color.FromArgb(84, 84, 84);
            public static readonly Color ButtonBorder = Color.FromArgb(90, 90, 90);
            public static readonly Color ButtonText = Color.FromArgb(225, 225, 225);

            // Inputs
            public static readonly Color InputBackground = Color.FromArgb(36, 36, 36);
            public static readonly Color InputBorder = Color.FromArgb(72, 72, 72);
            public static readonly Color InputBorderFocused = Color.FromArgb(0, 120, 212);
            public static readonly Color InputText = Color.FromArgb(225, 225, 225);

            // Feedback
            public static readonly Color Error = Color.FromArgb(212, 53, 53);
            public static readonly Color ErrorBackground = Color.FromArgb(60, 30, 30);
            public static readonly Color Warning = Color.FromArgb(210, 167, 0);
            public static readonly Color Success = Color.FromArgb(95, 175, 80);
            public static readonly Color Info = Color.FromArgb(0, 120, 212);

            // Title Bar
            public static readonly Color TitleBarBackground = Color.FromArgb(20, 20, 20);
            public static readonly Color TitleBarInactive = Color.FromArgb(32, 32, 32);
            public static readonly Color TitleBarButtonHover = Color.FromArgb(60, 60, 60);
            public static readonly Color TitleBarButtonClose = Color.FromArgb(232, 17, 35);
            public static readonly Color TitleBarButtonCloseHover = Color.FromArgb(255, 50, 50);

            // Sidebar
            public static readonly Color SidebarBackground = Color.FromArgb(28, 28, 28);
            public static readonly Color SidebarItemHover = Color.FromArgb(48, 48, 48);
            public static readonly Color SidebarItemSelected = Color.FromArgb(0, 120, 212);
            public static readonly Color SidebarItemText = Color.FromArgb(180, 180, 180);
            public static readonly Color SidebarItemTextSelected = Color.FromArgb(255, 255, 255);

            // Section
            public static readonly Color SectionBorder = Color.FromArgb(0, 120, 212);
        }

        public static class Fonts
        {
            private static Font Make(string family, float size, FontStyle style = FontStyle.Regular)
            {
                try { return new Font(family, size, style, GraphicsUnit.Point); }
                catch { return new Font("Segoe UI", size, style, GraphicsUnit.Point); }
            }

            public static readonly Font Title = Make(TextFontName, 13F, FontStyle.Bold);
            public static readonly Font TitleBar = Make(TextFontName, 9F);
            public static readonly Font Header = Make(TextFontName, 12F, FontStyle.Bold);
            public static readonly Font SectionHeader = Make(TextFontName, 10.5F, FontStyle.Bold);
            public static readonly Font Body = Make(TextFontName, 9F);
            public static readonly Font BodyBold = Make(TextFontName, 9F, FontStyle.Bold);
            public static readonly Font Small = Make(TextFontName, 8F);
            public static readonly Font Caption = Make(TextFontName, 8F);   // help text under labels
            public static readonly Font Monospace = Make("Consolas", 9F);
            public static readonly Font Button = Make(TextFontName, 9F);
            public static readonly Font Icon = Make(IconFontName, 12F);
            public static readonly Font IconLarge = Make(IconFontName, 16F);
        }

        public static class Sizes
        {
            public const int TitleBarHeight = 36;
            public const int ButtonHeight = 32;
            public const int ButtonWidth = 92;
            public const int InputHeight = 30;
            public const int SidebarWidth = 200;
            public const int SectionSpacing = 12;
            public const int ItemSpacing = 8;
            public const int PanelPadding = 20;
            public const int ControlRadius = 4;
            public const int ContainerRadius = 8;
            public const int CardRadius = 6;
            public const int LabelColumnWidth = 190;
            public const int RowHeight = 34;
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
                    btn.FlatAppearance.BorderColor = Colors.StrokeControl;
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
            var panel = new RoundedPanel
            {
                Width = width,
                BackColor = Colors.Surface,
                CornerRadius = Sizes.CardRadius,
                Padding = new Padding(16, 28, 16, 12),
                Margin = new Padding(0, 0, 0, Sizes.SectionSpacing)
            };

            var header = new Label
            {
                Text = title,
                Font = Fonts.SectionHeader,
                ForeColor = Colors.TextBright,
                BackColor = Color.Transparent,
                AutoSize = true,
                Location = new Point(16, 8)
            };

            var accentLine = new Panel
            {
                Height = 2,
                Width = 28,
                BackColor = Colors.SectionBorder,
                Location = new Point(16, 30)
            };

            panel.Controls.Add(accentLine);
            panel.Controls.Add(header);

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

        /// <summary>Build a rounded graphics path for a rectangle.</summary>
        public static GraphicsPath CreateRoundedRectPath(Rectangle rect, int radius)
        {
            var path = new GraphicsPath();
            int diameter = radius * 2;
            if (diameter >= rect.Width) diameter = rect.Width - 1;
            if (diameter >= rect.Height) diameter = rect.Height - 1;
            if (diameter < 1) diameter = 1;
            path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
            path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
            path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
        }
    }

    /// <summary>
    /// A Panel with rounded corners and an optional 1px Fluent stroke.
    /// </summary>
    public class RoundedPanel : Panel
    {
        public int CornerRadius { get; set; } = 0;
        public Color BorderColor { get; set; } = Color.FromArgb(72, 72, 72);
        public float BorderWidth { get; set; } = 1F;

        public RoundedPanel()
        {
            this.DoubleBuffered = true;
            this.BackColor = Theme.Colors.Surface;
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (CornerRadius > 0)
                this.Region = CreateRoundedRegion();
            Invalidate();
        }

        private System.Drawing.Region CreateRoundedRegion()
        {
            var r = this.ClientRectangle;
            using (var path = Theme.CreateRoundedRectPath(r, CornerRadius))
                return new System.Drawing.Region(path);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (CornerRadius > 0)
            {
                var g = e.Graphics;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                using (var path = Theme.CreateRoundedRectPath(this.ClientRectangle, CornerRadius))
                {
                    using (var brush = new SolidBrush(this.BackColor))
                        g.FillPath(brush, path);
                    if (BorderWidth > 0)
                    {
                        using (var pen = new Pen(BorderColor, BorderWidth))
                            g.DrawPath(pen, path);
                    }
                }
            }
            else
            {
                base.OnPaint(e);
            }
        }
    }
}
