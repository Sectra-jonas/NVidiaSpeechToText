using System;
using System.Windows;
using SpeechToTextTray.Core.Helpers;
using SpeechToTextTray.Core.Models;
using SpeechToTextTray.Core.Services;
using SpeechToTextTray.UI.TrayIcon;
using SpeechToTextTray.UI.Windows;
using SpeechToTextTray.Utils;

namespace SpeechToTextTray
{
    /// <summary>
    /// Main application class
    /// </summary>
    public partial class App : Application
    {
        // Services
        private TrayIconManager _trayIcon = null!;
        private GlobalHotkeyService _hotkeyService = null!;
        private AudioRecordingService _audioService = null!;
        private BackendApiClient _apiClient = null!;
        private TextInjectionService _textInjection = null!;
        private SettingsService _settingsService = null!;
        private TempFileManager _tempFileManager = null!;
        private NotificationHelper _notificationHelper = null!;

        // State
        private AppSettings _settings = null!;
        private RecordingState _currentState = RecordingState.Idle;
        private string _currentRecordingPath = null!;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {
                Logger.Info("Application starting...");

                // Create hidden main window (required for WPF infrastructure)
                MainWindow = new MainWindow
                {
                    Visibility = Visibility.Hidden,
                    ShowInTaskbar = false
                };

                // Initialize services
                InitializeServices();

                // Initialize tray icon
                InitializeTrayIcon();

                // Register hotkey
                RegisterHotkey();

                // Check backend health (async, don't block startup)
                CheckBackendHealthAsync();

                // Clean up old temp files and logs
                CleanupOldFiles();

                Logger.Info("Application started successfully");
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to start application", ex);
                MessageBox.Show(
                    $"Failed to start application:\n\n{ex.Message}",
                    "Startup Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Shutdown();
            }
        }

        private void InitializeServices()
        {
            // Settings
            _settingsService = new SettingsService();
            _settings = _settingsService.LoadSettings();
            Logger.Info($"Settings loaded from: {_settingsService.GetSettingsPath()}");

            // Core services
            _audioService = new AudioRecordingService();
            _apiClient = new BackendApiClient(_settings.BackendUrl, _settings.TimeoutSeconds);
            _textInjection = new TextInjectionService(_settings.FallbackToClipboard);
            _hotkeyService = new GlobalHotkeyService();
            _tempFileManager = new TempFileManager();
            _notificationHelper = new NotificationHelper(_settings.ShowNotifications);

            // Subscribe to hotkey events
            _hotkeyService.HotkeyPressed += OnHotkeyPressed;

            Logger.Info("Services initialized");
        }

        private void InitializeTrayIcon()
        {
            _trayIcon = new TrayIconManager();
            _trayIcon.Initialize();
            _trayIcon.SetState(RecordingState.Idle);

            // Subscribe to tray icon events
            _trayIcon.SettingsRequested += OnSettingsRequested;
            _trayIcon.AboutRequested += OnAboutRequested;
            _trayIcon.ExitRequested += OnExitRequested;

            Logger.Info("Tray icon initialized");
        }

        private void RegisterHotkey()
        {
            bool registered = _hotkeyService.RegisterHotkey(_settings.Hotkey.Modifiers, _settings.Hotkey.Key);

            if (registered)
            {
                Logger.Info($"Hotkey registered: {_settings.Hotkey}");
                _trayIcon?.ShowNotification(
                    "Speech-to-Text Ready",
                    $"Press {_settings.Hotkey} to start recording",
                    Hardcodet.Wpf.TaskbarNotification.BalloonIcon.Info);
            }
            else
            {
                Logger.Warning($"Failed to register hotkey: {_settings.Hotkey}");
                _trayIcon?.ShowNotification(
                    "Hotkey Registration Failed",
                    $"The hotkey {_settings.Hotkey} is already in use. Please change it in settings.",
                    Hardcodet.Wpf.TaskbarNotification.BalloonIcon.Warning);
            }
        }

        private async void CheckBackendHealthAsync()
        {
            try
            {
                bool isOnline = await _apiClient.IsBackendOnlineAsync();
                if (!isOnline)
                {
                    Logger.Warning("Backend is not reachable");
                    _trayIcon?.ShowNotification(
                        "Backend Not Available",
                        "The backend server is not running. Please start it to use transcription.",
                        Hardcodet.Wpf.TaskbarNotification.BalloonIcon.Warning);
                }
                else
                {
                    Logger.Info("Backend is online");
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Backend health check failed", ex);
            }
        }

        private void CleanupOldFiles()
        {
            try
            {
                int deletedTempFiles = _tempFileManager.CleanupOldFiles();
                if (deletedTempFiles > 0)
                {
                    Logger.Info($"Cleaned up {deletedTempFiles} old temp files");
                }

                Logger.CleanupOldLogs(7);
            }
            catch (Exception ex)
            {
                Logger.Error("Cleanup failed", ex);
            }
        }

        private async void OnHotkeyPressed(object? sender, EventArgs e)
        {
            try
            {
                await Dispatcher.InvokeAsync(async () =>
                {
                    await ToggleRecordingAsync();
                });
            }
            catch (Exception ex)
            {
                Logger.Error("Hotkey handler error", ex);
            }
        }

        private async System.Threading.Tasks.Task ToggleRecordingAsync()
        {
            try
            {
                if (_currentState == RecordingState.Recording)
                {
                    // Stop recording
                    await StopRecordingAsync();
                }
                else if (_currentState == RecordingState.Idle)
                {
                    // Start recording
                    StartRecording();
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Toggle recording error", ex);
                _currentState = RecordingState.Error;
                _trayIcon?.SetState(RecordingState.Error);
                _trayIcon?.ShowNotification(
                    "Error",
                    $"An error occurred: {ex.Message}",
                    Hardcodet.Wpf.TaskbarNotification.BalloonIcon.Error);

                // Reset to idle after error
                await System.Threading.Tasks.Task.Delay(2000);
                _currentState = RecordingState.Idle;
                _trayIcon?.SetState(RecordingState.Idle);
            }
        }

        private void StartRecording()
        {
            Logger.Info("Starting recording...");

            // Update state
            _currentState = RecordingState.Recording;
            _trayIcon?.SetState(RecordingState.Recording);

            // Create temp file
            _currentRecordingPath = _tempFileManager.CreateTempFilePath();

            // Start recording
            _audioService.StartRecording(_settings.AudioDeviceId, _currentRecordingPath);

            Logger.Info($"Recording started: {_currentRecordingPath}");
        }

        private async System.Threading.Tasks.Task StopRecordingAsync()
        {
            Logger.Info("Stopping recording...");

            // Stop recording
            _audioService.StopRecording();

            // Update state
            _currentState = RecordingState.Processing;
            _trayIcon?.SetState(RecordingState.Processing, "Transcribing...");

            try
            {
                // Transcribe
                Logger.Info("Sending audio to backend...");
                var result = await _apiClient.TranscribeAsync(_currentRecordingPath);

                Logger.Info($"Transcription received: {result.Text.Length} characters, Language: {result.Language}");

                // Inject text
                if (_settings.InjectTextAutomatically && !string.IsNullOrWhiteSpace(result.Text))
                {
                    _currentState = RecordingState.Injecting;
                    _trayIcon?.SetState(RecordingState.Injecting);

                    bool injected = _textInjection.InjectText(result.Text);

                    if (!injected && _settings.FallbackToClipboard)
                    {
                        Logger.Info("Text injection failed, copied to clipboard");
                        _trayIcon?.ShowNotification(
                            "Text Copied to Clipboard",
                            "The transcribed text has been copied to your clipboard. Press Ctrl+V to paste.",
                            Hardcodet.Wpf.TaskbarNotification.BalloonIcon.Info);
                    }
                    else
                    {
                        Logger.Info("Text injected successfully");
                    }
                }

                // Show success notification
                if (_settings.ShowNotifications)
                {
                    _trayIcon?.ShowNotification(
                        "Transcription Complete",
                        $"Text: {(result.Text.Length > 50 ? result.Text.Substring(0, 50) + "..." : result.Text)}",
                        Hardcodet.Wpf.TaskbarNotification.BalloonIcon.Info);
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Transcription failed", ex);
                _trayIcon?.ShowNotification(
                    "Transcription Failed",
                    $"Error: {ex.Message}",
                    Hardcodet.Wpf.TaskbarNotification.BalloonIcon.Error);
            }
            finally
            {
                // Clean up temp file
                _tempFileManager.DeleteFile(_currentRecordingPath);

                // Reset to idle
                _currentState = RecordingState.Idle;
                _trayIcon?.SetState(RecordingState.Idle);
            }
        }

        private void OnSettingsRequested(object? sender, EventArgs e)
        {
            try
            {
                Logger.Info("Opening settings window");

                var settingsWindow = new SettingsWindow(
                    _settingsService,
                    _audioService,
                    _apiClient,
                    _settings);

                if (settingsWindow.ShowDialog() == true && settingsWindow.SettingsChanged)
                {
                    // Settings were saved
                    if (settingsWindow.UpdatedSettings != null)
                    {
                        _settings = settingsWindow.UpdatedSettings;

                        // Update services with new settings
                        ApplyNewSettings();

                        Logger.Info("Settings updated");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error opening settings", ex);
            }
        }

        private void ApplyNewSettings()
        {
            // Re-register hotkey
            _hotkeyService.UnregisterHotkey();
            RegisterHotkey();

            // Update API client timeout
            _apiClient?.Dispose();
            _apiClient = new BackendApiClient(_settings.BackendUrl, _settings.TimeoutSeconds);

            // Update text injection service
            _textInjection = new TextInjectionService(_settings.FallbackToClipboard);

            // Update notification helper
            _notificationHelper = new NotificationHelper(_settings.ShowNotifications);

            Logger.Info("Settings applied");
        }

        private void OnAboutRequested(object? sender, EventArgs e)
        {
            MessageBox.Show(
                "Speech-to-Text Tray Application\n\n" +
                "Version 1.0.0\n\n" +
                "A Windows tray application for speech-to-text transcription\n" +
                "using NVIDIA Parakeet model.\n\n" +
                "Press your configured hotkey to start/stop recording.\n" +
                "Transcribed text will be automatically inserted into the active window.",
                "About Speech-to-Text",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void OnExitRequested(object? sender, EventArgs e)
        {
            Logger.Info("Application exit requested");

            var result = MessageBox.Show(
                "Are you sure you want to exit Speech-to-Text?",
                "Exit Confirmation",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                Shutdown();
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Logger.Info("Application shutting down...");

            // Cleanup
            _hotkeyService?.Dispose();
            _audioService?.Dispose();
            _apiClient?.Dispose();
            _trayIcon?.Dispose();

            // Clean up temp files on exit (optional)
            // _tempFileManager?.CleanupAllFiles();

            Logger.Info("Application shut down");

            base.OnExit(e);
        }
    }
}
