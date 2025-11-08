using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
using SpeechToTextTray.Core.Services;
using SpeechToTextTray.Utils;

namespace SpeechToTextTray.UI.Windows
{
    /// <summary>
    /// Recording overlay window that displays device and provider information while recording
    /// </summary>
    public partial class RecordingOverlayWindow : Window
    {
        private readonly SettingsService _settingsService;
        private Storyboard? _pulseAnimation;
        private bool _isPositionInitialized = false;

        public RecordingOverlayWindow(SettingsService settingsService)
        {
            InitializeComponent();

            _settingsService = settingsService;

            // Get the pulse animation from resources
            _pulseAnimation = (Storyboard?)FindResource("PulseAnimation");

            // Start hidden
            Hide();
        }

        /// <summary>
        /// Update the device and provider information displayed in the overlay
        /// </summary>
        public void UpdateInfo(string deviceName, string providerName)
        {
            DeviceNameText.Text = deviceName;
            ProviderNameText.Text = providerName;
        }

        /// <summary>
        /// Show the overlay at the specified position (or default bottom-right if null)
        /// </summary>
        public void ShowAtPosition(double? x, double? y)
        {
            try
            {
                // If position values are provided, use them (saved position from settings)
                if (x.HasValue && y.HasValue)
                {
                    // Ensure position is still within screen bounds
                    var workArea = SystemParameters.WorkArea;
                    Left = Math.Max(workArea.Left, Math.Min(x.Value, workArea.Right - Width));
                    Top = Math.Max(workArea.Top, Math.Min(y.Value, workArea.Bottom - Height));
                    _isPositionInitialized = true;
                }
                else if (!_isPositionInitialized)
                {
                    // Calculate default position for first time (no saved position)
                    var workArea = SystemParameters.WorkArea;
                    Left = workArea.Right - Width - 20;
                    Top = workArea.Bottom - Height - 20;
                    _isPositionInitialized = true;
                }
                // else: keep current position (already initialized, no new position provided)

                // Show the window
                Show();

                // Start the pulsing animation
                _pulseAnimation?.Begin();

                Logger.Info($"Recording overlay shown at ({Left}, {Top})");
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to show recording overlay", ex);
            }
        }

        /// <summary>
        /// Hide the overlay and stop the animation
        /// </summary>
        public new void Hide()
        {
            try
            {
                // Stop the pulsing animation
                _pulseAnimation?.Stop();

                // Hide the window
                base.Hide();

                Logger.Info("Recording overlay hidden");
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to hide recording overlay", ex);
            }
        }

        /// <summary>
        /// Handle window drag when user clicks and drags
        /// </summary>
        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                DragMove();
            }
            catch (Exception ex)
            {
                // DragMove can throw if mouse button is not pressed
                Logger.Error("Failed to drag overlay window", ex);
            }
        }

        /// <summary>
        /// Save position when window is moved
        /// </summary>
        private void Window_LocationChanged(object? sender, EventArgs e)
        {
            try
            {
                // Only save if window is visible and position has been initialized
                if (IsVisible && _isPositionInitialized)
                {
                    var settings = _settingsService.LoadSettings();
                    settings.RecordingOverlayX = Left;
                    settings.RecordingOverlayY = Top;
                    _settingsService.SaveSettings(settings);

                    Logger.Info($"Recording overlay position saved: ({Left}, {Top})");
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to save overlay position", ex);
            }
        }
    }
}
