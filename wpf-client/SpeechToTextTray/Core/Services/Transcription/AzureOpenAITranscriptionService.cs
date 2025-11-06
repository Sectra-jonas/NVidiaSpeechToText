using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.AI.OpenAI;
using OpenAI.Audio;
using SpeechToTextTray.Core.Models;
using SpeechToTextTray.Utils;

namespace SpeechToTextTray.Core.Services.Transcription
{
    /// <summary>
    /// Azure OpenAI Whisper transcription service
    /// Supports excellent English transcription and translation to English from other languages
    /// </summary>
    public class AzureOpenAITranscriptionService : ITranscriptionService
    {
        private readonly AzureOpenAITranscriptionConfig _config;
        private readonly AzureOpenAIClient _client;
        private readonly AudioClient _audioClient;
        private bool _disposed = false;

        // Azure OpenAI Whisper limitations
        private const long MAX_FILE_SIZE_BYTES = 25 * 1024 * 1024; // 25MB
        private static readonly string[] SUPPORTED_FORMATS = { ".mp3", ".mp4", ".mpeg", ".mpga", ".m4a", ".wav", ".webm" };

        public TranscriptionProvider Provider => TranscriptionProvider.AzureOpenAI;

        public AzureOpenAITranscriptionService(AzureOpenAITranscriptionConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));

            // Validate configuration
            if (string.IsNullOrWhiteSpace(_config.Endpoint))
                throw new InvalidOperationException("Azure OpenAI endpoint is required");

            if (string.IsNullOrWhiteSpace(_config.ApiKey))
                throw new InvalidOperationException("Azure OpenAI API key is required");

            if (string.IsNullOrWhiteSpace(_config.DeploymentName))
                throw new InvalidOperationException("Azure OpenAI deployment name is required");

            try
            {
                // Initialize Azure OpenAI client
                var endpoint = new Uri(_config.Endpoint);
                var credential = new AzureKeyCredential(_config.ApiKey);
                _client = new AzureOpenAIClient(endpoint, credential);

                // Get audio client for the deployment
                _audioClient = _client.GetAudioClient(_config.DeploymentName);

                Logger.Info($"Azure OpenAI client initialized (Endpoint: {_config.Endpoint}, Deployment: {_config.DeploymentName})");
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to initialize Azure OpenAI client", ex);
                throw new InvalidOperationException("Failed to initialize Azure OpenAI service", ex);
            }
        }

        public async Task<TranscriptionResponse> TranscribeAsync(
            string audioFilePath,
            CancellationToken cancellationToken = default)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(AzureOpenAITranscriptionService));

            if (string.IsNullOrWhiteSpace(audioFilePath))
                throw new ArgumentException("Audio file path cannot be null or empty", nameof(audioFilePath));

            if (!File.Exists(audioFilePath))
                throw new FileNotFoundException("Audio file not found", audioFilePath);

            try
            {
                // Validate file size
                var fileInfo = new FileInfo(audioFilePath);
                if (fileInfo.Length > MAX_FILE_SIZE_BYTES)
                {
                    throw new InvalidOperationException($"Audio file size ({fileInfo.Length / 1024 / 1024}MB) exceeds Azure OpenAI Whisper limit of 25MB");
                }

                // Validate file format
                var extension = Path.GetExtension(audioFilePath).ToLower();
                if (Array.IndexOf(SUPPORTED_FORMATS, extension) == -1)
                {
                    throw new InvalidOperationException($"Audio format '{extension}' is not supported by Azure OpenAI Whisper. Supported formats: {string.Join(", ", SUPPORTED_FORMATS)}");
                }

                // Perform transcription
                return await TranscribeFileAsync(audioFilePath, Path.GetFileName(audioFilePath), cancellationToken);
            }
            catch (Exception ex)
            {
                Logger.Error($"Azure OpenAI transcription failed: {audioFilePath}", ex);
                return new TranscriptionResponse
                {
                    Success = false,
                    Text = "",
                    Language = "unknown",
                    AudioDuration = 0,
                    OriginalFilename = Path.GetFileName(audioFilePath),
                    Provider = TranscriptionProvider.AzureOpenAI,
                    ErrorMessage = ex.Message,
                    Exception = ex
                };
            }
        }

        private async Task<TranscriptionResponse> TranscribeFileAsync(
            string audioFilePath,
            string originalFilename,
            CancellationToken cancellationToken)
        {
            Logger.Info($"Transcribing audio with Azure OpenAI Whisper: {audioFilePath}");

            try
            {
                // Prepare transcription options
                var options = new AudioTranscriptionOptions
                {
                    ResponseFormat = AudioTranscriptionFormat.Verbose
                };

                // Add optional language hint
                if (!string.IsNullOrWhiteSpace(_config.Language))
                {
                    options.Language = _config.Language;
                }

                // Add optional prompt
                if (!string.IsNullOrWhiteSpace(_config.Prompt))
                {
                    options.Prompt = _config.Prompt;
                }

                // Open audio file stream
                using var audioStream = File.OpenRead(audioFilePath);

                // Call Azure OpenAI Whisper API
                var result = await _audioClient.TranscribeAudioAsync(audioStream, originalFilename, options, cancellationToken);

                // Extract transcription details
                var transcription = result.Value;
                var text = transcription.Text ?? "";
                var language = transcription.Language ?? "unknown";
                var duration = transcription.Duration?.TotalSeconds ?? 0;

                Logger.Info($"Azure OpenAI transcription complete: {text.Length} characters, Language: {language}, Duration: {duration:F1}s");

                return new TranscriptionResponse
                {
                    Success = true,
                    Text = text.Trim(),
                    Language = language,
                    AudioDuration = duration,
                    OriginalFilename = originalFilename,
                    Provider = TranscriptionProvider.AzureOpenAI,
                    ErrorMessage = null
                };
            }
            catch (RequestFailedException ex)
            {
                Logger.Error($"Azure OpenAI API request failed: {ex.Message} (Status: {ex.Status})", ex);
                return new TranscriptionResponse
                {
                    Success = false,
                    Text = "",
                    Language = "unknown",
                    AudioDuration = 0,
                    OriginalFilename = originalFilename,
                    Provider = TranscriptionProvider.AzureOpenAI,
                    ErrorMessage = $"Azure OpenAI API error: {ex.Message}",
                    Exception = ex
                };
            }
            catch (Exception ex)
            {
                Logger.Error($"Azure OpenAI transcription error: {ex.Message}", ex);
                return new TranscriptionResponse
                {
                    Success = false,
                    Text = "",
                    Language = "unknown",
                    AudioDuration = 0,
                    OriginalFilename = originalFilename,
                    Provider = TranscriptionProvider.AzureOpenAI,
                    ErrorMessage = ex.Message,
                    Exception = ex
                };
            }
        }

        public TranscriptionProviderInfo GetProviderInfo()
        {
            return new TranscriptionProviderInfo
            {
                Provider = TranscriptionProvider.AzureOpenAI,
                ProviderName = "Azure OpenAI Whisper",
                Version = "Azure.AI.OpenAI 2.1.0",
                Status = IsAvailable() ? "Ready" : "Unavailable (check network)",
                RequiresNetwork = true,
                RequiresCredentials = true,
                SupportedLanguages = new[] { "Optimized for English, supports 50+ languages with translation to English" },
                Description = "OpenAI's Whisper model hosted on Azure for excellent transcription quality"
            };
        }

        public async Task<ValidationResult> ValidateConfigurationAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    // Check network connectivity
                    if (!IsNetworkAvailable())
                    {
                        return new ValidationResult
                        {
                            IsValid = false,
                            ErrorMessage = "No network connection available. Azure OpenAI requires internet connectivity.",
                            ErrorType = ValidationErrorType.NetworkError
                        };
                    }

                    // Validate endpoint URL format
                    if (!Uri.TryCreate(_config.Endpoint, UriKind.Absolute, out var endpoint))
                    {
                        return new ValidationResult
                        {
                            IsValid = false,
                            ErrorMessage = "Invalid Azure OpenAI endpoint URL format",
                            ErrorType = ValidationErrorType.InvalidCredentials
                        };
                    }

                    // Validate endpoint is HTTPS
                    if (endpoint.Scheme != Uri.UriSchemeHttps)
                    {
                        return new ValidationResult
                        {
                            IsValid = false,
                            ErrorMessage = "Azure OpenAI endpoint must use HTTPS",
                            ErrorType = ValidationErrorType.InvalidCredentials
                        };
                    }

                    // Basic validation passed
                    // Note: We can't test the API without making a real request with an audio file
                    Logger.Info("Azure OpenAI configuration validation passed (basic checks)");

                    return new ValidationResult
                    {
                        IsValid = true,
                        ErrorMessage = null,
                        ErrorType = ValidationErrorType.None
                    };
                }
                catch (Exception ex)
                {
                    Logger.Error("Azure OpenAI configuration validation failed", ex);
                    return new ValidationResult
                    {
                        IsValid = false,
                        ErrorMessage = $"Azure OpenAI configuration invalid: {ex.Message}",
                        ErrorType = ValidationErrorType.InvalidCredentials
                    };
                }
            });
        }

        public bool IsAvailable()
        {
            return IsNetworkAvailable() && !_disposed;
        }

        private bool IsNetworkAvailable()
        {
            try
            {
                return System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable();
            }
            catch
            {
                return false;
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                // AzureOpenAIClient and AudioClient don't implement IDisposable in the current SDK
                _disposed = true;
            }
        }
    }
}
