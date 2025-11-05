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

        /// <summary>
        /// Sample rate required by the backend (16kHz)
        /// </summary>
        private const int SampleRate = 16000;

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
        /// Start recording audio to a WAV file
        /// </summary>
        /// <param name="deviceId">Device ID (from AudioDevice.Id), or null for default</param>
        /// <param name="outputPath">Path where the WAV file will be saved</param>
        public void StartRecording(string deviceId, string outputPath)
        {
            if (IsRecording)
                throw new InvalidOperationException("Already recording");

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

            // Create WaveIn event for recording
            _waveIn = new WaveInEvent
            {
                DeviceNumber = deviceNumber,
                WaveFormat = new WaveFormat(SampleRate, 1) // 16kHz, mono
            };

            // Create WAV file writer
            _writer = new WaveFileWriter(outputPath, _waveIn.WaveFormat);
            _currentFilePath = outputPath;

            // Handle data available
            _waveIn.DataAvailable += (sender, e) =>
            {
                // Write to file
                _writer.Write(e.Buffer, 0, e.BytesRecorded);

                // Calculate audio level for visualization
                float level = CalculateLevel(e.Buffer, e.BytesRecorded);
                AudioLevelChanged?.Invoke(this, level);
            };

            // Handle recording stopped
            _waveIn.RecordingStopped += (sender, e) =>
            {
                if (e.Exception != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Recording error: {e.Exception.Message}");
                }
            };

            // Start recording
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

            _waveIn?.StopRecording();
            _waveIn?.Dispose();
            _waveIn = null!;

            _writer?.Dispose();
            _writer = null!;

            IsRecording = false;
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
            StopRecording();
        }
    }
}
