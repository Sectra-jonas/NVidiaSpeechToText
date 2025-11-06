using System;
using System.Collections.Generic;
using System.Linq;
using NAudio.Wave;
using SpeechToTextTray.Core.Models;

namespace SpeechToTextTray.Core.Services
{
    /// <summary>
    /// Service for recording audio from Windows input devices using NAudio
    /// </summary>
    public class AudioRecordingService : IDisposable
    {
        private WaveInEvent? _waveIn;
        private WaveFileWriter? _writer;
        private string? _currentFilePath;
        private DateTime _recordingStartTime;
        private string _currentDeviceId = "default";

        /// <summary>
        /// Sample rate required by the backend (16kHz)
        /// </summary>
        private const int SampleRate = 16000;

        /// <summary>
        /// Buffer size in milliseconds - optimized for low latency
        /// </summary>
        private const int BufferMilliseconds = 50;

        /// <summary>
        /// Event fired when audio level changes (for visual feedback)
        /// </summary>
        public event EventHandler<float>? AudioLevelChanged;

        /// <summary>
        /// Gets whether recording is currently in progress
        /// </summary>
        public bool IsRecording { get; private set; }

        /// <summary>
        /// Gets the duration of the current/last recording
        /// </summary>
        public TimeSpan RecordingDuration =>
            IsRecording ? DateTime.Now - _recordingStartTime : TimeSpan.Zero;

        /// <summary>
        /// Get list of available audio input devices
        /// </summary>
        public List<AudioDevice> GetAvailableDevices()
        {
            var devices = new List<AudioDevice>();

            for (int i = 0; i < WaveInEvent.DeviceCount; i++)
            {
                try
                {
                    var caps = WaveInEvent.GetCapabilities(i);
                    devices.Add(new AudioDevice
                    {
                        Id = i.ToString(),
                        Name = caps.ProductName,
                        Channels = caps.Channels,
                        IsDefault = i == 0 // First device is typically default
                    });
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error getting device {i}: {ex.Message}");
                }
            }

            return devices;
        }

        /// <summary>
        /// Initialize audio device for recording. Call this once at startup.
        /// Pre-opens the audio device to eliminate startup delay.
        /// If the requested device is not available, falls back to the default device.
        /// </summary>
        /// <param name="deviceId">Device ID (from AudioDevice.Id), or null for default</param>
        /// <returns>The actual device ID that was initialized (may differ from requested if fallback occurred)</returns>
        public string Initialize(string deviceId)
        {
            _currentDeviceId = deviceId ?? "default";

            try
            {
                InitializeWaveIn(_currentDeviceId);
                return _currentDeviceId;
            }
            catch (ArgumentException ex)
            {
                // Device not available - log warning and fall back to default
                System.Diagnostics.Debug.WriteLine($"Requested audio device '{deviceId}' not available: {ex.Message}");
                System.Diagnostics.Debug.WriteLine("Falling back to default audio device");

                // Try default device (0)
                if (WaveInEvent.DeviceCount > 0)
                {
                    _currentDeviceId = "0";
                    InitializeWaveIn(_currentDeviceId);
                    return _currentDeviceId;
                }
                else
                {
                    // No audio devices available at all
                    throw new InvalidOperationException("No audio input devices available on this system", ex);
                }
            }
        }

        /// <summary>
        /// Change the audio device. Disposes the current device and initializes the new one.
        /// If the requested device is not available, falls back to the default device.
        /// </summary>
        /// <param name="deviceId">Device ID (from AudioDevice.Id), or null for default</param>
        /// <returns>The actual device ID that was initialized (may differ from requested if fallback occurred)</returns>
        public string ChangeDevice(string deviceId)
        {
            string newDeviceId = deviceId ?? "default";
            if (_currentDeviceId == newDeviceId)
                return _currentDeviceId;

            if (IsRecording)
                throw new InvalidOperationException("Cannot change device while recording");

            DisposeWaveIn();
            _currentDeviceId = newDeviceId;

            try
            {
                InitializeWaveIn(_currentDeviceId);
                return _currentDeviceId;
            }
            catch (ArgumentException ex)
            {
                // Device not available - log warning and fall back to default
                System.Diagnostics.Debug.WriteLine($"Requested audio device '{deviceId}' not available: {ex.Message}");
                System.Diagnostics.Debug.WriteLine("Falling back to default audio device");

                // Try default device (0)
                if (WaveInEvent.DeviceCount > 0)
                {
                    _currentDeviceId = "0";
                    InitializeWaveIn(_currentDeviceId);
                    return _currentDeviceId;
                }
                else
                {
                    // No audio devices available at all
                    throw new InvalidOperationException("No audio input devices available on this system", ex);
                }
            }
        }

        /// <summary>
        /// Initialize WaveInEvent with the specified device
        /// </summary>
        private void InitializeWaveIn(string deviceId)
        {
            // Parse device number
            int deviceNumber = 0;
            if (!string.IsNullOrEmpty(deviceId) && deviceId != "default")
            {
                if (!int.TryParse(deviceId, out deviceNumber))
                    deviceNumber = 0;
            }

            // Validate device exists
            if (deviceNumber < 0 || deviceNumber >= WaveInEvent.DeviceCount)
                throw new ArgumentException($"Invalid device ID: {deviceId}");

            // Create WaveIn event for recording with optimized buffer settings
            _waveIn = new WaveInEvent
            {
                DeviceNumber = deviceNumber,
                WaveFormat = new WaveFormat(SampleRate, 1), // 16kHz, mono
                BufferMilliseconds = BufferMilliseconds,     // Low latency buffer
                NumberOfBuffers = 3                          // Adequate for reliability
            };

            // Wire up event handlers (done once)
            _waveIn.DataAvailable += OnDataAvailable;
            _waveIn.RecordingStopped += OnRecordingStopped;
        }

        /// <summary>
        /// Handle incoming audio data
        /// </summary>
        private void OnDataAvailable(object? sender, WaveInEventArgs e)
        {
            // Write to file
            _writer?.Write(e.Buffer, 0, e.BytesRecorded);

            // Calculate audio level for visualization
            float level = CalculateLevel(e.Buffer, e.BytesRecorded);
            AudioLevelChanged?.Invoke(this, level);
        }

        /// <summary>
        /// Handle recording stopped event
        /// </summary>
        private void OnRecordingStopped(object? sender, StoppedEventArgs e)
        {
            if (e.Exception != null)
            {
                System.Diagnostics.Debug.WriteLine($"Recording error: {e.Exception.Message}");
            }
        }

        /// <summary>
        /// Start recording audio to a WAV file. Device must be initialized first.
        /// </summary>
        /// <param name="outputPath">Path where the WAV file will be saved</param>
        public void StartRecording(string outputPath)
        {
            if (IsRecording)
                throw new InvalidOperationException("Already recording");

            if (_waveIn == null)
                throw new InvalidOperationException("Audio device not initialized. Call Initialize() first.");

            // Create WAV file writer (fast operation)
            _writer = new WaveFileWriter(outputPath, _waveIn.WaveFormat);
            _currentFilePath = outputPath;

            // Start recording (fast - device already initialized)
            _recordingStartTime = DateTime.Now;
            _waveIn.StartRecording();
            IsRecording = true;
        }

        /// <summary>
        /// Stop recording and finalize the WAV file
        /// </summary>
        public void StopRecording()
        {
            if (!IsRecording)
                return;

            // Stop recording (but keep device open for next recording)
            _waveIn?.StopRecording();

            // Dispose writer
            _writer?.Dispose();
            _writer = null!;

            IsRecording = false;
        }

        /// <summary>
        /// Dispose WaveInEvent and unhook event handlers
        /// </summary>
        private void DisposeWaveIn()
        {
            if (_waveIn != null)
            {
                if (IsRecording)
                {
                    _waveIn.StopRecording();
                    IsRecording = false;
                }

                _waveIn.DataAvailable -= OnDataAvailable;
                _waveIn.RecordingStopped -= OnRecordingStopped;
                _waveIn.Dispose();
                _waveIn = null;
            }
        }

        /// <summary>
        /// Calculate audio level (0.0 to 1.0) from buffer
        /// </summary>
        private float CalculateLevel(byte[] buffer, int bytesRecorded)
        {
            float max = 0;

            // Convert bytes to 16-bit samples
            for (int i = 0; i < bytesRecorded; i += 2)
            {
                if (i + 1 < bytesRecorded)
                {
                    short sample = BitConverter.ToInt16(buffer, i);
                    float sampleValue = Math.Abs(sample / 32768f);
                    max = Math.Max(max, sampleValue);
                }
            }

            return max;
        }

        public void Dispose()
        {
            DisposeWaveIn();
            _writer?.Dispose();
        }
    }
}
