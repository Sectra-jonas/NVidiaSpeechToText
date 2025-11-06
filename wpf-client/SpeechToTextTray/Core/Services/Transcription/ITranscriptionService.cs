using System;
using System.Threading;
using System.Threading.Tasks;
using SpeechToTextTray.Core.Models;

namespace SpeechToTextTray.Core.Services.Transcription
{
    /// <summary>
    /// Interface for transcription service providers
    /// Allows seamless switching between local, Azure, and future providers
    /// </summary>
    public interface ITranscriptionService : IDisposable
    {
        /// <summary>
        /// Transcribe an audio file asynchronously
        /// </summary>
        /// <param name="audioFilePath">Path to the audio file (WAV, WebM, MP3, etc.)</param>
        /// <param name="cancellationToken">Cancellation token for async operation</param>
        /// <returns>Transcription response with text and metadata</returns>
        Task<TranscriptionResponse> TranscribeAsync(
            string audioFilePath,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Get provider information (name, status, capabilities)
        /// </summary>
        /// <returns>Provider information</returns>
        TranscriptionProviderInfo GetProviderInfo();

        /// <summary>
        /// Validate the configuration before initialization
        /// Checks credentials, network connectivity, model availability, etc.
        /// </summary>
        /// <returns>Validation result with success status and error messages</returns>
        Task<ValidationResult> ValidateConfigurationAsync();

        /// <summary>
        /// Check if the service is currently available
        /// For Azure: checks network connectivity
        /// For Local: checks model files exist
        /// </summary>
        /// <returns>True if service is ready to use</returns>
        bool IsAvailable();

        /// <summary>
        /// Get the provider type
        /// </summary>
        TranscriptionProvider Provider { get; }
    }
}
