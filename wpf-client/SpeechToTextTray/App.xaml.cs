using System;
using System.Linq;
using System.Threading;
using System.Windows;
using SpeechToTextTray.Core.Helpers;
using SpeechToTextTray.Core.Models;
using SpeechToTextTray.Core.Services;
using SpeechToTextTray.Core.Services.Transcription;
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
        private ITranscriptionService _transcriptionService = null!;
        private TextInjectionService _textInjection = null!;
        private SettingsService _settingsService = null!;
        private TempFileManager _tempFileManager = null!;
        private NotificationHelper _notificationHelper = null!;
        private SpeechMikeService? _speechMikeService;
        private RecordingOverlayWindow? _overlayWindow;

        // State
        private AppSettings _settings = null!;
        private RecordingState _currentState = RecordingState.Idle;
        private string _currentRecordingPath = null!;

        // Synchronization for preventing race conditions during recording toggle
        private readonly SemaphoreSlim _stateLock = new SemaphoreSlim(1, 1);

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
            _textInjection = new TextInjectionService(_settings.FallbackToClipboard);
            _hotkeyService = new GlobalHotkeyService();
            _tempFileManager = new TempFileManager();
            _notificationHelper = new NotificationHelper(_settings.ShowNotifications);

            // Initialize transcription service using factory
            try
            {
                _transcriptionService = TranscriptionServiceFactory.Create(_settings.Transcription);
                var providerInfo = _transcriptionService.GetProviderInfo();
                Logger.Info($"Transcription service initialized: {providerInfo.ProviderName} ({providerInfo.Status})");
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to initialize transcription service", ex);
                throw new InvalidOperationException($"Failed to initialize transcription service: {ex.Message}", ex);
            }

            // Initialize audio device (pre-opens device to eliminate capture delay)
            string actualDeviceId = _audioService.Initialize(_settings.AudioDeviceId);

            // Check if fallback occurred
            if (actualDeviceId != _settings.AudioDeviceId)
            {
                Logger.Warning($"Configured audio device '{_settings.AudioDeviceId}' not available. Using device '{actualDeviceId}' instead.");

                // Update settings with actual device
                _settings.AudioDeviceId = actualDeviceId;
                _settingsService.SaveSettings(_settings);

                // Get device name for user notification
                var devices = _audioService.GetAvailableDevices();
                var actualDevice = devices.FirstOrDefault(d => d.Id == actualDeviceId);
                string deviceName = actualDevice?.Name ?? $"Device {actualDeviceId}";

                // Notify user after tray icon is initialized (we'll do this after InitializeTrayIcon)
                System.Threading.Tasks.Task.Delay(1000).ContinueWith(_ =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        _trayIcon?.ShowNotification(
                            "Audio Device Changed",
                            $"The previously configured audio device is no longer available. Using: {deviceName}",
                            Hardcodet.Wpf.TaskbarNotification.BalloonIcon.Warning);
                    });
                });
            }

            Logger.Info($"Audio device initialized: {actualDeviceId}");

            // Subscribe to audio capture events
            _audioService.CaptureStarted += OnCaptureStarted;

            // Subscribe to hotkey events
            _hotkeyService.HotkeyPressed += OnHotkeyPressed;

            // Initialize SpeechMike if enabled
            if (_settings.EnableSpeechMike)
            {
                _speechMikeService = new SpeechMikeService();
                if (_speechMikeService.Initialize())
                {
                    _speechMikeService.RecordingAction += OnSpeechMikeAction;
                    Logger.Info("SpeechMike integration enabled");
                }
                else
                {
                    _speechMikeService = null;
                    Logger.Warning("SpeechMike enabled in settings but device not available");
                }
            }

            // Initialize recording overlay window
            _overlayWindow = new RecordingOverlayWindow(_settingsService);
            Logger.Info("Recording overlay window initialized");

            Logger.Info($"Services initialized ({_settings.Transcription.Provider} transcription)");
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
                // NHotkey.Wpf already invokes on UI thread, no Dispatcher needed
                await ToggleRecordingAsync();
            }
            catch (Exception ex)
            {
                Logger.Error("Hotkey handler error", ex);
            }
        }

        private void OnCaptureStarted(object? sender, EventArgs e)
        {
            try
            {
                // Audio events come from background thread - marshal to UI thread
                Dispatcher.Invoke(() =>
                {
                    Logger.Info("Audio capture started - showing indicators");

                    // Update tray icon to show recording state
                    _trayIcon?.SetState(RecordingState.Recording);

                    // Turn on SpeechMike LED indicator
                    _speechMikeService?.SetRecordingIndicator(true);

                    // Show recording overlay
                    if (_settings.ShowRecordingOverlay && _overlayWindow != null)
                    {
                        // Get current device and provider info
                        var devices = _audioService.GetAvailableDevices();
                        var device = devices.FirstOrDefault(d => d.Id == _settings.AudioDeviceId);
                        var providerInfo = _transcriptionService.GetProviderInfo();

                        // Update and show overlay
                        _overlayWindow.UpdateInfo(
                            device?.Name ?? "Unknown Device",
                            providerInfo.ProviderName
                        );
                        _overlayWindow.ShowAtPosition(_settings.RecordingOverlayX, _settings.RecordingOverlayY);
                    }
                });
            }
            catch (Exception ex)
            {
                Logger.Error("CaptureStarted handler error", ex);
            }
        }

        private async void OnSpeechMikeAction(object? sender, RecordingActionEventArgs e)
        {
            try
            {
                // SpeechMike button events come from COM on a background thread
                // Must marshal to UI thread for WPF operations
                await Dispatcher.InvokeAsync(async () =>
                {
                    if (e.StartRecording)
                    {
                        // Start recording if idle
                        if (_currentState == RecordingState.Idle)
                        {
                            await ToggleRecordingAsync();
                        }
                        else
                        {
                            Logger.Info($"SpeechMike Record pressed but cannot start - current state: {_currentState}");
                        }
                    }
                    else
                    {
                        // Stop recording if currently recording
                        if (_currentState == RecordingState.Recording)
                        {
                            await ToggleRecordingAsync();
                        }
                        else
                        {
                            Logger.Info($"SpeechMike Stop triggered but not recording - current state: {_currentState}");
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                Logger.Error("SpeechMike action handler error", ex);
            }
        }

        private async System.Threading.Tasks.Task ToggleRecordingAsync()
        {
            // Non-blocking lock attempt - prevents race conditions from rapid hotkey presses
            if (!await _stateLock.WaitAsync(0))
            {
                Logger.Warning("Recording operation already in progress, ignoring hotkey press");
                return;
            }

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
                else
                {
                    Logger.Warning($"Cannot toggle recording in current state: {_currentState}");
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
            finally
            {
                // Always release the lock
                _stateLock.Release();
            }
        }

        private void StartRecording()
        {
            Logger.Info("Starting recording...");

            // Update internal state
            _currentState = RecordingState.Recording;

            // Create temp file
            _currentRecordingPath = _tempFileManager.CreateTempFilePath();

            // Start recording (device already initialized)
            // Visual indicators (tray icon, SpeechMike LED, overlay) will be shown
            // when OnCaptureStarted event fires (~50ms later when audio actually starts)
            _audioService.StartRecording(_currentRecordingPath);

            Logger.Info($"Recording started: {_currentRecordingPath}");
        }

        private async System.Threading.Tasks.Task StopRecordingAsync()
        {
            Logger.Info("Stopping recording...");

            // Stop recording and wait for file to be fully written
            await _audioService.StopRecordingAsync();

            // File is now guaranteed to be finalized and ready for transcription

            // Update state
            _currentState = RecordingState.Processing;
            _trayIcon?.SetState(RecordingState.Processing, "Transcribing...");

            try
            {
                // Transcribe using selected provider
                var providerInfo = _transcriptionService.GetProviderInfo();
                Logger.Info($"Transcribing audio using {providerInfo.ProviderName}...");
                var result = await _transcriptionService.TranscribeAsync(_currentRecordingPath);

                // Check if transcription was successful
                if (!result.Success)
                {
                    // Transcription failed
                    string errorMessage = result.ErrorMessage ?? "Unknown error occurred during transcription";
                    Logger.Error($"Transcription failed: {errorMessage}");

                    // Log exception details if available
                    if (result.Exception != null)
                    {
                        Logger.Error("Transcription exception details:", result.Exception);
                    }

                    // Notify user of failure
                    _trayIcon?.ShowNotification(
                        "Transcription Failed",
                        errorMessage,
                        Hardcodet.Wpf.TaskbarNotification.BalloonIcon.Error);

                    return; // Exit early, don't try to inject text
                }

                // Transcription succeeded
                Logger.Info($"Transcription successful: {result.Text.Length} characters, Language: {result.Language}");

                // Check if we got any text
                if (string.IsNullOrWhiteSpace(result.Text))
                {
                    Logger.Warning("Transcription returned empty text (no speech detected)");
                    _trayIcon?.ShowNotification(
                        "No Speech Detected",
                        "The transcription service did not detect any speech in the recording.",
                        Hardcodet.Wpf.TaskbarNotification.BalloonIcon.Warning);
                    return; // Exit early, nothing to inject
                }

                // Inject text
                if (_settings.InjectTextAutomatically)
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

                // Turn off SpeechMike LED indicator
                _speechMikeService?.SetRecordingIndicator(false);

                // Hide recording overlay
                _overlayWindow?.Hide();

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

            // Update audio device if changed
            string actualDeviceId = _audioService.ChangeDevice(_settings.AudioDeviceId);

            // Check if fallback occurred
            if (actualDeviceId != _settings.AudioDeviceId)
            {
                Logger.Warning($"Requested audio device '{_settings.AudioDeviceId}' not available. Using device '{actualDeviceId}' instead.");

                // Update settings with actual device
                _settings.AudioDeviceId = actualDeviceId;
                _settingsService.SaveSettings(_settings);

                // Get device name for user notification
                var devices = _audioService.GetAvailableDevices();
                var actualDevice = devices.FirstOrDefault(d => d.Id == actualDeviceId);
                string deviceName = actualDevice?.Name ?? $"Device {actualDeviceId}";

                _trayIcon?.ShowNotification(
                    "Audio Device Changed",
                    $"The selected audio device is no longer available. Using: {deviceName}",
                    Hardcodet.Wpf.TaskbarNotification.BalloonIcon.Warning);
            }

            Logger.Info($"Audio device updated: {actualDeviceId}");

            // Update text injection service
            _textInjection = new TextInjectionService(_settings.FallbackToClipboard);

            // Update notification helper
            _notificationHelper = new NotificationHelper(_settings.ShowNotifications);

            // Update SpeechMike service based on settings
            if (_settings.EnableSpeechMike && _speechMikeService == null)
            {
                // Enable SpeechMike
                _speechMikeService = new SpeechMikeService();
                if (_speechMikeService.Initialize())
                {
                    _speechMikeService.RecordingAction += OnSpeechMikeAction;
                    Logger.Info("SpeechMike integration enabled via settings");
                    _trayIcon?.ShowNotification(
                        "SpeechMike Enabled",
                        "SpeechMike device is now active for recording",
                        Hardcodet.Wpf.TaskbarNotification.BalloonIcon.Info);
                }
                else
                {
                    _speechMikeService = null;
                    Logger.Warning("Failed to enable SpeechMike - device not available");
                    _trayIcon?.ShowNotification(
                        "SpeechMike Not Available",
                        "No SpeechMike device found. Please connect device and try again.",
                        Hardcodet.Wpf.TaskbarNotification.BalloonIcon.Warning);
                }
            }
            else if (!_settings.EnableSpeechMike && _speechMikeService != null)
            {
                // Disable SpeechMike
                _speechMikeService.RecordingAction -= OnSpeechMikeAction;
                _speechMikeService.Dispose();
                _speechMikeService = null;
                Logger.Info("SpeechMike integration disabled via settings");
            }

            // Recreate transcription service if provider changed
            ITranscriptionService? oldService = null;
            try
            {
                oldService = _transcriptionService;
                var newService = TranscriptionServiceFactory.Create(_settings.Transcription);

                // Only update reference if creation succeeded
                _transcriptionService = newService;

                var providerInfo = _transcriptionService.GetProviderInfo();
                Logger.Info($"Transcription service updated: {providerInfo.ProviderName}");
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to update transcription service", ex);
                _trayIcon?.ShowNotification(
                    "Transcription Service Error",
                    $"Failed to update transcription service: {ex.Message}",
                    Hardcodet.Wpf.TaskbarNotification.BalloonIcon.Error);
            }
            finally
            {
                // Always dispose old service, even if new service creation failed
                // This prevents resource leaks when switching providers
                if (oldService != null && oldService != _transcriptionService)
                {
                    try
                    {
                        oldService.Dispose();
                        Logger.Info("Old transcription service disposed successfully");
                    }
                    catch (Exception ex)
                    {
                        Logger.Warning($"Failed to dispose old transcription service: {ex.Message}");
                    }
                }
            }

            Logger.Info("Settings applied");
        }

        private void OnAboutRequested(object? sender, EventArgs e)
        {
            MessageBox.Show(
                "Speech-to-Text Tray Application\n\n" +
                "Version 1.0.0\n\n" +
                "A Windows tray application for speech-to-text transcription with multiple provider options:\n" +
                "• Local (NVIDIA Parakeet via sherpa-onnx)\n" +
                "• Azure Speech Service\n" +
                "• Azure OpenAI\n\n" +
                "Features:\n" +
                "• Configurable hotkey for recording\n" +
                "• Optional Philips SpeechMike integration\n" +
                "• Automatic text injection into active window\n\n" +
                "Configure settings via the tray icon menu.",
                "About Speech-to-Text",
                MessageBoxButton.OK,
                MessageBoxImage.Information,
                MessageBoxResult.OK,
                MessageBoxOptions.DefaultDesktopOnly);
        }

        private void OnExitRequested(object? sender, EventArgs e)
        {
            Logger.Info("Application exit requested");

            var result = MessageBox.Show(
                "Are you sure you want to exit Speech-to-Text?",
                "Exit Confirmation",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question,
                MessageBoxResult.No,
                MessageBoxOptions.DefaultDesktopOnly);

            if (result == MessageBoxResult.Yes)
            {
                Shutdown();
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Logger.Info("Application shutting down...");

            // Unsubscribe from events
            if (_audioService != null)
            {
                _audioService.CaptureStarted -= OnCaptureStarted;
            }

            // Cleanup
            _hotkeyService?.Dispose();
            _audioService?.Dispose();
            _transcriptionService?.Dispose();
            _speechMikeService?.Dispose();
            _overlayWindow?.Close();
            _overlayWindow = null;
            _trayIcon?.Dispose();
            _stateLock?.Dispose();

            // Clean up temp files on exit (optional)
            // _tempFileManager?.CleanupAllFiles();

            Logger.Info("Application shut down");

            base.OnExit(e);
        }
    }
}
