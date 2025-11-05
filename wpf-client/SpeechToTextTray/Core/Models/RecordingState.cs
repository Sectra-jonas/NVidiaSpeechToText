namespace SpeechToTextTray.Core.Models
{
    /// <summary>
    /// Represents the current state of the application
    /// </summary>
    public enum RecordingState
    {
        /// <summary>
        /// Application is idle and ready to start recording
        /// </summary>
        Idle,

        /// <summary>
        /// Currently recording audio from microphone
        /// </summary>
        Recording,

        /// <summary>
        /// Processing: uploading audio and waiting for transcription
        /// </summary>
        Processing,

        /// <summary>
        /// Injecting transcribed text into active window
        /// </summary>
        Injecting,

        /// <summary>
        /// Error state - something went wrong
        /// </summary>
        Error
    }
}
