using System;
using Newtonsoft.Json;

namespace OlymPOS.Services
{
    public class AppSettings : IAppSettings
    {
        private readonly string _settingsKey = "app_settings";
        private SettingsModel _settings;

        public string RemoteConnectionString { get; }

        public string LocalConnectionString => "olympos_cache.db3";

        public bool UseOfflineMode
        {
            get => _settings.UseOfflineMode;
            set
            {
                _settings.UseOfflineMode = value;
                SaveSettings();
            }
        }

        public bool AutoSyncEnabled
        {
            get => _settings.AutoSyncEnabled;
            set
            {
                _settings.AutoSyncEnabled = value;
                SaveSettings();
            }
        }

        public TimeSpan SyncInterval
        {
            get => _settings.SyncInterval;
            set
            {
                _settings.SyncInterval = value;
                SaveSettings();
            }
        }

        public AppSettings()
        {
            // Load settings from preferences
            _settings = LoadSettings();

            // Set the connection string from the global setting
            RemoteConnectionString = GlobalConString.ConnStr;
        }

        private SettingsModel LoadSettings()
        {
            var settingsJson = Preferences.Get(_settingsKey, null);

            if (string.IsNullOrEmpty(settingsJson))
            {
                // Default settings
                return new SettingsModel
                {
                    UseOfflineMode = false,
                    AutoSyncEnabled = true,
                    SyncInterval = TimeSpan.FromMinutes(15)
                };
            }

            try
            {
                return JsonConvert.DeserializeObject<SettingsModel>(settingsJson);
            }
            catch
            {
                // If deserialization fails, return default settings
                return new SettingsModel
                {
                    UseOfflineMode = false,
                    AutoSyncEnabled = true,
                    SyncInterval = TimeSpan.FromMinutes(15)
                };
            }
        }

        private void SaveSettings()
        {
            var settingsJson = JsonConvert.SerializeObject(_settings);
            Preferences.Set(_settingsKey, settingsJson);
        }

        private class SettingsModel
        {
            public bool UseOfflineMode { get; set; }
            public bool AutoSyncEnabled { get; set; }
            public TimeSpan SyncInterval { get; set; }
        }
    }
}
