using System;
using System.Windows;

namespace SpeechToTextTray.Utils
{
    /// <summary>
    /// Helper for showing Windows notifications
    /// </summary>
    public class NotificationHelper
    {
        private readonly bool _notificationsEnabled;

        public NotificationHelper(bool enabled = true)
        {
            _notificationsEnabled = enabled;
        }

        /// <summary>
        /// Show a simple notification balloon (uses tray icon if available)
        /// </summary>
        public void Show(string title, string message, NotificationType type = NotificationType.Info)
        {
            if (!_notificationsEnabled)
                return;

            try
            {
                // For now, use MessageBox as a simple notification
                // In production, this would integrate with the TrayIcon's ShowBalloonTip
                // or use Windows 10 toast notifications
                Application.Current?.Dispatcher?.Invoke(() =>
                {
                    // This is a simple fallback - the TrayIconManager will provide better notifications
                    System.Diagnostics.Debug.WriteLine($"[{type}] {title}: {message}");
                });
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to show notification", ex);
            }
        }

        /// <summary>
        /// Show success notification
        /// </summary>
        public void ShowSuccess(string message)
        {
            Show("Success", message, NotificationType.Success);
        }

        /// <summary>
        /// Show error notification
        /// </summary>
        public void ShowError(string message)
        {
            Show("Error", message, NotificationType.Error);
        }

        /// <summary>
        /// Show warning notification
        /// </summary>
        public void ShowWarning(string message)
        {
            Show("Warning", message, NotificationType.Warning);
        }

        /// <summary>
        /// Show info notification
        /// </summary>
        public void ShowInfo(string message)
        {
            Show("Information", message, NotificationType.Info);
        }
    }

    public enum NotificationType
    {
        Info,
        Success,
        Warning,
        Error
    }
}
