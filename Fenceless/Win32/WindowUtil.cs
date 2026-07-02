using System;
using System.Runtime.InteropServices;
using Fenceless.Util;

namespace Fenceless.Win32
{
    public class WindowUtil
    {
        public const int WM_NCHITTEST = 0x84;          // variables for dragging the form
        public const int HTCLIENT = 0x1;
        public const int HTCAPTION = 0x2;
        public const int HTLEFT = 10;
        public const int HTRIGHT = 11;
        public const int HTTOP = 12;
        public const int HTTOPLEFT = 13;
        public const int HTTOPRIGHT = 14;
        public const int HTBOTTOM = 15;
        public const int HTBOTTOMLEFT = 16;
        public const int HTBOTTOMRIGHT = 17;

        public const int WM_SYSCOMMAND = 274;
        public const int SC_MAXIMIZE = 0xF030;
        public const int SC_MINIMIZE = 0xF020;

        public const UInt32 SWP_NOSIZE = 0x0001;
        public const UInt32 SWP_NOMOVE = 0x0002;
        public const UInt32 SWP_NOACTIVATE = 0x0010;
        public const UInt32 SWP_NOZORDER = 0x0004;
        public const int WM_ACTIVATEAPP = 0x001C;
        public const int WM_ACTIVATE = 0x0006;
        public const int WM_SETFOCUS = 0x0007;
        public const int WM_SHOWWINDOW = 0x0018;
        public const int WM_SIZE = 0x0005;
    public const int WM_COMMAND = 0x0111;
    public const int WM_WINDOWPOSCHANGING = 0x0046;
    public const int WM_WINDOWPOSCHANGED = 0x0047;
    // Shell taskbar commands triggered by Show Desktop
    public const int MIN_ALL = 0x01A3;
    public const int MIN_ALL_UNDO = 0x01A0;
        public static readonly IntPtr HWND_BOTTOM = new IntPtr(1);

        [StructLayout(LayoutKind.Sequential)]
        public struct WINDOWPOS
        {
            public IntPtr hwnd;
            public IntPtr hwndInsertAfter;
            public int x;
            public int y;
            public int cx;
            public int cy;
            public uint flags;
        }

        public const int SIZE_RESTORED = 0;
        public const int SIZE_MINIMIZED = 1;

        public const int SW_SHOWNORMAL = 1;
        public const int SW_SHOWNOACTIVATE = 4;
        public const int SW_SHOW = 5;
        public const int SW_RESTORE = 9;

        [DllImport("user32.dll")]
        public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        public static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool IsIconic(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X,
           int Y, int cx, int cy, uint uFlags);
        [DllImport("user32.dll")]
        public static extern IntPtr DeferWindowPos(IntPtr hWinPosInfo, IntPtr hWnd,
           IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);
        [DllImport("user32.dll")]
        public static extern IntPtr BeginDeferWindowPos(int nNumWindows);
        [DllImport("user32.dll")]
        public static extern bool EndDeferWindowPos(IntPtr hWinPosInfo);

        [DllImport("user32.dll")]
        public static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);
        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        #region Window styles
        [Flags]
        public enum ExtendedWindowStyles
        {
            // ...
            WS_EX_TOOLWINDOW = 0x00000080,
            WS_EX_NOACTIVATE = 0x08000000,
            // ...
        }

        public enum GetWindowLongFields
        {
            // ...
            GWL_EXSTYLE = (-20),
            // ...
        }

        [DllImport("user32.dll")]
        public static extern IntPtr GetWindowLong(IntPtr hWnd, int nIndex);

        public static IntPtr SetWindowLong(IntPtr hWnd, int nIndex, IntPtr dwNewLong)
        {
            int error = 0;
            IntPtr result = IntPtr.Zero;
            // Win32 SetWindowLong doesn't clear error on success
            SetLastError(0);

            if (IntPtr.Size == 4)
            {
                // use SetWindowLong
                Int32 tempResult = IntSetWindowLong(hWnd, nIndex, IntPtrToInt32(dwNewLong));
                error = Marshal.GetLastWin32Error();
                result = new IntPtr(tempResult);
            }
            else
            {
                // use SetWindowLongPtr
                result = IntSetWindowLongPtr(hWnd, nIndex, dwNewLong);
                error = Marshal.GetLastWin32Error();
            }

            if ((result == IntPtr.Zero) && (error != 0))
            {
                throw new System.ComponentModel.Win32Exception(error);
            }

            return result;
        }

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr", SetLastError = true)]
        private static extern IntPtr IntSetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("user32.dll", EntryPoint = "SetWindowLong", SetLastError = true)]
        private static extern Int32 IntSetWindowLong(IntPtr hWnd, int nIndex, Int32 dwNewLong);

        private static int IntPtrToInt32(IntPtr intPtr)
        {
            return unchecked((int)intPtr.ToInt64());
        }

        [DllImport("kernel32.dll", EntryPoint = "SetLastError")]
        public static extern void SetLastError(int dwErrorCode);
        #endregion

        //allows the context menu to be in dark mode (1), or force dark mode (2)
        [DllImport("uxtheme.dll", EntryPoint = "#135", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern int SetPreferredAppMode(int preferredAppMode);

        #region Fluent / Windows 11 backdrop

        // DWM attribute IDs (Windows 11 build 22000+).
        private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;   // DWM_WINDOW_CORNER_PREFERENCE
        private const int DWMWA_SYSTEMBACKDROP_TYPE = 38;        // DWMSBT_* (build 22523+)
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;   // dark title bar / borders

        // DWM_WINDOW_CORNER_PREFERENCE values
        private const int DWMWCP_DEFAULT = 0;
        private const int DWMWCP_DONOTROUND = 1;
        private const int DWMWCP_ROUND = 2;
        private const int DWMWCP_ROUNDSMALL = 3;

        // DWMSYSTEMBACKDROP_TYPE values
        private const int DWMSBT_AUTO = 0;
        private const int DWMSBT_NONE = 1;
        private const int DWMSBT_MAINWINDOW = 2;   // Mica
        private const int DWMSBT_TRANSIENTWINDOW = 3; // Acrylic
        private const int DWMSBT_TABBEDWINDOW = 4; // Tabbed

        [DllImport("dwmapi.dll", PreserveSig = false)]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

        [DllImport("dwmapi.dll", PreserveSig = false)]
        private static extern int DwmGetWindowAttribute(IntPtr hwnd, int attr, out int value, int size);

        private static bool? _isWin11;

        /// <summary>
        /// True on Windows 11 (build 22000+) where Fluent backdrop APIs are available.
        /// </summary>
        public static bool IsWindows11
        {
            get
            {
                if (_isWin11.HasValue) return _isWin11.Value;
                bool result;
                try
                {
                    using (var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                        @"SOFTWARE\Microsoft\Windows NT\CurrentVersion"))
                    {
                        var current = key?.GetValue("CurrentBuild") as string;
                        result = int.TryParse(current ?? "", out int build) && build >= 22000;
                    }
                }
                catch
                {
                    result = false;
                }
                _isWin11 = result;
                return result;
            }
        }

        /// <summary>
        /// Apply a Mica (or fallback) backdrop and rounded corners on Windows 11.
        /// Silently no-ops on Windows 10.
        /// </summary>
        public static void ApplyFluentBackdrop(IntPtr handle, bool darkMode = true, bool mica = true)
        {
            if (!IsWindows11 || handle == IntPtr.Zero) return;
            try
            {
                int dark = darkMode ? 1 : 0;
                DwmSetWindowAttribute(handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref dark, sizeof(int));

                int corner = DWMWCP_ROUND;
                DwmSetWindowAttribute(handle, DWMWA_WINDOW_CORNER_PREFERENCE, ref corner, sizeof(int));

                int backdrop = mica ? DWMSBT_MAINWINDOW : DWMSBT_TRANSIENTWINDOW;
                DwmSetWindowAttribute(handle, DWMWA_SYSTEMBACKDROP_TYPE, ref backdrop, sizeof(int));
            }
            catch
            {
                // Best-effort; never throw for cosmetic attributes.
            }
        }

        #endregion

        public static void HideFromAltTab(IntPtr Handle)
        {
            try
            {
                if (Handle == IntPtr.Zero)
                {
                    Logger.Instance?.Warning("Attempted to hide null window handle from Alt+Tab", "WindowUtil");
                    return;
                }

                // Get current extended style
                var exStyle = GetWindowLong(Handle, (int)GetWindowLongFields.GWL_EXSTYLE);
                if (exStyle == IntPtr.Zero)
                {
                    var error = Marshal.GetLastWin32Error();
                    Logger.Instance?.Warning($"GetWindowLong failed with error {error} when hiding from Alt+Tab", "WindowUtil");
                    return;
                }

                // Add WS_EX_TOOLWINDOW and WS_EX_NOACTIVATE styles, remove WS_EX_APPWINDOW style
                exStyle = new IntPtr(exStyle.ToInt64() | (int)ExtendedWindowStyles.WS_EX_TOOLWINDOW);
                exStyle = new IntPtr(exStyle.ToInt64() | (int)ExtendedWindowStyles.WS_EX_NOACTIVATE);
                exStyle = new IntPtr(exStyle.ToInt64() & ~0x00040000); // Remove WS_EX_APPWINDOW
                
                SetWindowLong(Handle, (int)GetWindowLongFields.GWL_EXSTYLE, exStyle);

                // Move window to bottom to ensure it doesn't appear in Alt+Tab
                SetWindowPos(Handle, HWND_BOTTOM, 0, 0, 0, 0, SWP_NOSIZE | SWP_NOMOVE | SWP_NOACTIVATE);
                
                Logger.Instance?.Debug("Successfully hidden window from Alt+Tab and prevented Show Desktop minimize", "WindowUtil");
            }
            catch (Exception ex)
            {
                Logger.Instance?.Error("Failed to hide window from Alt+Tab", "WindowUtil", ex);
            }
        }
    }
}
