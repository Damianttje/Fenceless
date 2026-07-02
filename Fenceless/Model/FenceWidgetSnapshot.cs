using System;
using System.Collections.Generic;

namespace Fenceless.Model
{
    public sealed class FenceWidgetSnapshot
    {
        public FenceWidgetSnapshot(
            FenceType fenceType,
            IReadOnlyList<FenceWidgetItem> items,
            string title,
            string subtitle = "",
            string status = "",
            DateTime? updatedAt = null,
            bool hasError = false)
        {
            FenceType = fenceType;
            Items = items ?? Array.Empty<FenceWidgetItem>();
            Title = title ?? string.Empty;
            Subtitle = subtitle ?? string.Empty;
            Status = status ?? string.Empty;
            UpdatedAt = updatedAt;
            HasError = hasError;
        }

        public FenceType FenceType { get; }
        public IReadOnlyList<FenceWidgetItem> Items { get; }
        public string Title { get; }
        public string Subtitle { get; }
        public string Status { get; }
        public DateTime? UpdatedAt { get; }
        public bool HasError { get; }

        public static FenceWidgetSnapshot Empty(FenceType fenceType, string title, string status = "")
        {
            return new FenceWidgetSnapshot(fenceType, Array.Empty<FenceWidgetItem>(), title, status: status);
        }
    }
}
