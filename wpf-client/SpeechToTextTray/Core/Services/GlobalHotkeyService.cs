using System;
using System.Windows.Input;
using NHotkey;
using NHotkey.Wpf;

namespace SpeechToTextTray.Core.Services
{
    /// <summary>
    /// Service for registering and managing global hotkeys
    /// </summary>
    public class GlobalHotkeyService : IDisposable
    {
        private const string HotkeyId = "ToggleRecording";
        private bool _isRegistered;

        /// <summary>
        /// Event fired when the registered hotkey is pressed
        /// </summary>
        public event EventHandler? HotkeyPressed;

        /// <summary>
        /// Register a global hotkey
        /// </summary>
        /// <param name="modifiers">Modifier keys (Ctrl, Shift, Alt, Win)</param>
        /// <param name="key">Main key</param>
        /// <returns>True if registration successful, false if hotkey already in use</returns>
        public bool RegisterHotkey(ModifierKeys modifiers, Key key)
        {
            try
            {
                // Unregister any existing hotkey first
                UnregisterHotkey();

                // Register the new hotkey
                HotkeyManager.Current.AddOrReplace(
                    HotkeyId,
                    key,
                    modifiers,
                    OnHotkeyInvoked
                );

                _isRegistered = true;
                return true;
            }
            catch (HotkeyAlreadyRegisteredException)
            {
                System.Diagnostics.Debug.WriteLine($"Hotkey {modifiers}+{key} is already registered by another application");
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to register hotkey: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Unregister the current hotkey
        /// </summary>
        public void UnregisterHotkey()
        {
            if (_isRegistered)
            {
                try
                {
                    HotkeyManager.Current.Remove(HotkeyId);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error unregistering hotkey: {ex.Message}");
                }
                finally
                {
                    _isRegistered = false;
                }
            }
        }

        /// <summary>
        /// Check if a hotkey combination is available (not used by other apps)
        /// </summary>
        /// <remarks>
        /// Note: This is a best-effort check. The only way to truly know is to try registering.
        /// </remarks>
        public bool IsHotkeyAvailable(ModifierKeys modifiers, Key key)
        {
            try
            {
                // Try to register temporarily
                string testId = "TestHotkey";
                HotkeyManager.Current.AddOrReplace(testId, key, modifiers, (sender, e) => { });
                HotkeyManager.Current.Remove(testId);
                return true;
            }
            catch (HotkeyAlreadyRegisteredException)
            {
                return false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Gets whether a hotkey is currently registered
        /// </summary>
        public bool IsRegistered => _isRegistered;

        /// <summary>
        /// Internal handler for when hotkey is invoked
        /// </summary>
        private void OnHotkeyInvoked(object? sender, HotkeyEventArgs e)
        {
            e.Handled = true;
            HotkeyPressed?.Invoke(this, EventArgs.Empty);
        }

        public void Dispose()
        {
            UnregisterHotkey();
        }
    }
}
