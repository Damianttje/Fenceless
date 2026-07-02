using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace Fenceless.Model
{
    public sealed class FenceItemLayoutSnapshot
    {
        private FenceItemLayoutSnapshot(List<FenceLayoutItem> items, FenceGridLayout gridLayout, int contentBottom)
        {
            Items = items;
            GridLayout = gridLayout;
            ContentBottom = contentBottom;
        }

        public IReadOnlyList<FenceLayoutItem> Items { get; }
        public FenceGridLayout GridLayout { get; }
        public int ContentBottom { get; }

        public static FenceItemLayoutSnapshot Create(
            IReadOnlyList<string> entries,
            int clientWidth,
            int titleHeight,
            int scrollOffset,
            int itemSpacing,
            int iconSize,
            int baseItemWidth,
            int baseTextHeight)
        {
            var gridLayout = FenceGridLayout.Calculate(clientWidth, itemSpacing, iconSize, baseItemWidth, baseTextHeight);
            var items = new List<FenceLayoutItem>(entries.Count);
            var contentBottom = 0;

            for (var i = 0; i < entries.Count; i++)
            {
                var position = gridLayout.GetItemPosition(i, titleHeight, scrollOffset);
                var bounds = new Rectangle(position.X, position.Y, gridLayout.ActualItemWidth, gridLayout.ActualItemHeight);
                items.Add(new FenceLayoutItem(entries[i], i, bounds));

                var unscrolledPosition = gridLayout.GetItemPosition(i, titleHeight, 0);
                contentBottom = Math.Max(contentBottom, unscrolledPosition.Y - titleHeight + gridLayout.ActualItemHeight);
            }

            return new FenceItemLayoutSnapshot(items, gridLayout, contentBottom);
        }

        public string? HitTest(Point position)
        {
            return Items.FirstOrDefault(item => item.Bounds.Contains(position))?.Value;
        }

        public int GetTargetIndex(Point position, int titleHeight, int scrollOffset)
        {
            return GridLayout.GetGridIndex(position, titleHeight, scrollOffset, Items.Count);
        }

        public int GetMaxScrollOffset(int viewportHeight)
        {
            return Math.Max(0, ContentBottom - Math.Max(0, viewportHeight));
        }
    }

    public sealed class FenceLayoutItem
    {
        public FenceLayoutItem(string value, int index, Rectangle bounds)
        {
            Value = value;
            Index = index;
            Bounds = bounds;
        }

        public string Value { get; }
        public int Index { get; }
        public Rectangle Bounds { get; }
    }
}
