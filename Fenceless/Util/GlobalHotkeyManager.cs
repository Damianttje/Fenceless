using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Fenceless.Util
{
    public class GlobalHotkeyManager : IDisposable
    {
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private const int WM_HOTKEY = 0x0312;
        private const uint MOD_ALT = 0x0001;
        private const uint MOD_CONTROL = 0x0002;
        private const uint MOD_SHIFT = 0x0004;
        private const uint MOD_WIN = 0x0008;
        private const int ERROR_HOTKEY_ALREADY_REGISTERED = 1409;

        private readonly MessageWindow messageWindow;
        private readonly Dictionary<int, Action> hotkeyActions = new Dictionary<int, Action>();
        private int nextHotkeyId = 1;
        private bool disposed = false;
        private readonly Logger logger = Logger.Instance;

        public GlobalHotkeyManager()
        {
            messageWindow = new MessageWindow(this);
        }

        public HotkeyRegistrationResult RegisterHotkeyDetailed(Keys key, bool ctrl = false, bool alt = false, bool shift = false, bool win = false, Action? action = null)
        {
            uint modifiers = 0;
            if (ctrl) modifiers |= MOD_CONTROL;
            if (alt) modifiers |= MOD_ALT;
            if (shift) modifiers |= MOD_SHIFT;
            if (win) modifiers |= MOD_WIN;

            int id = nextHotkeyId++;
            if (RegisterHotKey(messageWindow.Handle, id, modifiers, (uint)key))
            {
                if (action != null)
                {
                    hotkeyActions[id] = action;
                }
                logger?.Debug($"Successfully registered hotkey ID {id} (Ctrl:{ctrl} Alt:{alt} Shift:{shift} Win:{win} Key:{key})", "GlobalHotkeyManager");
                return HotkeyRegistrationResult.Success(id);
            }
            else
            {
                var error = Marshal.GetLastWin32Error();
                var message = DescribeNativeError(error);
                if (error == ERROR_HOTKEY_ALREADY_REGISTERED)
                {
                    logger?.Info($"Hotkey already in use by Windows or another app (Ctrl:{ctrl} Alt:{alt} Shift:{shift} Win:{win} Key:{key})", "GlobalHotkeyManager");
                }
                else
                {
                    logger?.Warning($"Failed to register hotkey (Ctrl:{ctrl} Alt:{alt} Shift:{shift} Win:{win} Key:{key}, Error:{error}: {message})", "GlobalHotkeyManager");
                }

                return HotkeyRegistrationResult.Failure(error, message);
            }
        }

        public void UnregisterHotkey(int id)
        {
            if (UnregisterHotKey(messageWindow.Handle, id))
            {
                logger?.Debug($"Successfully unregistered hotkey ID {id}", "GlobalHotkeyManager");
            }
            else
            {
                logger?.Warning($"Failed to unregister hotkey ID {id}", "GlobalHotkeyManager");
            }
            hotkeyActions.Remove(id);
        }

        public sealed class HotkeyRegistrationResult
        {
            private HotkeyRegistrationResult(int id, int nativeError, string message)
            {
                Id = id;
                NativeError = nativeError;
                Message = message;
            }

            public int Id { get; }
            public int NativeError { get; }
            public string Message { get; }
            public bool Registered => Id != -1;

            public static HotkeyRegistrationResult Success(int id) => new HotkeyRegistrationResult(id, 0, "");
            public static HotkeyRegistrationResult Failure(int nativeError, string message) => new HotkeyRegistrationResult(-1, nativeError, message);
        }

        private void OnHotkeyPressed(int id)
        {
            if (hotkeyActions.TryGetValue(id, out Action? action))
            {
                try
                {
                    action?.Invoke();
                }
                catch (Exception ex)
                {
                    logger?.Error($"Error executing hotkey action for ID {id}", "GlobalHotkeyManager", ex);
                }
            }
        }

        public void Dispose()
        {
            if (!disposed)
            {
                foreach (var id in hotkeyActions.Keys.ToList())
                {
                    UnregisterHotKey(messageWindow.Handle, id);
                }
                hotkeyActions.Clear();
                messageWindow?.DestroyHandle();
                disposed = true;
            }
        }

        private static string DescribeNativeError(int error)
        {
            switch (error)
            {
                case ERROR_HOTKEY_ALREADY_REGISTERED:
                    return "Shortcut is already registered by Windows or another application.";
                case 0:
                    return "No native error was reported.";
                default:
                    return $"Win32 error {error}.";
            }
        }

        private class MessageWindow : NativeWindow
        {
            private readonly GlobalHotkeyManager manager;

            public MessageWindow(GlobalHotkeyManager manager)
            {
                this.manager = manager;
                CreateHandle(new CreateParams());
            }

            protected override void WndProc(ref Message m)
            {
                if (m.Msg == WM_HOTKEY)
                {
                    int id = m.WParam.ToInt32();
                    manager.OnHotkeyPressed(id);
                }
                base.WndProc(ref m);
            }
        }
    }
}
