using System.Drawing;
using Fenceless.Model;
using Fenceless.Util;

namespace Fenceless.UI.Widgets
{
    public sealed class RunningTasksWidgetRenderer : WidgetRendererBase
    {
        protected override int RowHeight => 50;

        protected override void DrawItem(FenceWidgetRenderContext context, FenceWidgetItem item, Rectangle bounds)
        {
            DrawRowBackground(context, item, bounds);

            var iconRect = new Rectangle(bounds.Left + 9, bounds.Top + 9, 32, 32);
            DrawIcon(context, item, iconRect);

            using (var titleFont = new Font(context.BodyFont.FontFamily, 9f, FontStyle.Bold))
            using (var metaFont = new Font(context.BodyFont.FontFamily, 8f, FontStyle.Regular))
            using (var format = new StringFormat { Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap })
            {
                var textLeft = iconRect.Right + 10;
                var badgeWidth = item.IsMinimized ? 74 : 58;
                var textWidth = bounds.Right - textLeft - badgeWidth - 12;
                context.Graphics.DrawString(item.Title, titleFont, GraphicsOptimizer.GetCachedBrush(context.TextColor),
                    new RectangleF(textLeft, bounds.Top + 7, textWidth, 18), format);
                context.Graphics.DrawString(item.Subtitle, metaFont, GraphicsOptimizer.GetCachedBrush(Color.FromArgb(155, context.TextColor)),
                    new RectangleF(textLeft, bounds.Top + 27, textWidth, 15), format);

                var badgeRect = new Rectangle(bounds.Right - badgeWidth - 8, bounds.Top + 15, badgeWidth, 20);
                using (var path = Rounded(badgeRect, 10))
                using (var badgeFormat = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                using (var badgeFont = new Font(context.BodyFont.FontFamily, 7.5f, FontStyle.Bold))
                {
                    var badgeColor = item.IsMinimized ? Color.FromArgb(210, 204, 163, 0) : Color.FromArgb(220, context.AccentColor);
                    context.Graphics.FillPath(GraphicsOptimizer.GetCachedBrush(Color.FromArgb(42, badgeColor)), path);
                    context.Graphics.DrawString(item.Detail, badgeFont, GraphicsOptimizer.GetCachedBrush(badgeColor), badgeRect, badgeFormat);
                }
            }
        }
    }
}
