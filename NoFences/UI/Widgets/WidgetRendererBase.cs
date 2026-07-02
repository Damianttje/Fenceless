using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Fenceless.Model;
using Fenceless.Util;
using Fenceless.Win32;

namespace Fenceless.UI.Widgets
{
    public abstract class WidgetRendererBase : IFenceWidgetRenderer
    {
        protected const int Padding = 10;
        protected const int HeaderHeight = 48;
        protected const int RowGap = 6;

        public FenceWidgetRenderResult Render(FenceWidgetRenderContext context)
        {
            var graphics = context.Graphics;
            var body = GetBodyBounds(context);
            var contentHeight = MeasureContentHeight(context);
            var maxScrollOffset = Math.Max(0, contentHeight - body.Height);

            DrawWidgetHeader(context, body);

            var state = graphics.Save();
            graphics.SetClip(body);

            if (context.Snapshot.Items.Count == 0)
            {
                DrawEmptyState(context, body);
            }
            else
            {
                for (var i = 0; i < context.Snapshot.Items.Count; i++)
                {
                    var rowBounds = GetItemBounds(context, i);
                    if (rowBounds.Bottom < body.Top || rowBounds.Top > body.Bottom)
                        continue;

                    DrawItem(context, context.Snapshot.Items[i], rowBounds);
                }
            }

            graphics.Restore(state);
            return new FenceWidgetRenderResult(maxScrollOffset);
        }

        public string HitTest(FenceWidgetRenderContext context, Point point)
        {
            if (!GetBodyBounds(context).Contains(point))
                return null;

            for (var i = 0; i < context.Snapshot.Items.Count; i++)
            {
                if (GetItemBounds(context, i).Contains(point))
                    return context.Snapshot.Items[i].LegacyValue;
            }

            return null;
        }

        protected abstract void DrawItem(FenceWidgetRenderContext context, FenceWidgetItem item, Rectangle bounds);

        protected virtual int RowHeight => 52;

        protected Rectangle GetBodyBounds(FenceWidgetRenderContext context)
        {
            return new Rectangle(
                context.Bounds.Left,
                context.Bounds.Top + context.TitleHeight,
                context.Bounds.Width,
                Math.Max(0, context.Bounds.Height - context.TitleHeight));
        }

        protected Rectangle GetItemBounds(FenceWidgetRenderContext context, int index)
        {
            var y = context.TitleHeight + HeaderHeight + Padding - context.ScrollOffset + index * (RowHeight + RowGap);
            return new Rectangle(Padding, y, Math.Max(0, context.Bounds.Width - Padding * 2), RowHeight);
        }

        protected int MeasureContentHeight(FenceWidgetRenderContext context)
        {
            if (context.Snapshot.Items.Count == 0)
                return HeaderHeight + 100;

            return HeaderHeight + Padding + context.Snapshot.Items.Count * (RowHeight + RowGap) + Padding;
        }

        protected virtual void DrawWidgetHeader(FenceWidgetRenderContext context, Rectangle body)
        {
            var graphics = context.Graphics;
            var headerRect = new Rectangle(Padding, body.Top + 6, Math.Max(0, body.Width - Padding * 2), HeaderHeight - 10);
            var accent = context.AccentColor;

            using (var path = Rounded(headerRect, 7))
            {
                graphics.FillPath(GraphicsOptimizer.GetCachedBrush(Color.FromArgb(38, accent)), path);
            }

            graphics.FillRectangle(GraphicsOptimizer.GetCachedBrush(Color.FromArgb(170, accent)), headerRect.Left, headerRect.Top, 3, headerRect.Height);

            using (var statusFont = new Font(context.BodyFont.FontFamily, 8f, FontStyle.Bold))
            using (var subtitleFont = new Font(context.BodyFont.FontFamily, 8f, FontStyle.Regular))
            using (var trimFormat = new StringFormat { Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap })
            {
                var titleRect = new RectangleF(headerRect.Left + 10, headerRect.Top + 5, headerRect.Width - 110, 16);
                var subtitleRect = new RectangleF(headerRect.Left + 10, headerRect.Top + 22, headerRect.Width - 20, 14);
                var statusRect = new RectangleF(headerRect.Right - 96, headerRect.Top + 6, 86, 16);

                graphics.DrawString(context.Snapshot.Title, statusFont, GraphicsOptimizer.GetCachedBrush(context.TextColor), titleRect, trimFormat);
                graphics.DrawString(context.Snapshot.Subtitle, subtitleFont, GraphicsOptimizer.GetCachedBrush(Color.FromArgb(180, context.TextColor)), subtitleRect, trimFormat);
                graphics.DrawString(context.Snapshot.Status, subtitleFont, GraphicsOptimizer.GetCachedBrush(Color.FromArgb(210, accent)), statusRect, trimFormat);
            }
        }

        protected void DrawEmptyState(FenceWidgetRenderContext context, Rectangle body)
        {
            using (var font = new Font(context.BodyFont.FontFamily, 9f, FontStyle.Regular))
            using (var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter })
            {
                var rect = new RectangleF(body.Left + 18, body.Top + HeaderHeight + 12, body.Width - 36, Math.Max(40, body.Height - HeaderHeight - 20));
                var message = string.IsNullOrEmpty(context.Snapshot.Status) ? "No items" : context.Snapshot.Status;
                context.Graphics.DrawString(message, font, GraphicsOptimizer.GetCachedBrush(Color.FromArgb(170, context.TextColor)), rect, format);
            }
        }

        protected void DrawRowBackground(FenceWidgetRenderContext context, FenceWidgetItem item, Rectangle bounds)
        {
            var selected = item.LegacyValue == context.SelectedItem;
            var hovered = item.LegacyValue == context.HoveringItem;
            var alpha = selected ? 80 : hovered ? 52 : 28;
            var borderAlpha = selected ? 170 : hovered ? 120 : 42;

            using (var path = Rounded(bounds, 6))
            {
                context.Graphics.FillPath(GraphicsOptimizer.GetCachedBrush(Color.FromArgb(alpha, context.AccentColor)), path);
                context.Graphics.DrawPath(GraphicsOptimizer.GetCachedPen(Color.FromArgb(borderAlpha, context.AccentColor), selected ? 1.5f : 1f), path);
            }
        }

        protected void DrawIcon(FenceWidgetRenderContext context, FenceWidgetItem item, Rectangle bounds)
        {
            Bitmap bitmap = null;
            if (!string.IsNullOrEmpty(item.IconPath))
                bitmap = context.IconCache.GetIcon(item.IconPath, Math.Min(bounds.Width, bounds.Height));

            if (bitmap != null)
            {
                context.Graphics.DrawImage(bitmap, bounds);
                return;
            }

            var icon = item.Kind == FenceEntryKind.Folder
                ? IconUtil.FolderLarge
                : item.Kind == FenceEntryKind.ClipboardText || item.Kind == FenceEntryKind.ClipboardImage
                    ? SystemIcons.Information
                    : SystemIcons.Application;
            context.Graphics.DrawIcon(icon, bounds);
        }

        protected static GraphicsPath Rounded(Rectangle rect, int radius)
        {
            var path = new GraphicsPath();
            var diameter = radius * 2;
            var arc = new Rectangle(rect.Location, new Size(diameter, diameter));
            path.AddArc(arc, 180, 90);
            arc.X = rect.Right - diameter;
            path.AddArc(arc, 270, 90);
            arc.Y = rect.Bottom - diameter;
            path.AddArc(arc, 0, 90);
            arc.X = rect.Left;
            path.AddArc(arc, 90, 90);
            path.CloseFigure();
            return path;
        }

        protected static string TruncateMiddle(string value, int maxChars)
        {
            if (string.IsNullOrEmpty(value) || value.Length <= maxChars)
                return value ?? string.Empty;

            var head = Math.Max(1, maxChars / 2 - 1);
            var tail = Math.Max(1, maxChars - head - 1);
            return value.Substring(0, head) + "\u2026" + value.Substring(value.Length - tail);
        }
    }
}
