using System;
using System.IO;
using System.Text.Json;
using NoPugsNeeded.Enums;

namespace NoPugsNeeded.Settings
{
    public class SettingsController
    {
        private const string FOLLOWERS_SETTINGS_FILE_NAME = "FollowersSettings.json";
        public string ErrorMessage { get; set; }

        public bool WriteSettingsFile(FollowersSettings followersSettings)
        {
            try
            {
                ErrorMessage = string.Empty;
                string jsonString = JsonSerializer.Serialize(followersSettings);
                File.WriteAllText(FOLLOWERS_SETTINGS_FILE_NAME, jsonString);
                return true;
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return false;
            }
        }

        public FollowersSettings ReadSettingsFile(SettingsEnum settingsEnum)
        {
            try
            {
                ErrorMessage = string.Empty;
                switch (settingsEnum)
                {
                    case SettingsEnum.Followers:
                        string fileContent = File.ReadAllText(FOLLOWERS_SETTINGS_FILE_NAME);
                        FollowersSettings followersSettings = JsonSerializer.Deserialize<FollowersSettings>(fileContent);
                        return followersSettings;
                    default:
                        return null;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return null;
            }
        }
    }
}
