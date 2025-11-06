using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using SpeechToTextTray.Core.Models;
using SpeechToTextTray.Core.Services;
using SpeechToTextTray.Utils;

namespace SpeechToTextTray.UI.Windows
{
    /// <summary>
    /// Settings window for configuring the application
    /// </summary>
    public partial class SettingsWindow : Window
    {
        private readonly SettingsService _settingsService;
        private readonly AudioRecordingService _audioService;
        private AppSettings _currentSettings;

        public AppSettings? UpdatedSettings { get; private set; }
        public bool SettingsChanged { get; private set; }

        public SettingsWindow(
            SettingsService settingsService,
            AudioRecordingService audioService,
            AppSettings currentSettings)
        {
            InitializeComponent();

            _settingsService = settingsService;
            _audioService = audioService;
            _currentSettings = currentSettings;

            LoadSettings();
            LoadAudioDevices();
        }

        private void LoadSettings()
        {
            // Set hotkey
            hotkeyInput.HotkeyConfig = _currentSettings.Hotkey;

            // Set options
            startWithWindowsCheck.IsChecked = _currentSettings.StartWithWindows;
            showNotificationsCheck.IsChecked = _currentSettings.ShowNotifications;
            injectTextCheck.IsChecked = _currentSettings.InjectTextAutomatically;
            fallbackClipboardCheck.IsChecked = _currentSettings.FallbackToClipboard;
            playSoundsCheck.IsChecked = _currentSettings.PlaySoundEffects;

            // Enable/disable fallback option based on inject text option
            fallbackClipboardCheck.IsEnabled = injectTextCheck.IsChecked ?? false;
            injectTextCheck.Checked += (s, e) => fallbackClipboardCheck.IsEnabled = true;
            injectTextCheck.Unchecked += (s, e) => fallbackClipboardCheck.IsEnabled = false;
        }

        private void LoadAudioDevices()
        {
            try
            {
                var devices = _audioService.GetAvailableDevices();
                audioDeviceCombo.ItemsSource = devices;

                // Select current device
                if (!string.IsNullOrEmpty(_currentSettings.AudioDeviceId))
                {
                    var selectedDevice = devices.FirstOrDefault(d => d.Id == _currentSettings.AudioDeviceId);
                    if (selectedDevice != null)
                    {
                        audioDeviceCombo.SelectedItem = selectedDevice;
                    }
                    else if (devices.Any())
                    {
                        audioDeviceCombo.SelectedIndex = 0;
                    }
                }
                else if (devices.Any())
                {
                    audioDeviceCombo.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to load audio devices", ex);
                MessageBox.Show(
                    "Failed to load audio devices. Please check your system audio settings.",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Validate hotkey
                if (hotkeyInput.HotkeyConfig == null)
                {
                    MessageBox.Show(
                        "Please set a valid hotkey combination.",
                        "Validation Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                // Create updated settings
                UpdatedSettings = new AppSettings
                {
                    Hotkey = hotkeyInput.HotkeyConfig,
                    AudioDeviceId = (audioDeviceCombo.SelectedItem as AudioDevice)?.Id ?? "default",
                    StartWithWindows = startWithWindowsCheck.IsChecked ?? false,
                    ShowNotifications = showNotificationsCheck.IsChecked ?? true,
                    InjectTextAutomatically = injectTextCheck.IsChecked ?? true,
                    FallbackToClipboard = fallbackClipboardCheck.IsChecked ?? true,
                    PlaySoundEffects = playSoundsCheck.IsChecked ?? false
                };

                // Save to file
                _settingsService.SaveSettings(UpdatedSettings);

                SettingsChanged = true;
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to save settings", ex);
                MessageBox.Show(
                    $"Failed to save settings:\n\n{ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            SettingsChanged = false;
            DialogResult = false;
            Close();
        }

        private void Reset_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Are you sure you want to reset all settings to defaults?",
                "Reset Settings",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                _currentSettings = _settingsService.GetDefaultSettings();
                LoadSettings();
            }
        }
    }
}
