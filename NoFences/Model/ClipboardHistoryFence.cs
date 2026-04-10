using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Fenceless.Util;

namespace Fenceless.Model
{
    public class ClipboardHistoryFence : IFenceProvider
    {
        private readonly FenceInfo fenceInfo;
        private readonly Logger logger;
        private readonly List<string> historyItems = new List<string>();
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

                var text = Clipboard.GetText();
                if (string.IsNullOrEmpty(text)) return;

                if (historyItems.Count > 0 && historyItems[0] == text) return;

                historyItems.Insert(0, text);

                if (fenceInfo.MaxItems > 0 && historyItems.Count > fenceInfo.MaxItems)
                    historyItems.RemoveRange(fenceInfo.MaxItems, historyItems.Count - fenceInfo.MaxItems);

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
                var text = historyItems[i];
                var preview = text.Length > 60
                    ? text.Substring(0, 60) + "\u2026"
                    : text;
                fenceInfo.Files.Add($"clip:{i}:{preview}");
            }
        }

        public string GetClipboardText(int index)
        {
            if (index >= 0 && index < historyItems.Count)
                return historyItems[index];
            return null;
        }

        public void Refresh()
        {
            UpdateFenceFiles();
            ItemsChanged?.Invoke();
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

            logger.Debug($"Disposed ClipboardHistory fence '{fenceInfo.Name}'", "ClipboardHistoryFence");
        }
    }
}
