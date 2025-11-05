using System;
using System.IO;
using System.Text.Json;
using SpeechToTextTray.Core.Models;

namespace SpeechToTextTray.Core.Services
{
    /// <summary>
    /// Service for managing application settings persistence
    /// </summary>
    public class SettingsService
    {
        private const string SettingsFileName = "settings.json";
        private readonly string _settingsDirectory;
        private readonly string _settingsPath;

        /// <summary>
        /// Event fired when settings are changed
        /// </summary>
        public event EventHandler<AppSettings>? SettingsChanged;

        public SettingsService()
        {
            _settingsDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "SpeechToTextTray"
            );
            _settingsPath = Path.Combine(_settingsDirectory, SettingsFileName);

            // Ensure directory exists
            Directory.CreateDirectory(_settingsDirectory);
        }

        /// <summary>
        /// Load settings from file, or return defaults if file doesn't exist
        /// </summary>
        public AppSettings LoadSettings()
        {
            try
            {
                if (File.Exists(_settingsPath))
                {
                    var json = File.ReadAllText(_settingsPath);
                    var settings = JsonSerializer.Deserialize<AppSettings>(json);
                    return settings ?? GetDefaultSettings();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading settings: {ex.Message}");
                // Fall through to return defaults
            }

            return GetDefaultSettings();
        }

        /// <summary>
        /// Save settings to file
        /// </summary>
        public void SaveSettings(AppSettings settings)
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };

                var json = JsonSerializer.Serialize(settings, options);
                File.WriteAllText(_settingsPath, json);

                // Notify listeners
                SettingsChanged?.Invoke(this, settings);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to save settings: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Get default settings
        /// </summary>
        public AppSettings GetDefaultSettings()
        {
            return new AppSettings();
        }

        /// <summary>
        /// Get the settings file path (for debugging/display purposes)
        /// </summary>
        public string GetSettingsPath() => _settingsPath;

        /// <summary>
        /// Reset settings to defaults
        /// </summary>
        public void ResetToDefaults()
        {
            var defaults = GetDefaultSettings();
            SaveSettings(defaults);
        }

        /// <summary>
        /// Check if settings file exists
        /// </summary>
        public bool SettingsFileExists() => File.Exists(_settingsPath);
    }
}
