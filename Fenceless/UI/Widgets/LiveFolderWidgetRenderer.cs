using System.Drawing;
using System.Drawing.Drawing2D;
using Fenceless.Model;
using Fenceless.Util;

namespace Fenceless.UI.Widgets
{
    public sealed class LiveFolderWidgetRenderer : WidgetRendererBase
    {
        protected override int RowHeight => 56;

        protected override void DrawItem(FenceWidgetRenderContext context, FenceWidgetItem item, Rectangle bounds)
        {
            DrawRowBackground(context, item, bounds);

            var iconRect = new Rectangle(bounds.Left + 9, bounds.Top + 10, 34, 34);
            DrawIcon(context, item, iconRect);

            using (var titleFont = new Font(context.BodyFont.FontFamily, 9f, FontStyle.Bold))
            using (var metaFont = new Font(context.BodyFont.FontFamily, 8f, FontStyle.Regular))
            using (var format = new StringFormat { Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap })
            {
                var textLeft = iconRect.Right + 10;
                var textWidth = bounds.Right - textLeft - 8;
                context.Graphics.DrawString(item.Title, titleFont, GraphicsOptimizer.GetCachedBrush(context.TextColor),
                    new RectangleF(textLeft, bounds.Top + 8, textWidth, 18), format);
                context.Graphics.DrawString(TruncateMiddle(item.Path, 72), metaFont, GraphicsOptimizer.GetCachedBrush(Color.FromArgb(150, context.TextColor)),
                    new RectangleF(textLeft, bounds.Top + 27, textWidth, 14), format);
                context.Graphics.DrawString(item.Detail, metaFont, GraphicsOptimizer.GetCachedBrush(Color.FromArgb(190, context.AccentColor)),
                    new RectangleF(textLeft, bounds.Top + 41, textWidth, 12), format);
            }

            var tagRect = new Rectangle(bounds.Right - 58, bounds.Top + 9, 48, 18);
            using (var path = Rounded(tagRect, 9))
            using (var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter })
            using (var tagFont = new Font(context.BodyFont.FontFamily, 7.5f, FontStyle.Bold))
            {
                context.Graphics.FillPath(GraphicsOptimizer.GetCachedBrush(Color.FromArgb(48, context.AccentColor)), path);
                context.Graphics.DrawString(item.Subtitle, tagFont, GraphicsOptimizer.GetCachedBrush(Color.FromArgb(220, context.AccentColor)), tagRect, format);
            }
        }
    }
}
