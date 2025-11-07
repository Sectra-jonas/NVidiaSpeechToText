using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using SpeechToTextTray.Core.Models;
using SpeechToTextTray.Utils;

namespace SpeechToTextTray.Core.Services
{
    /// <summary>
    /// Service for managing application settings persistence with encrypted API keys
    /// </summary>
    public class SettingsService
    {
        private const string SettingsFileName = "settings.json";
        private const string ENCRYPTION_PREFIX = "ENC:"; // Marker for encrypted values
        private readonly string _settingsDirectory;
        private readonly string _settingsPath;

        /// <summary>
        /// Event fired when settings are changed
        /// </summary>
        public event EventHandler<AppSettings>? SettingsChanged;

        public SettingsService()
        {
            _settingsDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "SpeechToTextTray"
            );
            _settingsPath = Path.Combine(_settingsDirectory, SettingsFileName);

            // Ensure directory exists
            Directory.CreateDirectory(_settingsDirectory);
        }

        /// <summary>
        /// Load settings from file, or return defaults if file doesn't exist
        /// </summary>
        public AppSettings LoadSettings()
        {
            try
            {
                if (File.Exists(_settingsPath))
                {
                    var json = File.ReadAllText(_settingsPath);
                    var settings = JsonSerializer.Deserialize<AppSettings>(json);

                    if (settings != null)
                    {
                        // Decrypt API keys if they're encrypted
                        DecryptSettings(settings);
                        return settings;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error loading settings", ex);
                // Fall through to return defaults
            }

            return GetDefaultSettings();
        }

        /// <summary>
        /// Save settings to file with encrypted API keys
        /// </summary>
        public void SaveSettings(AppSettings settings)
        {
            try
            {
                // Create a copy to avoid modifying the original settings object
                var settingsToSave = CloneSettings(settings);

                // Encrypt sensitive API keys before saving
                EncryptSettings(settingsToSave);

                var options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };

                var json = JsonSerializer.Serialize(settingsToSave, options);
                File.WriteAllText(_settingsPath, json);

                Logger.Info("Settings saved successfully with encrypted API keys");

                // Notify listeners (with original unencrypted settings)
                SettingsChanged?.Invoke(this, settings);
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to save settings", ex);
                throw new InvalidOperationException($"Failed to save settings: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Get default settings
        /// </summary>
        public AppSettings GetDefaultSettings()
        {
            return new AppSettings();
        }

        /// <summary>
        /// Get the settings file path (for debugging/display purposes)
        /// </summary>
        public string GetSettingsPath() => _settingsPath;

        /// <summary>
        /// Reset settings to defaults
        /// </summary>
        public void ResetToDefaults()
        {
            var defaults = GetDefaultSettings();
            SaveSettings(defaults);
        }

        /// <summary>
        /// Check if settings file exists
        /// </summary>
        public bool SettingsFileExists() => File.Exists(_settingsPath);

        #region Encryption Helper Methods

        /// <summary>
        /// Encrypt sensitive API keys in settings before saving
        /// </summary>
        private void EncryptSettings(AppSettings settings)
        {
            try
            {
                // Encrypt Azure Speech Service subscription key
                if (!string.IsNullOrEmpty(settings.Transcription.Azure.SubscriptionKey) &&
                    !settings.Transcription.Azure.SubscriptionKey.StartsWith(ENCRYPTION_PREFIX))
                {
                    settings.Transcription.Azure.SubscriptionKey =
                        ENCRYPTION_PREFIX + EncryptString(settings.Transcription.Azure.SubscriptionKey);
                }

                // Encrypt Azure OpenAI API key
                if (!string.IsNullOrEmpty(settings.Transcription.AzureOpenAI.ApiKey) &&
                    !settings.Transcription.AzureOpenAI.ApiKey.StartsWith(ENCRYPTION_PREFIX))
                {
                    settings.Transcription.AzureOpenAI.ApiKey =
                        ENCRYPTION_PREFIX + EncryptString(settings.Transcription.AzureOpenAI.ApiKey);
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to encrypt API keys", ex);
                throw;
            }
        }

        /// <summary>
        /// Decrypt sensitive API keys after loading settings
        /// </summary>
        private void DecryptSettings(AppSettings settings)
        {
            try
            {
                // Decrypt Azure Speech Service subscription key
                if (!string.IsNullOrEmpty(settings.Transcription.Azure.SubscriptionKey))
                {
                    if (settings.Transcription.Azure.SubscriptionKey.StartsWith(ENCRYPTION_PREFIX))
                    {
                        var encrypted = settings.Transcription.Azure.SubscriptionKey.Substring(ENCRYPTION_PREFIX.Length);
                        settings.Transcription.Azure.SubscriptionKey = DecryptString(encrypted);
                    }
                    else
                    {
                        // Legacy plain text key detected - will be encrypted on next save
                        Logger.Warning("Plain text Azure subscription key detected. Will be encrypted on next save.");
                    }
                }

                // Decrypt Azure OpenAI API key
                if (!string.IsNullOrEmpty(settings.Transcription.AzureOpenAI.ApiKey))
                {
                    if (settings.Transcription.AzureOpenAI.ApiKey.StartsWith(ENCRYPTION_PREFIX))
                    {
                        var encrypted = settings.Transcription.AzureOpenAI.ApiKey.Substring(ENCRYPTION_PREFIX.Length);
                        settings.Transcription.AzureOpenAI.ApiKey = DecryptString(encrypted);
                    }
                    else
                    {
                        // Legacy plain text key detected - will be encrypted on next save
                        Logger.Warning("Plain text Azure OpenAI API key detected. Will be encrypted on next save.");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to decrypt API keys", ex);
                throw;
            }
        }

        /// <summary>
        /// Encrypt a string using Windows DPAPI (Data Protection API)
        /// Encrypted data can only be decrypted by the same user on the same machine
        /// </summary>
        private string EncryptString(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
                return plainText;

            try
            {
                byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
                byte[] encryptedBytes = ProtectedData.Protect(
                    plainBytes,
                    null, // No additional entropy
                    DataProtectionScope.CurrentUser); // User-specific encryption

                return Convert.ToBase64String(encryptedBytes);
            }
            catch (Exception ex)
            {
                Logger.Error("Encryption failed", ex);
                throw new CryptographicException("Failed to encrypt string", ex);
            }
        }

        /// <summary>
        /// Decrypt a string using Windows DPAPI (Data Protection API)
        /// Can only decrypt data encrypted by the same user on the same machine
        /// </summary>
        private string DecryptString(string encryptedText)
        {
            if (string.IsNullOrEmpty(encryptedText))
                return encryptedText;

            try
            {
                byte[] encryptedBytes = Convert.FromBase64String(encryptedText);
                byte[] decryptedBytes = ProtectedData.Unprotect(
                    encryptedBytes,
                    null, // No additional entropy
                    DataProtectionScope.CurrentUser); // User-specific decryption

                return Encoding.UTF8.GetString(decryptedBytes);
            }
            catch (Exception ex)
            {
                Logger.Error("Decryption failed", ex);
                throw new CryptographicException("Failed to decrypt string. Settings may be corrupted or from another user.", ex);
            }
        }

        /// <summary>
        /// Create a deep clone of settings to avoid modifying the original object during encryption
        /// </summary>
        private AppSettings CloneSettings(AppSettings original)
        {
            // Use JSON serialization for deep cloning
            var json = JsonSerializer.Serialize(original);
            return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
        }

        #endregion
    }
}
