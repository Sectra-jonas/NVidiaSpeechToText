using System;
using SpeechToTextTray.Core.Models;
using SpeechToTextTray.Utils;

namespace SpeechToTextTray.Core.Services.Transcription
{
    /// <summary>
    /// Factory for creating transcription service instances based on configuration
    /// </summary>
    public static class TranscriptionServiceFactory
    {
        /// <summary>
        /// Create a transcription service based on configuration
        /// </summary>
        /// <param name="config">Transcription configuration</param>
        /// <returns>Transcription service instance</returns>
        /// <exception cref="ArgumentNullException">If config is null</exception>
        /// <exception cref="NotSupportedException">If provider is not supported</exception>
        /// <exception cref="InvalidOperationException">If service creation fails</exception>
        public static ITranscriptionService Create(TranscriptionConfig config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            Logger.Info($"Creating transcription service: {config.Provider}");

            try
            {
                return config.Provider switch
                {
                    TranscriptionProvider.Local => CreateLocalService(config.Local),
                    TranscriptionProvider.Azure => CreateAzureService(config.Azure),
                    TranscriptionProvider.AzureOpenAI => CreateAzureOpenAIService(config.AzureOpenAI),
                    _ => throw new NotSupportedException($"Provider {config.Provider} is not supported")
                };
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to create transcription service: {config.Provider}", ex);
                throw;
            }
        }

        /// <summary>
        /// Create local transcription service
        /// </summary>
        private static ITranscriptionService CreateLocalService(LocalTranscriptionConfig config)
        {
            try
            {
                return new LocalTranscriptionService(config.ModelPath);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to initialize local transcription service: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Create Azure transcription service
        /// </summary>
        private static ITranscriptionService CreateAzureService(AzureTranscriptionConfig config)
        {
            if (config == null)
                throw new InvalidOperationException("Azure configuration is missing");

            if (string.IsNullOrWhiteSpace(config.SubscriptionKey))
                throw new InvalidOperationException("Azure subscription key is required");

            if (string.IsNullOrWhiteSpace(config.Region))
                throw new InvalidOperationException("Azure region is required");

            try
            {
                return new AzureTranscriptionService(config);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to initialize Azure transcription service: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Create Azure OpenAI transcription service
        /// </summary>
        private static ITranscriptionService CreateAzureOpenAIService(AzureOpenAITranscriptionConfig config)
        {
            if (config == null)
                throw new InvalidOperationException("Azure OpenAI configuration is missing");

            if (string.IsNullOrWhiteSpace(config.Endpoint))
                throw new InvalidOperationException("Azure OpenAI endpoint is required");

            if (string.IsNullOrWhiteSpace(config.ApiKey))
                throw new InvalidOperationException("Azure OpenAI API key is required");

            if (string.IsNullOrWhiteSpace(config.DeploymentName))
                throw new InvalidOperationException("Azure OpenAI deployment name is required");

            try
            {
                return new AzureOpenAITranscriptionService(config);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to initialize Azure OpenAI transcription service: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Validate configuration without creating service
        /// Performs basic configuration checks before service instantiation
        /// </summary>
        /// <param name="config">Configuration to validate</param>
        /// <returns>Validation result</returns>
        public static ValidationResult ValidateConfiguration(TranscriptionConfig config)
        {
            if (config == null)
            {
                return new ValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "Configuration is null",
                    ErrorType = ValidationErrorType.MissingConfiguration
                };
            }

            return config.Provider switch
            {
                TranscriptionProvider.Local => ValidateLocalConfig(config.Local),
                TranscriptionProvider.Azure => ValidateAzureConfig(config.Azure),
                TranscriptionProvider.AzureOpenAI => ValidateAzureOpenAIConfig(config.AzureOpenAI),
                _ => new ValidationResult
                {
                    IsValid = false,
                    ErrorMessage = $"Unknown provider: {config.Provider}",
                    ErrorType = ValidationErrorType.Other
                }
            };
        }

        /// <summary>
        /// Validate local configuration
        /// </summary>
        private static ValidationResult ValidateLocalConfig(LocalTranscriptionConfig config)
        {
            // Local service has minimal configuration requirements
            // Model path is optional (defaults to bundled model)
            return new ValidationResult
            {
                IsValid = true,
                ErrorMessage = null,
                ErrorType = ValidationErrorType.None
            };
        }

        /// <summary>
        /// Validate Azure configuration
        /// </summary>
        private static ValidationResult ValidateAzureConfig(AzureTranscriptionConfig config)
        {
            if (config == null)
            {
                return new ValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "Azure configuration is missing",
                    ErrorType = ValidationErrorType.MissingConfiguration
                };
            }

            if (string.IsNullOrWhiteSpace(config.SubscriptionKey))
            {
                return new ValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "Azure subscription key is required",
                    ErrorType = ValidationErrorType.MissingConfiguration
                };
            }

            if (string.IsNullOrWhiteSpace(config.Region))
            {
                return new ValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "Azure region is required",
                    ErrorType = ValidationErrorType.MissingConfiguration
                };
            }

            return new ValidationResult
            {
                IsValid = true,
                ErrorMessage = null,
                ErrorType = ValidationErrorType.None
            };
        }

        /// <summary>
        /// Validate Azure OpenAI configuration
        /// </summary>
        private static ValidationResult ValidateAzureOpenAIConfig(AzureOpenAITranscriptionConfig config)
        {
            if (config == null)
            {
                return new ValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "Azure OpenAI configuration is missing",
                    ErrorType = ValidationErrorType.MissingConfiguration
                };
            }

            if (string.IsNullOrWhiteSpace(config.Endpoint))
            {
                return new ValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "Azure OpenAI endpoint is required",
                    ErrorType = ValidationErrorType.MissingConfiguration
                };
            }

            if (string.IsNullOrWhiteSpace(config.ApiKey))
            {
                return new ValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "Azure OpenAI API key is required",
                    ErrorType = ValidationErrorType.MissingConfiguration
                };
            }

            if (string.IsNullOrWhiteSpace(config.DeploymentName))
            {
                return new ValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "Azure OpenAI deployment name is required",
                    ErrorType = ValidationErrorType.MissingConfiguration
                };
            }

            return new ValidationResult
            {
                IsValid = true,
                ErrorMessage = null,
                ErrorType = ValidationErrorType.None
            };
        }
    }
}
