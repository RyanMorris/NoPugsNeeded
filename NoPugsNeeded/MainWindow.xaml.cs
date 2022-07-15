using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using NoPugsNeeded.Controllers;
using NoPugsNeeded.Events;
using NoPugsNeeded.Settings;

namespace NoPugsNeeded
{
    public partial class MainWindow : Window
    {
        private bool _modeChosen;
        private bool _running;
        private IControl _control;
        private ControllerEvent _controllerEvent;

        public MainWindow()
        {
            InitializeComponent();
            _modeChosen = false;
            _running = false;
            _control = null;
            _controllerEvent = null;
            InitializeFollowersSettingsTab();
        }

        private void ModeChangedByGui(object sender, RoutedEventArgs e)
        {
            Cleanup();
            if (rBtn_FullControl.IsChecked == true)
            {
                label_ActiveMode.Content = "Full Control";
                _controllerEvent = new ControllerEvent();
                _controllerEvent.SpecialKeyPressed += HandleSpecialKeyPress;
                _control = new FullControlMode(_controllerEvent);
            }
            else if (rBtn_Followers.IsChecked == true)
            {
                label_ActiveMode.Content = "Followers";
                _controllerEvent = new ControllerEvent();
                _controllerEvent.SpecialKeyPressed += HandleSpecialKeyPress;
                _control = new FollowersMode(_controllerEvent);
            }
            else if (rBtn_Script.IsChecked == true)
            {
                label_ActiveMode.Content = "Script";
                _controllerEvent = new ControllerEvent();
                _controllerEvent.SpecialKeyPressed += HandleSpecialKeyPress;
                _control = new ScriptMode(_controllerEvent);
            }
            if (_running)
            {
                _control.Run();
                UpdateSuspendResume();
            }
            _modeChosen = true;
        }

        private void StartStopByGui(object sender, RoutedEventArgs e)
        {
            if (_modeChosen)
            {
                _running = !_running;
                if (_running)
                {
                    _control.Run();
                    btn_StartStop.Content = "Stop";
                    label_Status.Content = "Running";
                    label_Status.Foreground = Brushes.Green;
                }
                else
                {
                    _control.Stop();
                    btn_StartStop.Content = "Start";
                    label_Status.Content = "Stopped";
                    label_Status.Foreground = Brushes.Red;
                }
            }
        }

        private void HandleSpecialKeyPress(object sender, Key key)
        {
            switch (key)
            {
                case Key.F1:
                    rBtn_FullControl.IsChecked = true;
                    ModeChangedByCode();
                    break;
                case Key.F2:
                    rBtn_Followers.IsChecked = true;
                    ModeChangedByCode();
                    break;
                case Key.F3:
                    rBtn_Script.IsChecked = true;
                    ModeChangedByCode();
                    break;
                case Key.F5:
                    UpdateSuspendResume();
                    break;
                default:
                    break;
            }
        }

        private void ModeChangedByCode()
        {
            Cleanup();
            if (rBtn_FullControl.IsChecked == true)
            {
                label_ActiveMode.Content = "Full Control";
                _controllerEvent = new ControllerEvent();
                _controllerEvent.SpecialKeyPressed += HandleSpecialKeyPress;
                _control = new FullControlMode(_controllerEvent);
            }
            else if (rBtn_Followers.IsChecked == true)
            {
                label_ActiveMode.Content = "Followers";
                _controllerEvent = new ControllerEvent();
                _controllerEvent.SpecialKeyPressed += HandleSpecialKeyPress;
                _control = new FollowersMode(_controllerEvent);
            }
            else if (rBtn_Script.IsChecked == true)
            {
                label_ActiveMode.Content = "Script";
                _controllerEvent = new ControllerEvent();
                _controllerEvent.SpecialKeyPressed += HandleSpecialKeyPress;
                _control = new ScriptMode(_controllerEvent);
            }
            if (_running)
            {
                _control.Run();
                UpdateSuspendResume();
            }
            _modeChosen = true;
        }

        private void StartStopByCode()
        {
            if (_modeChosen)
            {
                _running = !_running;
                if (_running)
                {
                    _control.Run();
                    btn_StartStop.Content = "Stop";
                    label_Status.Content = "Running";
                    label_Status.Foreground = Brushes.Green;
                }
                else
                {
                    _control.Stop();
                    btn_StartStop.Content = "Start";
                    label_Status.Content = "Stopped";
                    label_Status.Foreground = Brushes.Red;
                }
            }
        }

        private void UpdateSuspendResume()
        {
            if (_control.IsEnabled())
            {
                label_Status.Content = "Running";
                label_Status.Foreground = Brushes.Green;
            }
            else
            {
                label_Status.Content = "Suspended";
                label_Status.Foreground = Brushes.BlueViolet;
            }
        }

        private void Cleanup()
        {
            if (_control != null)
            {
                _control.Stop();
            }
            if (_controllerEvent != null)
            {
                _controllerEvent.SpecialKeyPressed -= HandleSpecialKeyPress;
            }
            if (listview_LoadedScripts.Items != null)
            {
                listview_LoadedScripts.Items.Clear();
            }
        }

        private void LoadScript(object sender, RoutedEventArgs e)
        {
            if (rBtn_Script.IsChecked == true)
            {
                if (_control != null)
                {
                    bool loaded = _control.LoadScript(textbox_ScriptName.Text, textbox_Pid.Text);
                    if (loaded)
                    {
                        listview_LoadedScripts.Items.Add($"{textbox_ScriptName.Text} : {textbox_Pid.Text}");
                    }
                    else
                    {
                        listview_LoadedScripts.Items.Add(_control.GetFailedReason());
                    }
                }
            }
        }

        private void AddScriptsToListView()
        {
            if (_control != null && _control.ListScripts() != null)
            {
                foreach (ScriptRunner script in _control.ListScripts())
                {
                    listview_LoadedScripts.Items.Add($"{script.Name} : {script.PID}");
                }
            }
        }
    }
}
