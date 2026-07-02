using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Fenceless.Util;

namespace Fenceless.Model
{
    public class ClipboardHistoryFence : IFenceProvider, IWidgetDataProvider
    {
        private readonly FenceInfo fenceInfo;
        private readonly Logger logger;
        private readonly List<ClipboardHistoryItem> historyItems = new List<ClipboardHistoryItem>();
        private readonly object snapshotLock = new object();
        private FenceWidgetSnapshot snapshot;
        private IntPtr windowHandle;
        private bool disposed;

        public event Action ItemsChanged;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool AddClipboardFormatListener(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool RemoveClipboardFormatListener(IntPtr hWnd);

        public ClipboardHistoryFence(FenceInfo fenceInfo)
        {
            this.fenceInfo = fenceInfo;
            logger = Logger.Instance;
            snapshot = FenceWidgetSnapshot.Empty(FenceType.ClipboardHistory, fenceInfo.Name, "Clipboard is empty");
            logger.Info($"ClipboardHistory fence '{fenceInfo.Name}' initialized", "ClipboardHistoryFence");
        }

        public void StartListening(IntPtr handle)
        {
            windowHandle = handle;
            if (handle != IntPtr.Zero)
            {
                AddClipboardFormatListener(handle);
                logger.Debug($"Clipboard listener started for fence '{fenceInfo.Name}'", "ClipboardHistoryFence");
            }
        }

        public void OnClipboardChanged()
        {
            try
            {
                if (disposed) return;

                ClipboardHistoryItem item = null;

                if (Clipboard.ContainsText())
                {
                    var text = Clipboard.GetText();
                    if (string.IsNullOrEmpty(text)) return;

                    if (historyItems.Count > 0 && historyItems[0].Kind == FenceEntryKind.ClipboardText && historyItems[0].Text == text) return;
                    item = ClipboardHistoryItem.FromText(text);
                }
                else if (fenceInfo.CaptureImages && Clipboard.ContainsImage())
                {
                    var image = Clipboard.GetImage();
                    if (image == null) return;

                    item = ClipboardHistoryItem.FromImage(CreateBoundedPreview(image, 220, 140));
                }

                if (item == null) return;

                historyItems.Insert(0, item);

                if (fenceInfo.MaxItems > 0 && historyItems.Count > fenceInfo.MaxItems)
                {
                    foreach (var oldItem in historyItems.Skip(fenceInfo.MaxItems).ToList())
                        oldItem.Dispose();
                    historyItems.RemoveRange(fenceInfo.MaxItems, historyItems.Count - fenceInfo.MaxItems);
                }

                UpdateFenceFiles();
                ItemsChanged?.Invoke();
            }
            catch (Exception ex)
            {
                logger.Error($"Error handling clipboard change for fence '{fenceInfo.Name}'", "ClipboardHistoryFence", ex);
            }
        }

        private void UpdateFenceFiles()
        {
            fenceInfo.Files.Clear();
            for (int i = 0; i < historyItems.Count; i++)
            {
                var item = historyItems[i];
                fenceInfo.Files.Add(item.ToLegacyValue(i));
            }

            var widgetItems = new List<FenceWidgetItem>(historyItems.Count);
            for (int i = 0; i < historyItems.Count; i++)
            {
                var item = historyItems[i];
                var legacyValue = item.ToLegacyValue(i);
                widgetItems.Add(new FenceWidgetItem(
                    legacyValue,
                    item.Kind,
                    item.Title,
                    item.Subtitle,
                    item.Detail,
                    legacyValue,
                    clipboardIndex: i,
                    timestamp: item.CreatedAt,
                    previewImage: item.PreviewImage));
            }

            lock (snapshotLock)
            {
                snapshot = new FenceWidgetSnapshot(
                    FenceType.ClipboardHistory,
                    widgetItems,
                    fenceInfo.Name,
                    "Session clipboard timeline",
                    $"{widgetItems.Count} item{(widgetItems.Count == 1 ? "" : "s")}",
                    DateTime.Now);
            }
        }

        public string GetClipboardText(int index)
        {
            if (index >= 0 && index < historyItems.Count && historyItems[index].Kind == FenceEntryKind.ClipboardText)
                return historyItems[index].Text;
            return null;
        }

        public Image GetClipboardImage(int index)
        {
            if (index >= 0 && index < historyItems.Count && historyItems[index].Kind == FenceEntryKind.ClipboardImage)
                return historyItems[index].PreviewImage;
            return null;
        }

        public void RemoveAt(int index)
        {
            if (index < 0 || index >= historyItems.Count) return;
            historyItems[index].Dispose();
            historyItems.RemoveAt(index);
            UpdateFenceFiles();
            ItemsChanged?.Invoke();
        }

        public void Clear()
        {
            foreach (var item in historyItems)
                item.Dispose();
            historyItems.Clear();
            UpdateFenceFiles();
            ItemsChanged?.Invoke();
        }

        public void Refresh()
        {
            UpdateFenceFiles();
            ItemsChanged?.Invoke();
        }

        public FenceWidgetSnapshot GetSnapshot()
        {
            lock (snapshotLock)
            {
                return snapshot;
            }
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;

            if (windowHandle != IntPtr.Zero)
            {
                try
                {
                    RemoveClipboardFormatListener(windowHandle);
                }
                catch (Exception ex)
                {
                    logger.Error($"Error removing clipboard listener for '{fenceInfo.Name}'", "ClipboardHistoryFence", ex);
                }
            }

            foreach (var item in historyItems)
                item.Dispose();
            historyItems.Clear();

            logger.Debug($"Disposed ClipboardHistory fence '{fenceInfo.Name}'", "ClipboardHistoryFence");
        }

        private static Image CreateBoundedPreview(Image source, int maxWidth, int maxHeight)
        {
            var scale = Math.Min(maxWidth / (double)source.Width, maxHeight / (double)source.Height);
            scale = Math.Min(1.0, Math.Max(0.01, scale));
            var width = Math.Max(1, (int)Math.Round(source.Width * scale));
            var height = Math.Max(1, (int)Math.Round(source.Height * scale));
            var bitmap = new Bitmap(width, height);
            using (var graphics = Graphics.FromImage(bitmap))
            {
                graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                graphics.DrawImage(source, 0, 0, width, height);
            }
            return bitmap;
        }

        private sealed class ClipboardHistoryItem : IDisposable
        {
            private ClipboardHistoryItem(FenceEntryKind kind, string text, Image previewImage)
            {
                Kind = kind;
                Text = text ?? string.Empty;
                PreviewImage = previewImage;
                CreatedAt = DateTime.Now;
            }

            public FenceEntryKind Kind { get; }
            public string Text { get; }
            public Image PreviewImage { get; }
            public DateTime CreatedAt { get; }

            public string Title
            {
                get
                {
                    if (Kind == FenceEntryKind.ClipboardImage)
                        return "Image";

                    var singleLine = Text.Replace("\r", " ").Replace("\n", " ").Trim();
                    return singleLine.Length > 80 ? singleLine.Substring(0, 80) + "\u2026" : singleLine;
                }
            }

            public string Subtitle
            {
                get
                {
                    if (Kind == FenceEntryKind.ClipboardImage && PreviewImage != null)
                        return $"{PreviewImage.Width} x {PreviewImage.Height}";

                    var lineCount = Math.Max(1, Text.Split('\n').Length);
                    return $"{Text.Length} chars | {lineCount} line{(lineCount == 1 ? "" : "s")}";
                }
            }

            public string Detail => CreatedAt.ToString("g");

            public static ClipboardHistoryItem FromText(string text)
            {
                return new ClipboardHistoryItem(FenceEntryKind.ClipboardText, text, null);
            }

            public static ClipboardHistoryItem FromImage(Image previewImage)
            {
                return new ClipboardHistoryItem(FenceEntryKind.ClipboardImage, string.Empty, previewImage);
            }

            public string ToLegacyValue(int index)
            {
                if (Kind == FenceEntryKind.ClipboardImage)
                    return $"clipimg:{index}:Image";

                return $"clip:{index}:{Title}";
            }

            public void Dispose()
            {
                PreviewImage?.Dispose();
            }
        }
    }
}
