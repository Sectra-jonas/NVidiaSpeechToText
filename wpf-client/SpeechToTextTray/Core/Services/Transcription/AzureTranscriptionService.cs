using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using SpeechToTextTray.Core.Models;
using SpeechToTextTray.Utils;
using NAudio.Wave;

namespace SpeechToTextTray.Core.Services.Transcription
{
    /// <summary>
    /// Azure Speech SDK transcription service
    /// Supports 100+ languages with automatic language detection
    /// </summary>
    public class AzureTranscriptionService : ITranscriptionService
    {
        private readonly AzureTranscriptionConfig _config;
        private readonly SpeechConfig _speechConfig;
        private bool _disposed = false;

        public TranscriptionProvider Provider => TranscriptionProvider.Azure;

        public AzureTranscriptionService(AzureTranscriptionConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));

            // Validate configuration
            if (string.IsNullOrWhiteSpace(_config.SubscriptionKey))
                throw new InvalidOperationException("Azure subscription key is required");

            if (string.IsNullOrWhiteSpace(_config.Region))
                throw new InvalidOperationException("Azure region is required");

            try
            {
                // Initialize Speech SDK
                _speechConfig = SpeechConfig.FromSubscription(_config.SubscriptionKey, _config.Region);

                // Configure recognition language if specified
                if (!string.IsNullOrWhiteSpace(_config.Language))
                {
                    _speechConfig.SpeechRecognitionLanguage = _config.Language;
                }

                Logger.Info($"Azure Speech SDK initialized (Region: {_config.Region})");
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to initialize Azure Speech SDK", ex);
                throw new InvalidOperationException("Failed to initialize Azure Speech Service", ex);
            }
        }

        public async Task<TranscriptionResponse> TranscribeAsync(
            string audioFilePath,
            CancellationToken cancellationToken = default)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(AzureTranscriptionService));

            if (string.IsNullOrWhiteSpace(audioFilePath))
                throw new ArgumentException("Audio file path cannot be null or empty", nameof(audioFilePath));

            if (!File.Exists(audioFilePath))
                throw new FileNotFoundException("Audio file not found", audioFilePath);

            string? convertedWavPath = null;

            try
            {
                // Azure Speech SDK requires 16kHz mono WAV format
                var extension = Path.GetExtension(audioFilePath).ToLower();
                string wavPath;

                if (extension == ".wav")
                {
                    // Check if already 16kHz mono
                    using var reader = new WaveFileReader(audioFilePath);
                    if (reader.WaveFormat.SampleRate == 16000 && reader.WaveFormat.Channels == 1)
                    {
                        wavPath = audioFilePath;
                    }
                    else
                    {
                        // Need to resample/convert
                        convertedWavPath = ConvertTo16kHzMonoWav(audioFilePath);
                        wavPath = convertedWavPath;
                    }
                }
                else
                {
                    // Convert non-WAV formats to 16kHz mono WAV
                    convertedWavPath = ConvertTo16kHzMonoWav(audioFilePath);
                    wavPath = convertedWavPath;
                }

                // Perform transcription
                return await TranscribeWavFileAsync(wavPath, Path.GetFileName(audioFilePath), cancellationToken);
            }
            catch (Exception ex)
            {
                Logger.Error($"Azure transcription failed: {audioFilePath}", ex);
                return new TranscriptionResponse
                {
                    Success = false,
                    Text = "",
                    Language = "unknown",
                    AudioDuration = 0,
                    OriginalFilename = Path.GetFileName(audioFilePath),
                    Provider = TranscriptionProvider.Azure,
                    ErrorMessage = ex.Message,
                    Exception = ex
                };
            }
            finally
            {
                // Clean up temporary converted file if created
                if (convertedWavPath != null && File.Exists(convertedWavPath))
                {
                    try
                    {
                        File.Delete(convertedWavPath);
                    }
                    catch
                    {
                        // Ignore cleanup errors
                    }
                }
            }
        }

        private async Task<TranscriptionResponse> TranscribeWavFileAsync(
            string wavFilePath,
            string originalFilename,
            CancellationToken cancellationToken)
        {
            using var audioConfig = AudioConfig.FromWavFileInput(wavFilePath);
            using var recognizer = new SpeechRecognizer(_speechConfig, audioConfig);

            var transcriptionText = new StringBuilder();
            var detectedLanguage = _config.Language ?? "unknown";
            var recognitionCompleted = new TaskCompletionSource<bool>();
            var hasError = false;
            var errorMessage = "";

            // Subscribe to recognition events
            recognizer.Recognized += (s, e) =>
            {
                if (e.Result.Reason == ResultReason.RecognizedSpeech)
                {
                    transcriptionText.Append(e.Result.Text);

                    // Update detected language if available
                    if (!string.IsNullOrEmpty(e.Result.Properties.GetProperty(PropertyId.SpeechServiceConnection_AutoDetectSourceLanguageResult)))
                    {
                        detectedLanguage = e.Result.Properties.GetProperty(PropertyId.SpeechServiceConnection_AutoDetectSourceLanguageResult);
                    }

                    Logger.Info($"Azure recognized: {e.Result.Text}");
                }
                else if (e.Result.Reason == ResultReason.NoMatch)
                {
                    Logger.Warning("Azure: No speech could be recognized");
                }
            };

            recognizer.Canceled += (s, e) =>
            {
                if (e.Reason == CancellationReason.Error)
                {
                    hasError = true;
                    errorMessage = $"Recognition error: {e.ErrorDetails}";
                    Logger.Error($"Azure recognition error: {e.ErrorDetails}");
                }

                recognitionCompleted.TrySetResult(true);
            };

            recognizer.SessionStopped += (s, e) =>
            {
                Logger.Info("Azure recognition session stopped");
                recognitionCompleted.TrySetResult(true);
            };

            // Register cancellation
            using (cancellationToken.Register(() =>
            {
                Logger.Info("Azure transcription cancelled by user");
                recognitionCompleted.TrySetCanceled();
            }))
            {
                // Start continuous recognition
                await recognizer.StartContinuousRecognitionAsync();

                // Wait for completion or cancellation
                try
                {
                    await recognitionCompleted.Task;
                }
                catch (TaskCanceledException)
                {
                    Logger.Info("Azure transcription task cancelled");
                    throw;
                }

                // Stop recognition
                await recognizer.StopContinuousRecognitionAsync();
            }

            // Check for errors
            if (hasError)
            {
                return new TranscriptionResponse
                {
                    Success = false,
                    Text = "",
                    Language = detectedLanguage,
                    AudioDuration = 0,
                    OriginalFilename = originalFilename,
                    Provider = TranscriptionProvider.Azure,
                    ErrorMessage = errorMessage
                };
            }

            // Get audio duration
            double duration = 0;
            try
            {
                using var waveReader = new WaveFileReader(wavFilePath);
                duration = waveReader.TotalTime.TotalSeconds;
            }
            catch
            {
                // Ignore duration calculation errors
            }

            return new TranscriptionResponse
            {
                Success = true,
                Text = transcriptionText.ToString().Trim(),
                Language = detectedLanguage,
                AudioDuration = duration,
                OriginalFilename = originalFilename,
                Provider = TranscriptionProvider.Azure,
                ErrorMessage = null
            };
        }

        /// <summary>
        /// Convert audio file to 16kHz mono WAV format for Azure Speech SDK
        /// </summary>
        private string ConvertTo16kHzMonoWav(string inputPath)
        {
            var tempPath = Path.Combine(
                Path.GetTempPath(),
                $"azure_converted_{Guid.NewGuid()}.wav"
            );

            using (var reader = new AudioFileReader(inputPath))
            {
                var outFormat = new WaveFormat(16000, 16, 1);
                using (var resampler = new MediaFoundationResampler(reader, outFormat))
                {
                    WaveFileWriter.CreateWaveFile(tempPath, resampler);
                }
            }

            return tempPath;
        }

        public TranscriptionProviderInfo GetProviderInfo()
        {
            return new TranscriptionProviderInfo
            {
                Provider = TranscriptionProvider.Azure,
                ProviderName = "Azure Speech Service",
                Version = "SDK 1.43.0",
                Status = IsAvailable() ? "Ready" : "Unavailable (check network)",
                RequiresNetwork = true,
                RequiresCredentials = true,
                SupportedLanguages = new[] { "100+ languages with auto-detection" },
                Description = "Cloud-based speech recognition powered by Azure Cognitive Services"
            };
        }

        public async Task<ValidationResult> ValidateConfigurationAsync()
        {
            try
            {
                // Check network connectivity
                if (!IsNetworkAvailable())
                {
                    return new ValidationResult
                    {
                        IsValid = false,
                        ErrorMessage = "No network connection available. Azure Speech Service requires internet connectivity.",
                        ErrorType = ValidationErrorType.NetworkError
                    };
                }

                // Basic credential check - try to create a recognizer
                // This doesn't guarantee the credentials are valid, but catches obvious errors
                using var audioConfig = AudioConfig.FromDefaultMicrophoneInput();
                using var recognizer = new SpeechRecognizer(_speechConfig, audioConfig);

                Logger.Info("Azure configuration validation passed");

                return new ValidationResult
                {
                    IsValid = true,
                    ErrorMessage = null,
                    ErrorType = ValidationErrorType.None
                };
            }
            catch (Exception ex)
            {
                Logger.Error("Azure configuration validation failed", ex);
                return new ValidationResult
                {
                    IsValid = false,
                    ErrorMessage = $"Azure configuration invalid: {ex.Message}",
                    ErrorType = ValidationErrorType.InvalidCredentials
                };
            }
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
                // SpeechConfig doesn't implement IDisposable
                _disposed = true;
            }
        }
    }
}
