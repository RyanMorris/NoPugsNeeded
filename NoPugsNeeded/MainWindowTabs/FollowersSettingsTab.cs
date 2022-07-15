using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using NoPugsNeeded.Controllers;
using NoPugsNeeded.Events;
using NoPugsNeeded.Settings;
using NoPugsNeeded.Enums;

namespace NoPugsNeeded
{
    public partial class MainWindow : Window
    {
        private FollowersSettings _followersSettings;

        private void InitializeFollowersSettingsTab()
        {
            _followersSettings = LoadFollowersSettings();
        }

        private FollowersSettings LoadFollowersSettings()
        {
            SettingsController settingsController = new SettingsController();
            FollowersSettings followersSettings = settingsController.ReadSettingsFile(SettingsEnum.Followers);
            if (followersSettings == null)
            {
                followersSettings = new FollowersSettings();
            }
            checkbox_EnableSpacebar.IsChecked = followersSettings.EnableSpacebar;
            return followersSettings;
        }

        private void SaveFollowersSettings(object sender, RoutedEventArgs e)
        {
            SettingsController settingsController = new SettingsController();
            _followersSettings.EnableSpacebar = checkbox_EnableSpacebar.IsChecked ?? false;
            if (!settingsController.WriteSettingsFile(_followersSettings))
            {
                string error = settingsController.ErrorMessage;
            }
        }
    }
}
