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
        private readonly BackendApiClient _apiClient;
        private AppSettings _currentSettings;

        public AppSettings? UpdatedSettings { get; private set; }
        public bool SettingsChanged { get; private set; }

        public SettingsWindow(
            SettingsService settingsService,
            AudioRecordingService audioService,
            BackendApiClient apiClient,
            AppSettings currentSettings)
        {
            InitializeComponent();

            _settingsService = settingsService;
            _audioService = audioService;
            _apiClient = apiClient;
            _currentSettings = currentSettings;

            LoadSettings();
            LoadAudioDevices();
        }

        private void LoadSettings()
        {
            // Set hotkey
            hotkeyInput.HotkeyConfig = _currentSettings.Hotkey;

            // Set backend URL
            backendUrlInput.Text = _currentSettings.BackendUrl;

            // Set options
            startWithWindowsCheck.IsChecked = _currentSettings.StartWithWindows;
            showNotificationsCheck.IsChecked = _currentSettings.ShowNotifications;
            injectTextCheck.IsChecked = _currentSettings.InjectTextAutomatically;
            fallbackClipboardCheck.IsChecked = _currentSettings.FallbackToClipboard;
            playSoundsCheck.IsChecked = _currentSettings.PlaySoundEffects;

            // Set timeout
            timeoutInput.Text = _currentSettings.TimeoutSeconds.ToString();

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

        private async void TestBackend_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                backendStatusText.Text = "Testing connection...";
                backendStatusText.Foreground = System.Windows.Media.Brushes.Gray;

                var testClient = new BackendApiClient(backendUrlInput.Text, 10);
                bool isOnline = await testClient.IsBackendOnlineAsync();

                if (isOnline)
                {
                    var health = await testClient.CheckHealthAsync();
                    backendStatusText.Text = $"✓ Connected - Model: {health.Model.ModelName} ({health.Model.Device})";
                    backendStatusText.Foreground = System.Windows.Media.Brushes.Green;

                    MessageBox.Show(
                        $"Backend is online!\n\nModel: {health.Model.ModelName}\nDevice: {health.Model.Device}\nStatus: {health.Model.Status}",
                        "Connection Test",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                else
                {
                    backendStatusText.Text = "✗ Cannot connect to backend server";
                    backendStatusText.Foreground = System.Windows.Media.Brushes.Red;

                    MessageBox.Show(
                        "Cannot connect to backend server.\n\nPlease ensure:\n1. The backend is running\n2. The URL is correct\n3. No firewall is blocking the connection",
                        "Connection Failed",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }

                testClient.Dispose();
            }
            catch (Exception ex)
            {
                Logger.Error("Backend test failed", ex);
                backendStatusText.Text = $"✗ Error: {ex.Message}";
                backendStatusText.Foreground = System.Windows.Media.Brushes.Red;

                MessageBox.Show(
                    $"Connection test failed:\n\n{ex.Message}",
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

                // Validate backend URL
                if (string.IsNullOrWhiteSpace(backendUrlInput.Text))
                {
                    MessageBox.Show(
                        "Please enter a valid backend URL.",
                        "Validation Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                // Validate timeout
                if (!int.TryParse(timeoutInput.Text, out int timeout) || timeout < 10)
                {
                    MessageBox.Show(
                        "Please enter a valid timeout (minimum 10 seconds).",
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
                    BackendUrl = backendUrlInput.Text.TrimEnd('/'),
                    StartWithWindows = startWithWindowsCheck.IsChecked ?? false,
                    ShowNotifications = showNotificationsCheck.IsChecked ?? true,
                    InjectTextAutomatically = injectTextCheck.IsChecked ?? true,
                    FallbackToClipboard = fallbackClipboardCheck.IsChecked ?? true,
                    PlaySoundEffects = playSoundsCheck.IsChecked ?? false,
                    TimeoutSeconds = timeout
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

        private void NumberValidation(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            // Only allow numbers
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }
    }
}
