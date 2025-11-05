namespace SpeechToTextTray.Core.Models
{
    /// <summary>
    /// Represents an audio input device (microphone)
    /// </summary>
    public class AudioDevice
    {
        /// <summary>
        /// Device identifier (index or GUID)
        /// </summary>
        public required string Id { get; set; }

        /// <summary>
        /// Human-readable device name
        /// </summary>
        public required string Name { get; set; }

        /// <summary>
        /// Number of channels supported
        /// </summary>
        public int Channels { get; set; }

        /// <summary>
        /// Whether this is the default device
        /// </summary>
        public bool IsDefault { get; set; }

        public override string ToString() => Name;
    }
}
