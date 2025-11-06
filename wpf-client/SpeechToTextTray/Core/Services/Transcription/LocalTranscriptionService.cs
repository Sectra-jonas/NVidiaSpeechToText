using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SherpaOnnx;
using SpeechToTextTray.Core.Models;
using NAudio.Wave;

namespace SpeechToTextTray.Core.Services.Transcription
{
    /// <summary>
    /// Local transcription service using sherpa-onnx with NVIDIA Parakeet model
    /// </summary>
    public class LocalTranscriptionService : ITranscriptionService
    {
        private readonly OfflineRecognizer _recognizer;
        private readonly string _modelPath;
        private bool _disposed = false;

        public TranscriptionProvider Provider => TranscriptionProvider.Local;

        public LocalTranscriptionService(string? modelPath = null)
        {
            // Default to bundled model path
            _modelPath = modelPath ?? Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "Models",
                "sherpa-onnx-nemo-parakeet-tdt-0.6b-v3-int8"
            );

            if (!Directory.Exists(_modelPath))
                throw new DirectoryNotFoundException($"Model directory not found: {_modelPath}");

            // Verify required model files exist
            var encoderPath = Path.Combine(_modelPath, "encoder.int8.onnx");
            var decoderPath = Path.Combine(_modelPath, "decoder.int8.onnx");
            var joinerPath = Path.Combine(_modelPath, "joiner.int8.onnx");
            var tokensPath = Path.Combine(_modelPath, "tokens.txt");

            if (!File.Exists(encoderPath))
                throw new FileNotFoundException("Encoder model not found", encoderPath);
            if (!File.Exists(decoderPath))
                throw new FileNotFoundException("Decoder model not found", decoderPath);
            if (!File.Exists(joinerPath))
                throw new FileNotFoundException("Joiner model not found", joinerPath);
            if (!File.Exists(tokensPath))
                throw new FileNotFoundException("Tokens file not found", tokensPath);

            // Initialize sherpa-onnx offline recognizer
            var config = new OfflineRecognizerConfig
            {
                FeatConfig = new FeatureConfig
                {
                    SampleRate = 16000,
                    FeatureDim = 80
                },
                ModelConfig = new OfflineModelConfig
                {
                    Transducer = new OfflineTransducerModelConfig
                    {
                        Encoder = encoderPath,
                        Decoder = decoderPath,
                        Joiner = joinerPath
                    },
                    Tokens = tokensPath,
                    NumThreads = Environment.ProcessorCount / 2, // Use half of available cores
                    Debug = 0
                },
                DecodingMethod = "greedy_search",
                MaxActivePaths = 4
            };

            _recognizer = new OfflineRecognizer(config);
        }

        /// <summary>
        /// Transcribe an audio file
        /// </summary>
        /// <param name="audioFilePath">Path to the audio file (WAV, WebM, MP3, etc.)</param>
        /// <param name="cancellationToken">Cancellation token for async operation</param>
        /// <returns>Transcription response with text and metadata</returns>
        public async Task<TranscriptionResponse> TranscribeAsync(
            string audioFilePath,
            CancellationToken cancellationToken = default)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(LocalTranscriptionService));

            if (!File.Exists(audioFilePath))
                throw new FileNotFoundException("Audio file not found", audioFilePath);

            // Run transcription on a background thread to avoid blocking UI
            return await Task.Run(() => Transcribe(audioFilePath), cancellationToken);
        }

        private TranscriptionResponse Transcribe(string audioFilePath)
        {
            string? convertedWavPath = null;
            try
            {
                // Convert audio to 16kHz mono WAV if needed
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

                // Read audio samples
                float[] samples;
                int sampleRate;
                double duration;

                using (var reader = new WaveFileReader(wavPath))
                {
                    sampleRate = reader.WaveFormat.SampleRate;
                    duration = reader.TotalTime.TotalSeconds;

                    // Convert to float samples
                    var sampleProvider = reader.ToSampleProvider();
                    var sampleList = new System.Collections.Generic.List<float>();

                    float[] buffer = new float[reader.WaveFormat.SampleRate];
                    int samplesRead;
                    while ((samplesRead = sampleProvider.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        sampleList.AddRange(buffer.Take(samplesRead));
                    }

                    samples = sampleList.ToArray();
                }

                // Create stream and perform transcription
                var stream = _recognizer.CreateStream();
                stream.AcceptWaveform(sampleRate, samples);
                _recognizer.Decode(stream);

                // Get result
                var result = stream.Result;
                var text = result.Text.Trim();

                // Detect language (Parakeet supports 25 European languages)
                // For now, we'll set it as "auto" since sherpa-onnx doesn't expose language detection
                // The model supports: en, de, es, fr, it, pt, pl, nl, ru, uk, cs, ro, hu, el, bg,
                // hr, sk, sl, lt, lv, et, ga, mt, cy, is
                string language = "auto";

                return new TranscriptionResponse
                {
                    Success = true,
                    Text = text,
                    Language = language,
                    AudioDuration = duration,
                    OriginalFilename = Path.GetFileName(audioFilePath),
                    Provider = TranscriptionProvider.Local,
                    ErrorMessage = null
                };
            }
            catch (Exception ex)
            {
                return new TranscriptionResponse
                {
                    Success = false,
                    Text = "",
                    Language = "unknown",
                    AudioDuration = 0,
                    OriginalFilename = Path.GetFileName(audioFilePath),
                    Provider = TranscriptionProvider.Local,
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

        /// <summary>
        /// Convert audio file to 16kHz mono WAV format
        /// </summary>
        private string ConvertTo16kHzMonoWav(string inputPath)
        {
            var tempPath = Path.Combine(
                Path.GetTempPath(),
                $"sherpa_converted_{Guid.NewGuid()}.wav"
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

        /// <summary>
        /// Get provider information
        /// </summary>
        public TranscriptionProviderInfo GetProviderInfo()
        {
            return new TranscriptionProviderInfo
            {
                Provider = TranscriptionProvider.Local,
                ProviderName = "Local (sherpa-onnx)",
                Version = "Parakeet-TDT-0.6b-v3 (INT8 ONNX)",
                Status = _disposed ? "Disposed" : "Ready",
                RequiresNetwork = false,
                RequiresCredentials = false,
                SupportedLanguages = new[] { "25 European languages" },
                Description = "Offline CPU-based transcription using NVIDIA Parakeet model"
            };
        }

        /// <summary>
        /// Validate configuration (model files exist)
        /// </summary>
        public async Task<ValidationResult> ValidateConfigurationAsync()
        {
            return await Task.Run(() =>
            {
                if (!Directory.Exists(_modelPath))
                {
                    return new ValidationResult
                    {
                        IsValid = false,
                        ErrorMessage = $"Model directory not found: {_modelPath}",
                        ErrorType = ValidationErrorType.ModelNotFound
                    };
                }

                var requiredFiles = new[]
                {
                    "encoder.int8.onnx",
                    "decoder.int8.onnx",
                    "joiner.int8.onnx",
                    "tokens.txt"
                };

                foreach (var file in requiredFiles)
                {
                    var filePath = Path.Combine(_modelPath, file);
                    if (!File.Exists(filePath))
                    {
                        return new ValidationResult
                        {
                            IsValid = false,
                            ErrorMessage = $"Required model file not found: {file}",
                            ErrorType = ValidationErrorType.ModelNotFound
                        };
                    }
                }

                return new ValidationResult
                {
                    IsValid = true,
                    ErrorMessage = null,
                    ErrorType = ValidationErrorType.None
                };
            });
        }

        /// <summary>
        /// Check if service is available (model files exist)
        /// </summary>
        public bool IsAvailable()
        {
            return Directory.Exists(_modelPath) && !_disposed;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _recognizer?.Dispose();
                _disposed = true;
            }
        }
    }
}
