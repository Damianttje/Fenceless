using System;
using System.Drawing;

namespace Fenceless.Model
{
    public sealed class FenceWidgetItem
    {
        public FenceWidgetItem(
            string id,
            FenceEntryKind kind,
            string title,
            string subtitle = "",
            string detail = "",
            string legacyValue = "",
            string path = "",
            IntPtr taskHandle = default,
            int? clipboardIndex = null,
            DateTime? timestamp = null,
            bool isMinimized = false,
            string iconPath = "",
            Image previewImage = null)
        {
            Id = id ?? string.Empty;
            Kind = kind;
            Title = title ?? string.Empty;
            Subtitle = subtitle ?? string.Empty;
            Detail = detail ?? string.Empty;
            LegacyValue = legacyValue ?? string.Empty;
            Path = path ?? string.Empty;
            TaskHandle = taskHandle;
            ClipboardIndex = clipboardIndex;
            Timestamp = timestamp;
            IsMinimized = isMinimized;
            IconPath = iconPath ?? string.Empty;
            PreviewImage = previewImage;
        }

        public string Id { get; }
        public FenceEntryKind Kind { get; }
        public string Title { get; }
        public string Subtitle { get; }
        public string Detail { get; }
        public string LegacyValue { get; }
        public string Path { get; }
        public IntPtr TaskHandle { get; }
        public int? ClipboardIndex { get; }
        public DateTime? Timestamp { get; }
        public bool IsMinimized { get; }
        public string IconPath { get; }
        public Image PreviewImage { get; }
    }
}
