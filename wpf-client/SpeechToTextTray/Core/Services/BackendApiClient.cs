using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using SpeechToTextTray.Core.Models;

namespace SpeechToTextTray.Core.Services
{
    /// <summary>
    /// Client for communicating with the FastAPI backend
    /// </summary>
    public class BackendApiClient : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;

        public BackendApiClient(string baseUrl, int timeoutSeconds = 120)
        {
            _baseUrl = baseUrl?.TrimEnd('/') ?? "http://localhost:8000";
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(timeoutSeconds)
            };
        }

        /// <summary>
        /// Transcribe an audio file
        /// </summary>
        /// <param name="audioFilePath">Path to the audio file (WAV, WebM, MP3, etc.)</param>
        /// <returns>Transcription response with text and metadata</returns>
        public async Task<TranscriptionResponse> TranscribeAsync(string audioFilePath)
        {
            if (!File.Exists(audioFilePath))
                throw new FileNotFoundException("Audio file not found", audioFilePath);

            using var form = new MultipartFormDataContent();
            using var fileStream = File.OpenRead(audioFilePath);
            using var streamContent = new StreamContent(fileStream);

            // Determine content type based on file extension
            var extension = Path.GetExtension(audioFilePath).ToLower();
            var contentType = extension switch
            {
                ".wav" => "audio/wav",
                ".webm" => "audio/webm",
                ".mp3" => "audio/mpeg",
                ".flac" => "audio/flac",
                ".m4a" => "audio/mp4",
                ".ogg" => "audio/ogg",
                _ => "application/octet-stream"
            };

            streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
            form.Add(streamContent, "audio", Path.GetFileName(audioFilePath));

            var response = await _httpClient.PostAsync($"{_baseUrl}/transcribe", form);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException(
                    $"Backend returned {response.StatusCode}: {errorContent}"
                );
            }

            var result = await response.Content.ReadFromJsonAsync<TranscriptionResponse>();

            if (result == null)
                throw new InvalidOperationException("Failed to deserialize transcription response");

            return result;
        }

        /// <summary>
        /// Check backend health status
        /// </summary>
        public async Task<HealthResponse> CheckHealthAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/health");
                response.EnsureSuccessStatusCode();
                var result = await response.Content.ReadFromJsonAsync<HealthResponse>();
                return result ?? throw new InvalidOperationException("Failed to deserialize health response");
            }
            catch (Exception ex)
            {
                throw new HttpRequestException("Failed to connect to backend", ex);
            }
        }

        /// <summary>
        /// Get model information
        /// </summary>
        public async Task<ModelInfo> GetModelInfoAsync()
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/model-info");
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<ModelInfo>();
            return result ?? throw new InvalidOperationException("Failed to deserialize model info response");
        }

        /// <summary>
        /// Quick check if backend is reachable
        /// </summary>
        public async Task<bool> IsBackendOnlineAsync()
        {
            try
            {
                using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(5));
                var response = await _httpClient.GetAsync($"{_baseUrl}/health", cts.Token);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}
