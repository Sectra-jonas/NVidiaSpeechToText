using System.Text.Json.Serialization;

namespace SpeechToTextTray.Core.Models
{
    /// <summary>
    /// Response model from the FastAPI backend /transcribe endpoint
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
