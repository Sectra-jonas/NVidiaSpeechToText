using System;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Navigation;
using SpeechToTextTray.Core.Models;
using SpeechToTextTray.Core.Services;
using SpeechToTextTray.Core.Services.Transcription;
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
        private TranscriptionProvider _selectedProvider;

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

            // Load transcription settings
            _selectedProvider = _currentSettings.Transcription.Provider;

            if (_selectedProvider == TranscriptionProvider.Local)
            {
                localProviderRadio.IsChecked = true;
            }
            else if (_selectedProvider == TranscriptionProvider.Azure)
            {
                azureProviderRadio.IsChecked = true;
                LoadAzureSettings(_currentSettings.Transcription.Azure);
            }

            UpdateProviderPanelVisibility();
        }

        private void LoadAzureSettings(AzureTranscriptionConfig config)
        {
            if (config == null) return;

            // Set subscription key
            azureKeyInput.Text = config.SubscriptionKey;

            // Select region
            foreach (ComboBoxItem item in azureRegionCombo.Items)
            {
                if (item.Tag?.ToString() == config.Region)
                {
                    azureRegionCombo.SelectedItem = item;
                    break;
                }
            }

            // Select language
            if (!string.IsNullOrEmpty(config.Language))
            {
                foreach (ComboBoxItem item in azureLanguageCombo.Items)
                {
                    if (item.Tag?.ToString() == config.Language)
                    {
                        azureLanguageCombo.SelectedItem = item;
                        break;
                    }
                }
            }
            else
            {
                azureLanguageCombo.SelectedIndex = 0; // Auto-detect
            }
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
                    PlaySoundEffects = playSoundsCheck.IsChecked ?? false,
                    Transcription = new TranscriptionConfig
                    {
                        Provider = _selectedProvider,
                        Local = _currentSettings.Transcription.Local, // Keep existing local config
                        Azure = _selectedProvider == TranscriptionProvider.Azure
                            ? CreateAzureConfigFromUI()
                            : _currentSettings.Transcription.Azure
                    }
                };

                // Validate transcription configuration
                var validation = TranscriptionServiceFactory.ValidateConfiguration(UpdatedSettings.Transcription);
                if (!validation.IsValid)
                {
                    MessageBox.Show(
                        $"Transcription configuration error:\n\n{validation.ErrorMessage}",
                        "Validation Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

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

        private void OnProviderChanged(object sender, RoutedEventArgs e)
        {
            if (localProviderRadio.IsChecked == true)
            {
                _selectedProvider = TranscriptionProvider.Local;
            }
            else if (azureProviderRadio.IsChecked == true)
            {
                _selectedProvider = TranscriptionProvider.Azure;
            }

            UpdateProviderPanelVisibility();
        }

        private void UpdateProviderPanelVisibility()
        {
            if (azureConfigPanel != null)
            {
                azureConfigPanel.Visibility = _selectedProvider == TranscriptionProvider.Azure
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }
        }

        private async void TestAzureConnection_Click(object sender, RoutedEventArgs e)
        {
            // Disable button during test
            testConnectionButton.IsEnabled = false;
            azureStatusText.Text = "Testing...";
            azureStatusIndicator.Fill = new SolidColorBrush(Colors.Orange);

            try
            {
                var testConfig = CreateAzureConfigFromUI();

                // Validate configuration first
                var validation = TranscriptionServiceFactory.ValidateConfiguration(
                    new TranscriptionConfig { Provider = TranscriptionProvider.Azure, Azure = testConfig });

                if (!validation.IsValid)
                {
                    azureStatusText.Text = "Configuration invalid";
                    azureStatusIndicator.Fill = new SolidColorBrush(Colors.Red);
                    MessageBox.Show(
                        $"Configuration invalid:\n\n{validation.ErrorMessage}",
                        "Validation Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                // Try to create service
                using (var testService = new AzureTranscriptionService(testConfig))
                {
                    var serviceValidation = await testService.ValidateConfigurationAsync();

                    if (serviceValidation.IsValid)
                    {
                        azureStatusText.Text = "Connection successful";
                        azureStatusIndicator.Fill = new SolidColorBrush(Colors.Green);
                        MessageBox.Show(
                            "Azure Speech Service connection successful!",
                            "Test Connection",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                    }
                    else
                    {
                        azureStatusText.Text = "Connection failed";
                        azureStatusIndicator.Fill = new SolidColorBrush(Colors.Red);
                        MessageBox.Show(
                            $"Connection failed:\n\n{serviceValidation.ErrorMessage}",
                            "Test Connection",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Azure connection test failed", ex);
                azureStatusText.Text = "Test failed";
                azureStatusIndicator.Fill = new SolidColorBrush(Colors.Red);
                MessageBox.Show(
                    $"Test failed:\n\n{ex.Message}",
                    "Test Connection",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                testConnectionButton.IsEnabled = true;
            }
        }

        private AzureTranscriptionConfig CreateAzureConfigFromUI()
        {
            var language = (azureLanguageCombo.SelectedItem as ComboBoxItem)?.Tag?.ToString();

            return new AzureTranscriptionConfig
            {
                SubscriptionKey = azureKeyInput.Text.Trim(),
                Region = (azureRegionCombo.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "eastus",
                Language = string.IsNullOrWhiteSpace(language) ? null : language
            };
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = e.Uri.AbsoluteUri,
                    UseShellExecute = true
                });
                e.Handled = true;
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to open hyperlink", ex);
            }
        }
    }
}
