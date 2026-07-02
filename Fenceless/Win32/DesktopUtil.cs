using System;
using System.Runtime.InteropServices;
using Fenceless.Util;

namespace Fenceless.Win32
{
    public class DesktopUtil
    {
        private const Int32 GWL_STYLE = -16;
        private const Int32 GWL_HWNDPARENT = -8;
        private const Int32 WS_MAXIMIZEBOX = 0x00010000;
        private const Int32 WS_MINIMIZEBOX = 0x00020000;

        [DllImport("User32.dll", EntryPoint = "GetWindowLong")]
        private extern static Int32 GetWindowLongPtr(IntPtr hWnd, Int32 nIndex);

        [DllImport("User32.dll", EntryPoint = "SetWindowLong")]
        private extern static Int32 SetWindowLongPtr(IntPtr hWnd, Int32 nIndex, Int32 dwNewLong);

        [DllImport("user32.dll", SetLastError = true)]
        static extern int SetWindowLong(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr FindWindow(string lpWindowClass, string lpWindowName);

        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr FindWindowEx(IntPtr parentHandle, IntPtr childAfter, string className, string windowTitle);

        [DllImport("user32.dll")]
        static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        private static IntPtr workerW = IntPtr.Zero;

        public static void PreventMinimize(IntPtr handle)
        {
            Int32 windowStyle = GetWindowLongPtr(handle, GWL_STYLE);
            SetWindowLongPtr(handle, GWL_STYLE, windowStyle & ~WS_MAXIMIZEBOX & ~WS_MINIMIZEBOX);
        }

        public static void GlueToDesktop(IntPtr handle)
        {
            try
            {
                // Get the WorkerW window that contains the desktop icons
                IntPtr progman = FindWindow("Progman", null);
                
                // Send message to Progman to spawn WorkerW
                SendMessage(progman, 0x052C, IntPtr.Zero, IntPtr.Zero);
                
                // Find the WorkerW window
                workerW = IntPtr.Zero;
                EnumWindows(new EnumWindowsProc(EnumWindowsCallback), IntPtr.Zero);
                
                // If WorkerW found, set it as parent; otherwise use Progman
                IntPtr desktopHandle = workerW != IntPtr.Zero ? workerW : progman;
                
                if (desktopHandle != IntPtr.Zero)
                {
                    WindowUtil.SetWindowLong(handle, GWL_HWNDPARENT, desktopHandle);
                    WindowUtil.SetWindowPos(handle, WindowUtil.HWND_BOTTOM, 0, 0, 0, 0,
                        WindowUtil.SWP_NOMOVE | WindowUtil.SWP_NOSIZE | WindowUtil.SWP_NOACTIVATE);
                    Logger.Instance?.Debug($"Window glued to desktop owner (handle: {desktopHandle})", "DesktopUtil");
                }
                else
                {
                    Logger.Instance?.Warning("Could not find desktop handle to glue window", "DesktopUtil");
                }
            }
            catch (Exception ex)
            {
                Logger.Instance?.Error("Failed to glue window to desktop", "DesktopUtil", ex);
            }
        }

        private static bool EnumWindowsCallback(IntPtr hWnd, IntPtr lParam)
        {
            IntPtr shellDllDefView = FindWindowEx(hWnd, IntPtr.Zero, "SHELLDLL_DefView", null);
            if (shellDllDefView != IntPtr.Zero)
            {
                workerW = FindWindowEx(IntPtr.Zero, hWnd, "WorkerW", null);
            }
            return true;
        }
    }
}