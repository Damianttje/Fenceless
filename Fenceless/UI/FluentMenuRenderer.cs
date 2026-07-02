using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Fenceless.UI
{
    /// <summary>
    /// A dark, Fluent-inspired renderer for ContextMenuStrip / MenuStrip.
    /// Rounded selection highlights, accent check glyphs, themed separators,
    /// and proper padding. Assign via menu.Renderer = new FluentMenuRenderer().
    /// </summary>
    public class FluentMenuRenderer : ToolStripProfessionalRenderer
    {
        private static readonly Color MenuBg = Color.FromArgb(40, 40, 40);
        private static readonly Color MenuBorder = Color.FromArgb(72, 72, 72);
        private static readonly Color ItemHover = Color.FromArgb(56, 56, 56);
        private static readonly Color ItemSelected = Color.FromArgb(60, 60, 60);
        private static readonly Color TextColor = Color.FromArgb(225, 225, 225);
        private static readonly Color TextDisabled = Color.FromArgb(120, 120, 120);
        private static readonly Color SepColor = Color.FromArgb(60, 60, 60);
        private static readonly Color Accent = Color.FromArgb(0, 120, 212);

        public FluentMenuRenderer() : base(new FluentColorTable()) { }

        protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
        {
            // Fill the entire client rectangle flat. The OS clips to the
            // menu's rounded region on Windows 11; filling the full rect
            // avoids the default white background bleeding through the margins.
            using (var b = new SolidBrush(MenuBg))
                e.Graphics.FillRectangle(b, e.ToolStrip.ClientRectangle);
        }

        protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
        {
            // Intentionally no border — the drop shadow + rounded region
            // from the OS provide the chrome. Drawing a border here would
            // sit outside the region and leave a white/stray edge.
        }

        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
        {
            var item = e.Item;
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var bounds = new Rectangle(Point.Empty, item.Size);
            bounds.Inflate(-2, 0);
            bounds = FitIn(bounds, new Rectangle(0, 0, item.Bounds.Width, item.Bounds.Height));

            if (item.Selected || item.Pressed)
            {
                using (var path = RoundRect(bounds, 4))
                using (var b = new SolidBrush(item.Pressed ? ItemSelected : ItemHover))
                    g.FillPath(b, path);
            }
            else
            {
                using (var b = new SolidBrush(MenuBg))
                    g.FillRectangle(b, bounds);
            }

            // Manual check glyph. We disabled the image margin (ShowImageMargin
            // = false) for a flat look, which means the default check rendering
            // never fires. Render the checkmark ourselves so toggles (Locked,
            // Minify, Start with Windows) show their on/off state.
            if (item is ToolStripMenuItem mi && mi.Checked)
            {
                int size = 16;
                var checkRect = new Rectangle(6, bounds.Y + (bounds.Height - size) / 2, size, size);
                DrawCheckGlyph(g, checkRect);
            }
        }

        private void DrawCheckGlyph(Graphics g, Rectangle r)
        {
            using (var p = new Pen(Accent, 2.4F) { StartCap = LineCap.Round, EndCap = LineCap.Round })
            {
                g.DrawLine(p, r.Left + 2, r.Top + 9, r.Left + 7, r.Top + 14);
                g.DrawLine(p, r.Left + 7, r.Top + 14, r.Right - 3, r.Top + 4);
            }
        }

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            var color = e.Item.Enabled ? TextColor : TextDisabled;
            var font = e.TextFont ?? new Font(Theme.TextFontName, 9F);
            var rect = e.TextRectangle;
            // Leave room for the manually-drawn check glyph on checked items.
            if (e.Item is ToolStripMenuItem mi && mi.Checked)
                rect = new Rectangle(rect.X + 24, rect.Y, rect.Width - 24, rect.Height);
            TextRenderer.DrawText(e.Graphics, e.Text, font, rect, color,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
        }

        protected override void OnRenderArrow(ToolStripArrowRenderEventArgs e)
        {
            var color = e.Item.Enabled ? TextColor : TextDisabled;
            using (var p = new Pen(color, 2))
            {
                p.StartCap = LineCap.Round; p.EndCap = LineCap.Round;
                var r = e.ArrowRectangle;
                int cx = r.Left + r.Width / 2 - 1;
                int cy = r.Top + r.Height / 2;
                e.Graphics.DrawLine(p, r.Left + 2, cy - 4, cx, cy + 4);
                e.Graphics.DrawLine(p, cx, cy + 4, r.Right - 4, cy - 4);
            }
        }

        protected override void OnRenderItemCheck(ToolStripItemImageRenderEventArgs e)
        {
            // No-op: we render the check glyph manually in OnRenderMenuItemBackground
            // because the image margin is disabled.
        }

        protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
        {
            var r = e.Item.ContentRectangle;
            int y = r.Top + r.Height / 2;
            using (var p = new Pen(SepColor, 1))
                e.Graphics.DrawLine(p, r.Left + 12, y, r.Right - 4, y);
        }

        protected override void OnRenderImageMargin(ToolStripRenderEventArgs e)
        {
            // No image margin gutter — flat look.
        }

        private static GraphicsPath RoundRect(Rectangle r, int radius)
        {
            var path = new GraphicsPath();
            int d = radius * 2;
            if (d >= r.Width) d = r.Width - 1;
            if (d >= r.Height) d = r.Height - 1;
            if (d < 1) d = 1;
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        private static Rectangle FitIn(Rectangle r, Rectangle container)
        {
            if (r.Right > container.Right) r.Width = container.Right - r.X;
            if (r.Bottom > container.Bottom) r.Height = container.Bottom - r.Y;
            if (r.X < container.X) r.X = container.X;
            if (r.Y < container.Y) r.Y = container.Y;
            return r;
        }

        private class FluentColorTable : ProfessionalColorTable
        {
            public override Color MenuBorder => MenuBg;
            public override Color MenuItemBorder => Color.Transparent;
            public override Color MenuItemSelected => ItemHover;
            public override Color MenuItemSelectedGradientBegin => ItemHover;
            public override Color MenuItemSelectedGradientEnd => ItemHover;
            public override Color MenuItemPressedGradientBegin => ItemSelected;
            public override Color MenuItemPressedGradientEnd => ItemSelected;
            public override Color MenuStripGradientBegin => MenuBg;
            public override Color MenuStripGradientEnd => MenuBg;
            public override Color ToolStripDropDownBackground => MenuBg;
            public override Color ImageMarginGradientBegin => MenuBg;
            public override Color ImageMarginGradientMiddle => MenuBg;
            public override Color ImageMarginGradientEnd => MenuBg;
            public override Color SeparatorDark => SepColor;
            public override Color SeparatorLight => SepColor;
            public override Color CheckBackground => Accent;
            public override Color CheckPressedBackground => Accent;
            public override Color CheckSelectedBackground => Accent;
            public override Color ButtonSelectedHighlight => ItemHover;
            public override Color ButtonSelectedGradientBegin => ItemHover;
            public override Color ButtonSelectedGradientEnd => ItemHover;
        }
    }
}
