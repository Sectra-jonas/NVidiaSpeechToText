using System.Windows.Input;

namespace SpeechToTextTray.Core.Models
{
    /// <summary>
    /// Application settings that persist across sessions
    /// </summary>
    public class AppSettings
    {
        /// <summary>
        /// Global hotkey configuration for toggle recording
        /// </summary>
        public HotkeyConfig Hotkey { get; set; } = new HotkeyConfig
        {
            Modifiers = ModifierKeys.Control | ModifierKeys.Shift,
            Key = Key.Space
        };

        /// <summary>
        /// Selected audio input device ID (or "default")
        /// </summary>
        public string AudioDeviceId { get; set; } = "default";

        /// <summary>
        /// Whether to show Windows toast notifications
        /// </summary>
        public bool ShowNotifications { get; set; } = true;

        /// <summary>
        /// Whether to automatically inject text into active window
        /// </summary>
        public bool InjectTextAutomatically { get; set; } = true;

        /// <summary>
        /// Whether to fallback to clipboard if text injection fails
        /// </summary>
        public bool FallbackToClipboard { get; set; } = true;

        /// <summary>
        /// Whether to enable Philips SpeechMike integration
        /// </summary>
        public bool EnableSpeechMike { get; set; } = false;

        /// <summary>
        /// Whether to show recording overlay window during recording
        /// </summary>
        public bool ShowRecordingOverlay { get; set; } = true;

        /// <summary>
        /// Saved X position of recording overlay (null = default position)
        /// </summary>
        public double? RecordingOverlayX { get; set; } = null;

        /// <summary>
        /// Saved Y position of recording overlay (null = default position)
        /// </summary>
        public double? RecordingOverlayY { get; set; } = null;

        /// <summary>
        /// Transcription provider settings
        /// </summary>
        public TranscriptionConfig Transcription { get; set; } = new TranscriptionConfig();
    }

    /// <summary>
    /// Hotkey configuration (modifier keys + main key)
    /// </summary>
    public class HotkeyConfig
    {
        /// <summary>
        /// Modifier keys (Ctrl, Shift, Alt, Windows)
        /// </summary>
        public ModifierKeys Modifiers { get; set; }

        /// <summary>
        /// Main key to trigger the hotkey
        /// </summary>
        public Key Key { get; set; }

        /// <summary>
        /// Returns a human-readable string representation
        /// </summary>
        public override string ToString()
        {
            var parts = new System.Collections.Generic.List<string>();

            if (Modifiers.HasFlag(ModifierKeys.Control))
                parts.Add("Ctrl");
            if (Modifiers.HasFlag(ModifierKeys.Alt))
                parts.Add("Alt");
            if (Modifiers.HasFlag(ModifierKeys.Shift))
                parts.Add("Shift");
            if (Modifiers.HasFlag(ModifierKeys.Windows))
                parts.Add("Win");

            parts.Add(Key.ToString());

            return string.Join(" + ", parts);
        }
    }
}
