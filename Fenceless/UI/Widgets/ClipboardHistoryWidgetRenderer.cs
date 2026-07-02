using System.Drawing;
using Fenceless.Model;
using Fenceless.Util;

namespace Fenceless.UI.Widgets
{
    public sealed class ClipboardHistoryWidgetRenderer : WidgetRendererBase
    {
        protected override int RowHeight => 66;

        protected override void DrawItem(FenceWidgetRenderContext context, FenceWidgetItem item, Rectangle bounds)
        {
            DrawRowBackground(context, item, bounds);

            var previewRect = new Rectangle(bounds.Left + 9, bounds.Top + 9, 48, 48);
            if (item.Kind == FenceEntryKind.ClipboardImage && item.PreviewImage != null && context.FenceInfo.ShowPreviews)
            {
                DrawImagePreview(context, item.PreviewImage, previewRect);
            }
            else
            {
                DrawIcon(context, item, new Rectangle(previewRect.Left + 8, previewRect.Top + 8, 32, 32));
            }

            using (var titleFont = new Font(context.BodyFont.FontFamily, 9f, FontStyle.Bold))
            using (var metaFont = new Font(context.BodyFont.FontFamily, 8f, FontStyle.Regular))
            using (var format = new StringFormat { Trimming = StringTrimming.EllipsisCharacter })
            {
                var textLeft = previewRect.Right + 10;
                var textWidth = bounds.Right - textLeft - 8;
                var title = item.Kind == FenceEntryKind.ClipboardImage ? "Image copied to clipboard" : item.Title;
                context.Graphics.DrawString(title, titleFont, GraphicsOptimizer.GetCachedBrush(context.TextColor),
                    new RectangleF(textLeft, bounds.Top + 8, textWidth, 20), format);
                context.Graphics.DrawString(item.Subtitle, metaFont, GraphicsOptimizer.GetCachedBrush(Color.FromArgb(160, context.TextColor)),
                    new RectangleF(textLeft, bounds.Top + 31, textWidth, 14), format);
                context.Graphics.DrawString(item.Detail, metaFont, GraphicsOptimizer.GetCachedBrush(Color.FromArgb(190, context.AccentColor)),
                    new RectangleF(textLeft, bounds.Top + 47, textWidth, 13), format);
            }
        }

        private static void DrawImagePreview(FenceWidgetRenderContext context, Image image, Rectangle bounds)
        {
            using (var path = Rounded(bounds, 5))
            {
                context.Graphics.FillPath(GraphicsOptimizer.GetCachedBrush(Color.FromArgb(26, context.TextColor)), path);
                var state = context.Graphics.Save();
                context.Graphics.SetClip(path);

                var scale = System.Math.Min(bounds.Width / (double)image.Width, bounds.Height / (double)image.Height);
                var width = System.Math.Max(1, (int)System.Math.Round(image.Width * scale));
                var height = System.Math.Max(1, (int)System.Math.Round(image.Height * scale));
                var x = bounds.Left + (bounds.Width - width) / 2;
                var y = bounds.Top + (bounds.Height - height) / 2;
                context.Graphics.DrawImage(image, new Rectangle(x, y, width, height));
                context.Graphics.Restore(state);

                context.Graphics.DrawPath(GraphicsOptimizer.GetCachedPen(Color.FromArgb(120, context.AccentColor)), path);
            }
        }
    }
}
