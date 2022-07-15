using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Input;
using NoPugsNeeded.Events;

namespace NoPugsNeeded.Controllers
{
    public class FollowersMode : IControl
    {
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_KEYUP = 0x0101;
        private const int WM_SYSKEYDOWN = 0x0104;
        private const int WM_SYSKEYUP = 0x0105;
        private LowLevelKeyboardProc _proc;
        private IntPtr _hookID;
        private IEnumerable<IntPtr> _windows;
        private bool _hooked;
        private bool _enabled;
        private bool _altEnabled;
        private bool _shiftDown;
        private bool _ctrlDown;
        private int _exitReason;
        private ControllerEvent _controllerEvent;

        public FollowersMode(ControllerEvent controllerEvent)
        {
            _proc = HookCallback;
            _hookID = IntPtr.Zero;
            _hooked = false;
            _enabled = true;
            _altEnabled = true;
            _shiftDown = false;
            _ctrlDown = false;
            _exitReason = 0;
            _controllerEvent = controllerEvent;
        }

        ~FollowersMode()
        {
            Stop();
        }

        public void Run()
        {
            _windows = FindWindowsWithText("World of Warcraft");
            if (_windows.Count() > 0)
            {
                _hookID = SetHook(_proc);
                _hooked = true;
                _enabled = true;
            }
        }

        public void Stop()
        {
            if (_hooked)
                UnhookWindowsHookEx(_hookID);
            _hooked = false;
            _enabled = false;
        }

        public void SuspendOrResume()
        {
            _enabled = !_enabled;
        }

        public bool IsEnabled()
        {
            return _enabled;
        }

        public bool IsHooked()
        {
            return _hooked;
        }

        public int GetExitReason()
        {
            return _exitReason;
        }

        public bool LoadScript(string filename, string pidStr)
        {
            return false;
        }

        public List<ScriptRunner> ListScripts()
        {
            return null;
        }

        public string GetFailedReason()
        {
            return "";
        }

        public bool ValidKeyPressed(IntPtr wParam, Key key, bool isDown)
        {
            if ((isDown && wParam == (IntPtr)WM_SYSKEYDOWN)
                || (!isDown && wParam == (IntPtr)WM_SYSKEYUP)
                || (_shiftDown && (key == Key.W || key == Key.A || key == Key.D))
                || (!_ctrlDown && key != Key.W && key != Key.A && key != Key.D && key != Key.M && key != Key.NumLock && key != Key.Space))
            {
                return true;
            }
            return false;
        }

        public string GetWindowText(IntPtr hWnd)
        {
            int size = GetWindowTextLength(hWnd);
            if (size > 0)
            {
                var builder = new StringBuilder(size + 1);
                GetWindowText(hWnd, builder, builder.Capacity);
                return builder.ToString();
            }
            return string.Empty;
        }

        public IEnumerable<IntPtr> FindWindows(EnumWindowsProc filter)
        {
            IntPtr found = IntPtr.Zero;
            List<IntPtr> windows = new List<IntPtr>();
            EnumWindows(delegate (IntPtr wnd, IntPtr param)
            {
                if (filter(wnd, param))
                {
                    // only add the windows that pass the filter
                    windows.Add(wnd);
                }
                // but return true here so that we iterate all windows
                return true;
            }, IntPtr.Zero);
            return windows;
        }

        public IEnumerable<IntPtr> FindWindowsWithText(string titleText)
        {
            return FindWindows(delegate (IntPtr wnd, IntPtr param)
            {
                return GetWindowText(wnd).Contains(titleText);
            });
        }

        private IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (_windows != null)
            {
                int vkCode = Marshal.ReadInt32(lParam);
                if (nCode >= 0 && (wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_SYSKEYDOWN))
                {
                    Key key = KeyInterop.KeyFromVirtualKey(vkCode);
                    switch (key)
                    {
                        case Key.F1:
                            _exitReason = 1;
                            Stop();
                            _controllerEvent.OnSpecialKeyPressed(Key.F1);
                            break;
                        case Key.F2:
                            break;
                        case Key.F3:
                            _exitReason = 3;
                            Stop();
                            _controllerEvent.OnSpecialKeyPressed(Key.F3);
                            break;
                        case Key.F5:   // suspend mode
                            _enabled = !_enabled;
                            _controllerEvent.OnSpecialKeyPressed(Key.F5);
                            break;
                        case Key.F6:   // disable alt + key
                            _altEnabled = !_altEnabled;
                            break;
                        default:
                            if (_enabled && (_altEnabled || (!_altEnabled && wParam != (IntPtr)WM_SYSKEYDOWN)))
                            {
                                if (key == Key.LeftCtrl)
                                {
                                    _ctrlDown = true;
                                }
                                else if (key == Key.LeftShift)
                                {
                                    _shiftDown = true;
                                }
                                if (ValidKeyPressed(wParam, key, true))
                                {
                                    IntPtr result;
                                    uint msg = wParam == (IntPtr)WM_KEYDOWN ? (uint)WM_KEYDOWN : (uint)WM_SYSKEYDOWN;
                                    foreach (var window in _windows)
                                    {
                                        result = PostMessage(window, msg, vkCode, 0);
                                    }
                                }
                            }
                            break;
                    }
                }
                else if (nCode >= 0 && (wParam == (IntPtr)WM_KEYUP || wParam == (IntPtr)WM_SYSKEYUP))
                {
                    if (_enabled && (_altEnabled || (!_altEnabled && wParam != (IntPtr)WM_SYSKEYUP)))
                    {
                        Key key = KeyInterop.KeyFromVirtualKey(vkCode);
                        if (key == Key.LeftCtrl)
                        {
                            _ctrlDown = false;
                        }
                        else if (key == Key.LeftShift)
                        {
                            _shiftDown = false;
                        }
                        if (ValidKeyPressed(wParam, key, false))
                        {
                            IntPtr result;
                            uint msg = wParam == (IntPtr)WM_KEYUP ? (uint)WM_KEYUP : (uint)WM_SYSKEYUP;
                            foreach (var window in _windows)
                            {
                                result = PostMessage(window, msg, vkCode, 0);
                            }
                        }
                    }
                }
            }
            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        // =====================================================================================================================

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        // =====================================================================================================================

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        public static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern IntPtr SendMessageW(IntPtr hWnd, int uMsg, IntPtr wParam, string lParam);

        [DllImport("user32.dll")]
        public static extern IntPtr PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern IntPtr PostMessage(IntPtr hWnd, uint Msg, int wParam, int lParam);

        [DllImport("user32.dll")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder strText, int maxCount);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetWindowTextLength(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);
    }
}
