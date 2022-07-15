using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Input;

namespace NoPugsNeeded.Controllers
{
    public class ScriptRunnerHelper
    {
        private string _name;
        private IntPtr _window;
        private int _pid;
        private List<string> _stringCommandList;

        public ScriptRunnerHelper()
        { }

        public ScriptRunnerHelper(string name, IntPtr window, int pid, List<string> stringCommandList)
        {
            _name = name;
            _window = window;
            _pid = pid;
            _stringCommandList = stringCommandList;
        }

        public string Name
        {
            get => _name;
            set => _name = value;
        }

        public IntPtr Window
        {
            get => _window;
            set => _window = value;
        }

        public int PID
        {
            get => _pid;
            set => _pid = value;
        }

        public List<string> StringCommandList
        {
            get => _stringCommandList;
            set => _stringCommandList = value;
        }
    }

    public class ScriptRunner
    {
        private string _name;
        private IntPtr _window;
        private int _pid;
        private List<string> _stringCommandList;
        private BackgroundWorker _worker;

        private static int WM_KEYDOWN = 0x0100;
        private static int WM_KEYUP = 0x0101;

        public ScriptRunner()
        {
            _stringCommandList = new List<string>();
        }

        public ScriptRunner(string name)
        {
            _name = name;
            _stringCommandList = new List<string>();
        }

        ~ScriptRunner()
        {
            _worker.CancelAsync();
        }

        public string Name
        {
            get => _name;
            set => _name = value;
        }

        public IntPtr Window
        {
            get => _window;
            set => _window = value;
        }

        public int PID
        {
            get => _pid;
            set => _pid = value;
        }

        public void AddCommand(string command)
        {
            _stringCommandList.Add(command);
        }

        public void InitializeWorkerAndCommands()
        {
            _worker = new BackgroundWorker();
            _worker.WorkerSupportsCancellation = true;
            _worker.DoWork += RunNextCommand;
        }

        public void StartRunningAsync()
        {
            ScriptRunnerHelper helper = new ScriptRunnerHelper(_name, _window, _pid, _stringCommandList);
            _worker.RunWorkerAsync(helper);
        }

        public void StopRunning()
        {
            _worker.CancelAsync();
        }

        private static void RunNextCommand(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;
            // is this CancellationPending needed here?
            if (worker.CancellationPending)
            {
                e.Cancel = true;
                return;
            }
            ScriptRunnerHelper helper = (ScriptRunnerHelper)e.Argument;
            if (helper == null || helper.StringCommandList == null || helper.StringCommandList.Count < 1)
                return;
            int i = 0;
            float floatParse;
            string upper;
            Key keyParse;
            while (true)
            {
                if (worker.CancellationPending)
                {
                    e.Cancel = true;
                    break;
                }
                else
                {
                    string mainCommand = helper.StringCommandList[i];
                    if (mainCommand == "REPEAT" || mainCommand == "repeat")
                    {
                        i = 0;
                    }
                    else
                    {
                        string[] multiCommands = mainCommand.Split(' ');
                        for (int j = 0; j < multiCommands.Length; j++)
                        {
                            upper = multiCommands[j].ToUpper();
                            if (upper == "WAIT")
                            {
                                if (float.TryParse(multiCommands[++j], out floatParse))
                                {
                                    Thread.Sleep((int)(floatParse * 1000));
                                }
                            }
                            else
                            {
                                if (Enum.TryParse(upper, out keyParse))
                                {
                                    int keyCode = KeyInterop.VirtualKeyFromKey(keyParse);
                                    IntPtr result = PostMessage(helper.Window, (uint)WM_KEYDOWN, keyCode, 0);
                                    Thread.Sleep(50);
                                    result = PostMessage(helper.Window, (uint)WM_KEYUP, keyCode, 0);
                                }
                            }
                        }
                        i++;
                    }
                }
            }
        }

        [DllImport("user32.dll")]
        public static extern IntPtr PostMessage(IntPtr hWnd, uint Msg, int wParam, int lParam);
    }
}