using System;
using System.Drawing;

namespace Fenceless.Model
{
    public readonly struct FenceGridLayout
    {
        public FenceGridLayout(int itemSpacing, int actualItemWidth, int actualItemHeight, int itemsPerRow)
        {
            ItemSpacing = itemSpacing;
            ActualItemWidth = actualItemWidth;
            ActualItemHeight = actualItemHeight;
            ItemsPerRow = itemsPerRow;
        }

        public int ItemSpacing { get; }
        public int ActualItemWidth { get; }
        public int ActualItemHeight { get; }
        public int ItemsPerRow { get; }

        public static FenceGridLayout Calculate(
            int clientWidth,
            int itemSpacing,
            int iconSize,
            int baseItemWidth,
            int baseTextHeight)
        {
            var safeSpacing = Math.Max(0, itemSpacing);
            var actualItemWidth = Math.Max(iconSize + 10, baseItemWidth);
            var actualItemHeight = Math.Max(1, iconSize + baseTextHeight + 10);
            var availableWidth = Math.Max(1, clientWidth - safeSpacing);
            var itemsPerRow = Math.Max(1, availableWidth / Math.Max(1, actualItemWidth + safeSpacing));

            return new FenceGridLayout(safeSpacing, actualItemWidth, actualItemHeight, itemsPerRow);
        }

        public Point GetItemPosition(int index, int titleHeight, int scrollOffset)
        {
            var safeIndex = Math.Max(0, index);
            var row = safeIndex / ItemsPerRow;
            var col = safeIndex % ItemsPerRow;
            var x = ItemSpacing + col * (ActualItemWidth + ItemSpacing);
            var y = ItemSpacing + row * (ActualItemHeight + ItemSpacing) + titleHeight - scrollOffset;
            return new Point(x, y);
        }

        public int GetGridIndex(Point position, int titleHeight, int scrollOffset, int maxItems)
        {
            var contentY = position.Y - titleHeight + scrollOffset;
            var row = Math.Max(0, (contentY - ItemSpacing) / Math.Max(1, ActualItemHeight + ItemSpacing));
            var col = Math.Max(0, (position.X - ItemSpacing) / Math.Max(1, ActualItemWidth + ItemSpacing));
            col = Math.Min(col, ItemsPerRow - 1);
            var index = row * ItemsPerRow + col;
            return Math.Min(index, Math.Max(0, maxItems));
        }
    }
}
