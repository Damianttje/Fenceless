using System.Drawing;
using Fenceless.Model;
using Fenceless.Util;

namespace Fenceless.UI.Widgets
{
    public sealed class FenceWidgetRenderContext
    {
        public FenceWidgetRenderContext(
            Graphics graphics,
            Rectangle bounds,
            FenceInfo fenceInfo,
            FenceWidgetSnapshot snapshot,
            int titleHeight,
            int scrollOffset,
            string selectedItem,
            string hoveringItem,
            Font titleFont,
            Font bodyFont,
            IconCache iconCache,
            Color textColor,
            Color accentColor)
        {
            Graphics = graphics;
            Bounds = bounds;
            FenceInfo = fenceInfo;
            Snapshot = snapshot;
            TitleHeight = titleHeight;
            ScrollOffset = scrollOffset;
            SelectedItem = selectedItem;
            HoveringItem = hoveringItem;
            TitleFont = titleFont;
            BodyFont = bodyFont;
            IconCache = iconCache;
            TextColor = textColor;
            AccentColor = accentColor;
        }

        public Graphics Graphics { get; }
        public Rectangle Bounds { get; }
        public FenceInfo FenceInfo { get; }
        public FenceWidgetSnapshot Snapshot { get; }
        public int TitleHeight { get; }
        public int ScrollOffset { get; }
        public string SelectedItem { get; }
        public string HoveringItem { get; }
        public Font TitleFont { get; }
        public Font BodyFont { get; }
        public IconCache IconCache { get; }
        public Color TextColor { get; }
        public Color AccentColor { get; }
    }
}
