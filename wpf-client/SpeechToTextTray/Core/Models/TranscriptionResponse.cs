using System;
using System.Text.Json.Serialization;

namespace SpeechToTextTray.Core.Models
{
    /// <summary>
    /// Response model from transcription services (local or cloud-based)
    /// </summary>
    public class TranscriptionResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("text")]
        public required string Text { get; set; }

        [JsonPropertyName("language")]
        public required string Language { get; set; }

        [JsonPropertyName("audio_duration")]
        public double AudioDuration { get; set; }

        [JsonPropertyName("original_filename")]
        public required string OriginalFilename { get; set; }

        /// <summary>
        /// Error message if transcription failed
        /// </summary>
        [JsonPropertyName("error_message")]
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Provider that performed the transcription
        /// </summary>
        [JsonPropertyName("provider")]
        public TranscriptionProvider Provider { get; set; }

        /// <summary>
        /// Exception details (not serialized, for internal use)
        /// </summary>
        [JsonIgnore]
        public Exception? Exception { get; set; }
    }

    /// <summary>
    /// Model information from /model-info endpoint
    /// </summary>
    public class ModelInfo
    {
        [JsonPropertyName("model_name")]
        public required string ModelName { get; set; }

        [JsonPropertyName("device")]
        public required string Device { get; set; }

        [JsonPropertyName("status")]
        public required string Status { get; set; }
    }

    /// <summary>
    /// Health check response from /health endpoint
    /// </summary>
    public class HealthResponse
    {
        [JsonPropertyName("status")]
        public required string Status { get; set; }

        [JsonPropertyName("model")]
        public required ModelInfo Model { get; set; }
    }
}
