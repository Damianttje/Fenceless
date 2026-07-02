using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Fenceless.UI
{
    /// <summary>
    /// A color swatch button that paints a checkerboard behind the color
    /// (so transparency is visually hinted) and shows the hex code.
    /// Extends <see cref="Button"/> so it stays compatible with existing
    /// code that reads <see cref="Control.BackColor"/>.
    /// </summary>
    public class ColorPickerButton : Button
    {
        private int _alphaPercent = 100;

        /// <summary>Optional delegate returning the current opacity 0-100.
        /// When set, the swatch reads alpha live from this source on each paint.</summary>
        public Func<int> AlphaSource { get; set; }

        public int AlphaPercent
        {
            get => AlphaSource?.Invoke() ?? _alphaPercent;
            set { _alphaPercent = Math.Max(0, Math.Min(100, value)); Invalidate(); }
        }

        public ColorPickerButton()
        {
            this.FlatStyle = FlatStyle.Flat;
            this.UseVisualStyleBackColor = false;
            this.Font = Theme.Fonts.Small;
            this.Cursor = Cursors.Hand;
            this.Size = new Size(120, 28);
            this.FlatAppearance.BorderSize = 1;
            this.FlatAppearance.BorderColor = Theme.Colors.StrokeControl;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var rect = this.ClientRectangle;

            Color c = this.BackColor;
            int a = (int)Math.Round(255m * (AlphaPercent / 100m));

            using (var path = Theme.CreateRoundedRectPath(rect, Theme.Sizes.ControlRadius))
            {
                using (var bgBrush = new SolidBrush(Theme.Colors.InputBackground))
                    g.FillPath(bgBrush, path);

                DrawCheckerboard(g, rect);

                using (var colorBrush = new SolidBrush(Color.FromArgb(a, c.R, c.G, c.B)))
                    g.FillPath(colorBrush, path);

                using (var pen = new Pen(Theme.Colors.StrokeControl, 1F))
                    g.DrawPath(pen, path);
            }

            // Hex label
            bool dark = (c.R * 0.299 + c.G * 0.587 + c.B * 0.114) * (a / 255.0) > 110;
            using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
            using (var textBrush = new SolidBrush(dark ? Color.Black : Color.White))
            {
                g.DrawString($"#{c.R:X2}{c.G:X2}{c.B:X2}", this.Font, textBrush, rect, sf);
            }
        }

        private void DrawCheckerboard(Graphics g, Rectangle rect)
        {
            int cell = 6;
            var light = Theme.Colors.SurfaceHover;
            var dark = Theme.Colors.BackgroundDark;
            for (int y = 0; y < rect.Height; y += cell)
            {
                for (int x = 0; x < rect.Width; x += cell)
                {
                    bool odd = ((x / cell) + (y / cell)) % 2 == 0;
                    using (var b = new SolidBrush(odd ? light : dark))
                        g.FillRectangle(b, x, y, cell, cell);
                }
            }
        }
    }
}
