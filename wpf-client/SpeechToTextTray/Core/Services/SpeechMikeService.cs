using System;
using SpeechToTextTray.Core.Helpers;
using SpeechToTextTray.Utils;

namespace SpeechToTextTray.Core.Services
{
    /// <summary>
    /// Service for managing Philips SpeechMike device integration
    /// Wraps SpeechMikeCtrl and provides a clean event-driven interface
    /// </summary>
    public class SpeechMikeService : IDisposable
    {
        private SpeechMikeCtrl? _speechMike;
        private bool _disposed = false;

        /// <summary>
        /// Event fired when the record button is pressed or released
        /// </summary>
        public event EventHandler<RecordingActionEventArgs>? RecordingAction;

        /// <summary>
        /// Gets whether a SpeechMike device is currently connected and active
        /// </summary>
        public bool IsDeviceConnected => _speechMike != null && _speechMike.NumberOfDevices > 0;

        /// <summary>
        /// Initializes the SpeechMike device and subscribes to button events
        /// </summary>
        /// <returns>True if initialization succeeded, false otherwise</returns>
        public bool Initialize()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(SpeechMikeService));

            try
            {
                Logger.Info("Initializing SpeechMike service...");

                // Create SpeechMike control instance
                _speechMike = new SpeechMikeCtrl();

                // Check if any devices are connected
                if (_speechMike.NumberOfDevices == 0)
                {
                    Logger.Warning("No SpeechMike devices found");
                    _speechMike.Dispose();
                    _speechMike = null;
                    return false;
                }

                // Subscribe to button events
                _speechMike.SpMikeButtonEvent += OnSpeechMikeButton;

                Logger.Info($"SpeechMike initialized successfully: {_speechMike.DeviceTypeName} ({_speechMike.NumberOfDevices} device(s) connected)");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to initialize SpeechMike", ex);

                // Clean up on failure
                if (_speechMike != null)
                {
                    try
                    {
                        _speechMike.Dispose();
                    }
                    catch
                    {
                        // Ignore disposal errors
                    }
                    _speechMike = null;
                }

                return false;
            }
        }

        /// <summary>
        /// Handles button events from the SpeechMike device
        /// </summary>
        private void OnSpeechMikeButton(object? sender, SpMikeButtonEventArgs e)
        {
            try
            {
                switch (e.EventId)
                {
                    case SpMikeEventId.Record:
                        Logger.Info("SpeechMike Record button pressed");
                        RecordingAction?.Invoke(this, new RecordingActionEventArgs(true));
                        break;

                    case SpMikeEventId.Stop:
                        Logger.Info("SpeechMike Stop event triggered");
                        RecordingAction?.Invoke(this, new RecordingActionEventArgs(false));
                        break;

                    default:
                        // Ignore other button events (Play, FastForward, Rewind, etc.)
                        Logger.Info($"SpeechMike button event (ignored): {e.EventId}");
                        break;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error handling SpeechMike button event: {e.EventId}", ex);
            }
        }

        /// <summary>
        /// Sets the recording LED indicator on the SpeechMike device
        /// </summary>
        /// <param name="recording">True to turn on LED, false to turn off</param>
        public void SetRecordingIndicator(bool recording)
        {
            if (_speechMike != null)
            {
                try
                {
                    _speechMike.RecordingIndicator = recording;
                    Logger.Info($"SpeechMike LED indicator: {(recording ? "ON" : "OFF")}");
                }
                catch (Exception ex)
                {
                    Logger.Warning($"Failed to set SpeechMike recording indicator: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Disposes the SpeechMike service and releases device resources
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                Logger.Info("Disposing SpeechMike service...");

                if (_speechMike != null)
                {
                    try
                    {
                        // Unsubscribe from events
                        _speechMike.SpMikeButtonEvent -= OnSpeechMikeButton;

                        // Turn off LED before disposing
                        _speechMike.RecordingIndicator = false;

                        // Dispose device
                        _speechMike.Dispose();
                        Logger.Info("SpeechMike service disposed successfully");
                    }
                    catch (Exception ex)
                    {
                        Logger.Warning($"Error disposing SpeechMike: {ex.Message}");
                    }
                    finally
                    {
                        _speechMike = null;
                    }
                }

                _disposed = true;
            }
        }
    }

    /// <summary>
    /// Event arguments for recording action events (start/stop)
    /// </summary>
    public class RecordingActionEventArgs : EventArgs
    {
        /// <summary>
        /// True if recording should start, false if recording should stop
        /// </summary>
        public bool StartRecording { get; }

        public RecordingActionEventArgs(bool startRecording)
        {
            StartRecording = startRecording;
        }
    }
}
