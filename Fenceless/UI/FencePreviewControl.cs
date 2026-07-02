using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Fenceless.UI
{
    /// <summary>
    /// Renders a miniature live preview of a fence using the current
    /// Appearance-page values. State is supplied via a delegate so the
    /// control stays decoupled from <see cref="SettingsForm"/>.
    /// </summary>
    public class FencePreviewControl : UserControl
    {
        public struct PreviewState
        {
            public Color Background;
            public Color TitleBackground;
            public Color Text;
            public Color Border;
            public float BorderWidth;
            public int CornerRadius;
            public bool ShowShadow;
            public int IconSize;
            public int ItemSpacing;
            public string Title;
        }

        public Func<PreviewState> GetState { get; set; } = () => default;

        public FencePreviewControl()
        {
            this.DoubleBuffered = true;
            this.BackColor = Theme.Colors.BackgroundMid;
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            using (var bgBrush = new SolidBrush(this.BackColor))
                g.FillRectangle(bgBrush, this.ClientRectangle);

            var state = GetState();

            int padding = 16;
            int availableW = Math.Max(50, this.Width - padding * 2);
            int availableH = Math.Max(40, this.Height - padding * 2);

            float scale = Math.Min(1f, Math.Min(availableW / 524f, availableH / 200f));
            int fenceW = (int)(availableW);
            int fenceH = Math.Min(availableH, (int)(120 * scale) + 40);
            int titleH = Math.Max(14, (int)(25 * scale));

            var fenceRect = new Rectangle(padding, padding, fenceW, fenceH);

            if (state.ShowShadow)
            {
                var shadowRect = Rectangle.Inflate(fenceRect, 1, 1);
                shadowRect.Offset(2, 3);
                using (var shadowPath = Theme.CreateRoundedRectPath(shadowRect, Math.Max(2, state.CornerRadius)))
                using (var shadowBrush = new SolidBrush(Color.FromArgb(90, 0, 0, 0)))
                    g.FillPath(shadowBrush, shadowPath);
            }

            int radius = Math.Max(0, Math.Min(state.CornerRadius, fenceH / 2));
            var path = Theme.CreateRoundedRectPath(fenceRect, radius);

            using (var bgBrush = new SolidBrush(state.Background))
                g.FillPath(bgBrush, path);

            var titleRect = new Rectangle(fenceRect.X, fenceRect.Y, fenceRect.Width, titleH);
            var titlePath = BuildTitlePath(titleRect, radius);
            using (var titleBrush = new SolidBrush(state.TitleBackground))
                g.FillPath(titleBrush, titlePath);

            using (var titleFont = new Font(Theme.TextFontName, 9F, FontStyle.Bold))
            using (var textBrush = new SolidBrush(state.Text))
            {
                var sf = new StringFormat { Trimming = StringTrimming.EllipsisCharacter, Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center };
                g.DrawString(state.Title ?? "Fence", titleFont, textBrush,
                    new Rectangle(titleRect.X + 8, titleRect.Y, titleRect.Width - 16, titleRect.Height), sf);
            }

            int iconSize = Math.Max(8, Math.Min(state.IconSize, 32));
            int spacing = Math.Max(4, state.ItemSpacing);
            int gridTop = fenceRect.Y + titleH + 8;
            int gridLeft = fenceRect.X + 8;
            int gx = gridLeft, gy = gridTop;
            using (var iconBrush = new SolidBrush(state.Text))
            {
                for (int i = 0; i < 12; i++)
                {
                    if (gx + iconSize > fenceRect.Right - 8)
                    {
                        gx = gridLeft;
                        gy += iconSize + spacing;
                        if (gy + iconSize > fenceRect.Bottom - 6) break;
                    }
                    var iconRect = new Rectangle(gx, gy, iconSize, iconSize);
                    g.FillRectangle(iconBrush, iconRect);
                    gx += iconSize + spacing;
                }
            }

            if (state.BorderWidth > 0)
            {
                using (var borderPen = new Pen(state.Border, state.BorderWidth))
                    g.DrawPath(borderPen, path);
            }
        }

        private GraphicsPath BuildTitlePath(Rectangle rect, int radius)
        {
            var path = new GraphicsPath();
            int diameter = radius * 2;
            if (diameter > 0)
            {
                path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
                path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
                path.AddLine(rect.Right, rect.Y + radius, rect.Right, rect.Bottom);
                path.AddLine(rect.Right, rect.Bottom, rect.X, rect.Bottom);
                path.AddLine(rect.X, rect.Bottom, rect.X, rect.Y + radius);
            }
            else
            {
                path.AddRectangle(rect);
            }
            path.CloseFigure();
            return path;
        }
    }
}
