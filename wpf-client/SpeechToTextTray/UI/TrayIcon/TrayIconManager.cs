using System;
using System.Windows;
using Hardcodet.Wpf.TaskbarNotification;
using SpeechToTextTray.Core.Models;

namespace SpeechToTextTray.UI.TrayIcon
{
    /// <summary>
    /// Manager for the system tray icon and its interactions
    /// </summary>
    public class TrayIconManager : IDisposable
    {
        private TaskbarIcon _taskbarIcon = null!;
        private RecordingState _currentState = RecordingState.Idle;

        /// <summary>
        /// Event fired when user requests to open settings
        /// </summary>
        public event EventHandler? SettingsRequested;

        /// <summary>
        /// Event fired when user requests to show about dialog
        /// </summary>
        public event EventHandler? AboutRequested;

        /// <summary>
        /// Event fired when user requests to exit the application
        /// </summary>
        public event EventHandler? ExitRequested;

        /// <summary>
        /// Initialize the tray icon
        /// </summary>
        public void Initialize()
        {
            _taskbarIcon = new TaskbarIcon
            {
                IconSource = GetIconForState(RecordingState.Idle),
                ToolTipText = "Speech-to-Text - Ready",
                MenuActivation = PopupActivationMode.RightClick
            };

            // Create context menu
            var contextMenu = new System.Windows.Controls.ContextMenu();

            // Status item (disabled, shows current state)
            var statusItem = new System.Windows.Controls.MenuItem
            {
                Header = "Ready",
                IsEnabled = false,
                Tag = "status"
            };
            contextMenu.Items.Add(statusItem);

            contextMenu.Items.Add(new System.Windows.Controls.Separator());

            // Settings
            var settingsItem = new System.Windows.Controls.MenuItem { Header = "Settings..." };
            settingsItem.Click += (s, e) => SettingsRequested?.Invoke(this, EventArgs.Empty);
            contextMenu.Items.Add(settingsItem);

            // About
            var aboutItem = new System.Windows.Controls.MenuItem { Header = "About..." };
            aboutItem.Click += (s, e) => AboutRequested?.Invoke(this, EventArgs.Empty);
            contextMenu.Items.Add(aboutItem);

            contextMenu.Items.Add(new System.Windows.Controls.Separator());

            // Exit
            var exitItem = new System.Windows.Controls.MenuItem { Header = "Exit" };
            exitItem.Click += (s, e) => ExitRequested?.Invoke(this, EventArgs.Empty);
            contextMenu.Items.Add(exitItem);

            _taskbarIcon.ContextMenu = contextMenu;

            // Left-click to show status
            _taskbarIcon.TrayLeftMouseUp += (s, e) =>
            {
                ShowNotification("Speech-to-Text", GetStatusMessage() ?? "Status unknown", Hardcodet.Wpf.TaskbarNotification.BalloonIcon.Info);
            };
        }

        /// <summary>
        /// Update the tray icon state
        /// </summary>
        public void SetState(RecordingState state, string? additionalInfo = null)
        {
            _currentState = state;

            if (_taskbarIcon != null)
            {
                // Update icon
                _taskbarIcon.IconSource = GetIconForState(state);

                // Update tooltip
                _taskbarIcon.ToolTipText = $"Speech-to-Text - {GetStateDisplayText(state)}" +
                    (string.IsNullOrEmpty(additionalInfo) ? "" : $" ({additionalInfo})");

                // Update status menu item
                if (_taskbarIcon.ContextMenu != null)
                {
                    foreach (var item in _taskbarIcon.ContextMenu.Items)
                    {
                        if (item is System.Windows.Controls.MenuItem menuItem && "status".Equals(menuItem.Tag))
                        {
                            menuItem.Header = GetStateDisplayText(state) +
                                (string.IsNullOrEmpty(additionalInfo) ? "" : $" - {additionalInfo}");
                            break;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Show a balloon notification
        /// </summary>
        public void ShowNotification(string title, string message, Hardcodet.Wpf.TaskbarNotification.BalloonIcon icon = Hardcodet.Wpf.TaskbarNotification.BalloonIcon.Info)
        {
            _taskbarIcon?.ShowBalloonTip(title, message, icon);
        }

        /// <summary>
        /// Get the appropriate icon for a given state
        /// </summary>
        private System.Windows.Media.ImageSource GetIconForState(RecordingState state)
        {
            string iconPath = state switch
            {
                RecordingState.Recording => "pack://application:,,,/Resources/Icons/tray-icon-recording.ico",
                RecordingState.Processing => "pack://application:,,,/Resources/Icons/tray-icon-processing.ico",
                RecordingState.Injecting => "pack://application:,,,/Resources/Icons/tray-icon-processing.ico",
                RecordingState.Error => "pack://application:,,,/Resources/Icons/tray-icon-idle.ico", // Use idle icon for error
                _ => "pack://application:,,,/Resources/Icons/tray-icon-idle.ico"
            };

            try
            {
                return new System.Windows.Media.Imaging.BitmapImage(new Uri(iconPath));
            }
            catch
            {
                // Fallback: create a simple colored icon (null is acceptable for fallback)
                return CreateFallbackIcon(state)!;
            }
        }

        /// <summary>
        /// Create a simple fallback icon if resource icons aren't available
        /// </summary>
        private System.Windows.Media.ImageSource? CreateFallbackIcon(RecordingState state)
        {
            // This would create a simple colored square as an icon
            // For now, return null and the system will use a default icon
            return null;
        }

        /// <summary>
        /// Get display text for a state
        /// </summary>
        private string GetStateDisplayText(RecordingState state)
        {
            return state switch
            {
                RecordingState.Idle => "Ready",
                RecordingState.Recording => "Recording",
                RecordingState.Processing => "Transcribing",
                RecordingState.Injecting => "Inserting Text",
                RecordingState.Error => "Error",
                _ => "Unknown"
            };
        }

        /// <summary>
        /// Get current status message
        /// </summary>
        private string? GetStatusMessage()
        {
            return _currentState switch
            {
                RecordingState.Idle => "Ready to record. Press your hotkey to start.",
                RecordingState.Recording => "Recording audio...",
                RecordingState.Processing => "Processing transcription...",
                RecordingState.Injecting => "Inserting text...",
                RecordingState.Error => "An error occurred. Check logs.",
                _ => "Status unknown"
            };
        }

        public void Dispose()
        {
            _taskbarIcon?.Dispose();
        }
    }
}
