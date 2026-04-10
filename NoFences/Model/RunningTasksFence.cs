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
    public class RunningTasksFence : IFenceProvider
    {
        private readonly FenceInfo fenceInfo;
        private readonly Logger logger;
        private Timer pollTimer;
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

            int interval = fenceInfo.UpdateInterval > 0 ? fenceInfo.UpdateInterval : 3000;
            pollTimer = new Timer(PollWindows, null, TimeSpan.Zero, TimeSpan.FromMilliseconds(interval));

            PollWindows(null);
            logger.Info($"RunningTasks fence '{fenceInfo.Name}' initialized with {interval}ms interval", "RunningTasksFence");
        }

        private void PollWindows(object state)
        {
            try
            {
                var windows = new List<(IntPtr Hwnd, string Title)>();
                var processFilter = fenceInfo.ProcessFilter;

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

                        if (!fenceInfo.ShowMinimizedWindows && IsIconic(hWnd)) return true;

                        if (!string.IsNullOrEmpty(processFilter))
                        {
                            GetWindowThreadProcessId(hWnd, out uint processId);
                            try
                            {
                                var process = Process.GetProcessById((int)processId);
                                if (process.ProcessName.IndexOf(processFilter, StringComparison.OrdinalIgnoreCase) < 0)
                                    return true;
                            }
                            catch
                            {
                                return true;
                            }
                        }

                        windows.Add((hWnd, title));
                    }
                    catch
                    {
                    }

                    return true;
                }, IntPtr.Zero);

                if (fenceInfo.MaxItems > 0 && windows.Count > fenceInfo.MaxItems)
                    windows = windows.Take(fenceInfo.MaxItems).ToList();

                fenceInfo.Files.Clear();
                foreach (var (hwnd, title) in windows)
                {
                    fenceInfo.Files.Add($"task:{hwnd.ToInt64()}:{title}");
                }

                ItemsChanged?.Invoke();
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
    }
}
