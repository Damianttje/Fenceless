using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Fenceless.Util;

namespace Fenceless.Model
{
    public class RunningTasksFence : IFenceProvider, IWidgetDataProvider
    {
        private readonly FenceInfo fenceInfo;
        private readonly Logger logger;
        private readonly object snapshotLock = new object();
        private Timer pollTimer;
        private FenceWidgetSnapshot snapshot;
        private bool disposed;

        public event Action ItemsChanged;

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool IsIconic(IntPtr hWnd);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll")]
        private static extern int GetWindowTextLength(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        private const int SW_RESTORE = 9;

        private static readonly IntPtr[] ExcludedHwnds = Array.Empty<IntPtr>();

        public RunningTasksFence(FenceInfo fenceInfo)
        {
            this.fenceInfo = fenceInfo;
            logger = Logger.Instance;
            snapshot = FenceWidgetSnapshot.Empty(FenceType.RunningTasks, fenceInfo.Name, "No windows found");

            int interval = fenceInfo.UpdateInterval > 0 ? fenceInfo.UpdateInterval : 3000;
            pollTimer = new Timer(PollWindows, null, TimeSpan.Zero, TimeSpan.FromMilliseconds(interval));

            PollWindows(null);
            logger.Info($"RunningTasks fence '{fenceInfo.Name}' initialized with {interval}ms interval", "RunningTasksFence");
        }

        private void PollWindows(object state)
        {
            try
            {
                var windows = new List<WindowInfo>();
                var processFilter = fenceInfo.ProcessFilter;
                var currentProcessId = Process.GetCurrentProcess().Id;

                EnumWindows((hWnd, lParam) =>
                {
                    try
                    {
                        if (hWnd == IntPtr.Zero) return true;
                        if (!IsWindowVisible(hWnd)) return true;

                        var titleLength = GetWindowTextLength(hWnd);
                        if (titleLength == 0) return true;

                        var sb = new StringBuilder(titleLength + 1);
                        GetWindowText(hWnd, sb, sb.Capacity);
                        var title = sb.ToString();

                        if (string.IsNullOrWhiteSpace(title)) return true;

                        var minimized = IsIconic(hWnd);
                        if (!fenceInfo.ShowMinimizedWindows && minimized) return true;

                        GetWindowThreadProcessId(hWnd, out uint processId);
                        if (processId == currentProcessId) return true;

                        string processName = "Unknown";
                        string iconPath = string.Empty;
                        try
                        {
                            var process = Process.GetProcessById((int)processId);
                            processName = process.ProcessName;
                            try { iconPath = process.MainModule?.FileName ?? string.Empty; }
                            catch { }
                        }
                        catch
                        {
                        }

                        if (!string.IsNullOrEmpty(processFilter))
                        {
                            if (processName.IndexOf(processFilter, StringComparison.OrdinalIgnoreCase) < 0)
                                return true;
                        }

                        windows.Add(new WindowInfo(hWnd, title, processName, iconPath, (int)processId, minimized));
                    }
                    catch
                    {
                    }

                    return true;
                }, IntPtr.Zero);

                if (fenceInfo.MaxItems > 0 && windows.Count > fenceInfo.MaxItems)
                    windows = windows.Take(fenceInfo.MaxItems).ToList();

                var oldSignature = string.Join("|", fenceInfo.Files);
                fenceInfo.Files.Clear();
                var widgetItems = new List<FenceWidgetItem>(windows.Count);
                foreach (var window in windows)
                {
                    var legacyValue = $"task:{window.Hwnd.ToInt64()}:{window.Title}";
                    fenceInfo.Files.Add(legacyValue);
                    widgetItems.Add(new FenceWidgetItem(
                        legacyValue,
                        FenceEntryKind.Task,
                        window.Title,
                        window.ProcessName,
                        window.IsMinimized ? "Minimized" : "Running",
                        legacyValue,
                        taskHandle: window.Hwnd,
                        isMinimized: window.IsMinimized,
                        iconPath: window.IconPath));
                }

                lock (snapshotLock)
                {
                    snapshot = new FenceWidgetSnapshot(
                        FenceType.RunningTasks,
                        widgetItems,
                        fenceInfo.Name,
                        string.IsNullOrEmpty(processFilter) ? "All visible windows" : $"Filter: {processFilter}",
                        $"{widgetItems.Count} window{(widgetItems.Count == 1 ? "" : "s")}",
                        DateTime.Now);
                }

                var newSignature = string.Join("|", fenceInfo.Files);
                if (!string.Equals(oldSignature, newSignature, StringComparison.Ordinal))
                {
                    ItemsChanged?.Invoke();
                }
            }
            catch (Exception ex)
            {
                logger.Error($"Error polling windows for fence '{fenceInfo.Name}'", "RunningTasksFence", ex);
            }
        }

        public void Refresh()
        {
            PollWindows(null);
        }

        public FenceWidgetSnapshot GetSnapshot()
        {
            lock (snapshotLock)
            {
                return snapshot;
            }
        }

        public static bool TryBringToFront(string entry)
        {
            if (string.IsNullOrEmpty(entry) || !entry.StartsWith("task:")) return false;
            try
            {
                var colonIndex = entry.IndexOf(':', 5);
                if (colonIndex < 0) return false;
                var hwndStr = entry.Substring(5, colonIndex - 5);
                var hwnd = new IntPtr(long.Parse(hwndStr));
                if (IsIconic(hwnd))
                    ShowWindow(hwnd, SW_RESTORE);
                SetForegroundWindow(hwnd);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static string GetWindowTitle(string entry)
        {
            if (string.IsNullOrEmpty(entry) || !entry.StartsWith("task:")) return entry;
            var colonIndex = entry.IndexOf(':', 5);
            if (colonIndex < 0) return entry;
            return entry.Substring(colonIndex + 1);
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            pollTimer?.Dispose();
            logger.Debug($"Disposed RunningTasks fence '{fenceInfo.Name}'", "RunningTasksFence");
        }

        private sealed class WindowInfo
        {
            public WindowInfo(IntPtr hwnd, string title, string processName, string iconPath, int processId, bool isMinimized)
            {
                Hwnd = hwnd;
                Title = title;
                ProcessName = processName;
                IconPath = iconPath;
                ProcessId = processId;
                IsMinimized = isMinimized;
            }

            public IntPtr Hwnd { get; }
            public string Title { get; }
            public string ProcessName { get; }
            public string IconPath { get; }
            public int ProcessId { get; }
            public bool IsMinimized { get; }
        }
    }
}
