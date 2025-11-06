namespace SpeechToTextTray.Core.Models
{
    /// <summary>
    /// Transcription provider enum
    /// </summary>
    public enum TranscriptionProvider
    {
        Local = 0,
        Azure = 1,
        AzureOpenAI = 2
        // Future providers:
        // GoogleCloud = 3,
        // AWSTranscribe = 4
    }

    /// <summary>
    /// Main configuration for transcription providers
    /// </summary>
    public class TranscriptionConfig
    {
        /// <summary>
        /// Selected transcription provider
        /// </summary>
        public TranscriptionProvider Provider { get; set; } = TranscriptionProvider.Local;

        /// <summary>
        /// Configuration for local transcription
        /// </summary>
        public LocalTranscriptionConfig Local { get; set; } = new LocalTranscriptionConfig();

        /// <summary>
        /// Configuration for Azure transcription
        /// </summary>
        public AzureTranscriptionConfig Azure { get; set; } = new AzureTranscriptionConfig();

        /// <summary>
        /// Configuration for Azure OpenAI Whisper transcription
        /// </summary>
        public AzureOpenAITranscriptionConfig AzureOpenAI { get; set; } = new AzureOpenAITranscriptionConfig();
    }

    /// <summary>
    /// Local transcription configuration (sherpa-onnx)
    /// </summary>
    public class LocalTranscriptionConfig
    {
        /// <summary>
        /// Path to the ONNX model directory (optional, uses bundled model if null)
        /// </summary>
        public string? ModelPath { get; set; } = null;

        /// <summary>
        /// Number of CPU threads to use (0 = auto-detect)
        /// </summary>
        public int NumThreads { get; set; } = 0;
    }

    /// <summary>
    /// Azure Speech SDK configuration (minimal version)
    /// </summary>
    public class AzureTranscriptionConfig
    {
        /// <summary>
        /// Azure Speech service subscription key
        /// NOTE: Stored in plain text for now, encryption in future iteration
        /// </summary>
        public string SubscriptionKey { get; set; } = "";

        /// <summary>
        /// Azure region (e.g., "eastus", "westeurope")
        /// </summary>
        public string Region { get; set; } = "eastus";

        /// <summary>
        /// Recognition language (BCP-47 format, e.g., "en-US", "de-DE")
        /// Leave null/empty for auto-detection
        /// </summary>
        public string? Language { get; set; } = null;
    }

    /// <summary>
    /// Azure OpenAI Whisper configuration
    /// </summary>
    public class AzureOpenAITranscriptionConfig
    {
        /// <summary>
        /// Azure OpenAI service endpoint URL
        /// Example: https://your-resource-name.openai.azure.com/
        /// NOTE: Stored in plain text for now, encryption in future iteration
        /// </summary>
        public string Endpoint { get; set; } = "";

        /// <summary>
        /// Azure OpenAI API key
        /// NOTE: Stored in plain text for now, encryption in future iteration
        /// </summary>
        public string ApiKey { get; set; } = "";

        /// <summary>
        /// Deployment name for the Whisper model
        /// This is the custom deployment name you created in Azure OpenAI Studio
        /// </summary>
        public string DeploymentName { get; set; } = "";

        /// <summary>
        /// Optional language hint (BCP-47 format, e.g., "en", "de")
        /// Leave null/empty for auto-detection
        /// </summary>
        public string? Language { get; set; } = null;

        /// <summary>
        /// Optional prompt to guide transcription
        /// Can be used to improve accuracy for specific terminology or context
        /// </summary>
        public string? Prompt { get; set; } = null;
    }
}
