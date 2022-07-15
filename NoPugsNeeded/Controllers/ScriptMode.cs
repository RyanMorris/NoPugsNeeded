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
    public class ScriptMode : IControl
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
        private bool _scriptRunning;
        private bool _scriptLoaded;
        private bool _altEnabled;
        private bool _shiftDown;
        private bool _ctrlDown;
        private int _exitReason;
        private List<ScriptRunner> _scripts;
        private ControllerEvent _controllerEvent;
        private string _failedReason;

        public ScriptMode(ControllerEvent controllerEvent)
        {
            _proc = HookCallback;
            _hookID = IntPtr.Zero;
            _hooked = false;
            _enabled = true;
            _scriptLoaded = false;
            _scriptRunning = false;
            _altEnabled = false;
            _shiftDown = false;
            _ctrlDown = false;
            _exitReason = 0;
            _scripts = new List<ScriptRunner>();
            _controllerEvent = controllerEvent;
            _failedReason = "";
        }

        ~ScriptMode()
        {
            Stop();
        }

        public void Run()
        {
            _windows = FindWindowsWithText("World of Warcraft");
            if (_windows.Count() > 0)
            {
                _hookID = SetHook(_proc);
                RunScripts();
                _hooked = true;
                _enabled = true;
            }
        }

        public void Stop()
        {
            if (_hooked)
                UnhookWindowsHookEx(_hookID);
            StopScripts();
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

        public List<ScriptRunner> ListScripts()
        {
            return _scripts;
        }

        public string GetFailedReason()
        {
            return _failedReason;
        }

        public bool ValidKeyPressed(IntPtr wParam, Key key, bool isDown)
        {
            if ((isDown && wParam == (IntPtr)WM_SYSKEYDOWN)
                || (!isDown && wParam == (IntPtr)WM_SYSKEYUP)
                || key == Key.Up || key == Key.Down || key == Key.Left || key == Key.Right)
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
                            _exitReason = 2;
                            Stop();
                            _controllerEvent.OnSpecialKeyPressed(Key.F2);
                            break;
                        case Key.F3:
                            break;
                        case Key.F5:   // suspend/run script/mode
                            if (_scriptLoaded)
                            {
                                _scriptRunning = !_scriptRunning;
                                foreach (ScriptRunner script in _scripts)
                                {
                                    if (_scriptRunning)
                                    {
                                        script.StartRunningAsync();
                                    }
                                    else
                                    {
                                        script.StopRunning();
                                    }
                                }
                            }
                            _enabled = !_enabled;
                            _controllerEvent.OnSpecialKeyPressed(Key.F5);
                            //else
                            //{
                            //    _enabled = !_enabled;
                            //}
                            break;
                        case Key.F6:   // disable alt + key
                            //_altEnabled = !_altEnabled;
                            break;
                        //case Key.OemPeriod:    // load script
                        //    LoadScript();
                        //    break;
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
                                        //uint pid;
                                        //uint garbage = GetWindowThreadProcessId(window, out pid);
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

        public bool LoadScript(string filename, string pidStr)
        {
            if (!string.IsNullOrEmpty(filename))
            {
                string path = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), filename);
                if (!System.IO.File.Exists(path))
                {
                    _failedReason = "file does not exist";
                    return false;
                }
                string[] lines = System.IO.File.ReadAllLines(path);
                if (lines != null && lines.Length > 0)
                {
                    ScriptRunner script = new ScriptRunner(filename);
                    foreach (string line in lines)
                    {
                        script.AddCommand(line);
                    }
                    if (!string.IsNullOrEmpty(pidStr))
                    {
                        int pidInt;
                        if (int.TryParse(pidStr, out pidInt))
                        {
                            Process p = Process.GetProcessById(pidInt);
                            if (p != null)
                            {
                                script.PID = pidInt;
                                script.Window = p.MainWindowHandle;
                            }
                        }
                    }
                    script.InitializeWorkerAndCommands();
                    _scripts.Add(script);
                    _scriptLoaded = true;
                    return true;
                }
                else
                {
                    _failedReason = "file has no lines";
                    return false;
                }
            }
            _failedReason = "file name null or empty";
            return false;
        }

        private void RunScripts()
        {
            if (_scriptLoaded)
            {
                if (_scriptRunning)
                {
                    foreach (ScriptRunner script in _scripts)
                    {
                        script.StopRunning();
                    }
                }
                _scriptRunning = true;
                foreach (ScriptRunner script in _scripts)
                {
                    script.StartRunningAsync();
                }
            }
        }

        private void StopScripts()
        {
            foreach (ScriptRunner script in _scripts)
            {
                script.StopRunning();
            }
            _scriptRunning = false;
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

        [DllImport("user32.dll", SetLastError = true)]
        static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);
    }
}