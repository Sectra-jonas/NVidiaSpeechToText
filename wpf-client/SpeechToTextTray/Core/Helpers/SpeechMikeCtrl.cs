using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using PIA.SpMikeCtrl;

namespace SpeechToTextTray.Core.Helpers
{

    /// <summary>
    /// Identifies a SpeechMike button event
    /// </summary>
    public enum SpMikeEventId
    {
        /// <summary>Record</summary>
        Record,

        /// <summary>PlayPressed</summary>
        Play,

        /// <summary>FastForwardPressed</summary>
        FastForward,

        /// <summary>FastRewindPressed</summary>
        FastRewind,

        /// <summary>Stop</summary>
        Stop,

        /// <summary> End of letter  </summary>
        EOL,

        /// <summary>
        /// Function key 1
        /// </summary>
        F1,

        /// <summary>
        /// Function key 2
        /// </summary>
        F2,

        /// <summary>
        /// Function key 3
        /// </summary>
        F3,

        /// <summary>
        /// Function key 4
        /// </summary>
        F4,

        /// <summary>
        /// Instruction button, -i-
        /// </summary>
        Instruction,

        /// <summary>
        /// Insert/overwrite
        /// </summary>
        InsOvr,

        /// <summary>
        /// Command key, rear button of SpeechMike.
        /// </summary>
        Command
    }

    /// <summary>
    /// SpeechMike button event arguments
    /// </summary>
    public class SpMikeButtonEventArgs : EventArgs
    {
        /// <summary>
        /// Identifies the button event
        /// </summary>
        public SpMikeEventId EventId { get; set; }
    }

    /// <summary>
    /// Wrapper class for Philips SpeechMike functionality
    /// </summary>
    public class SpeechMikeCtrl
    {
        private readonly CSpmDeviceInfo deviceInfo;
        private readonly List<CSpmDeviceInfo> allDevices = new List<CSpmDeviceInfo>();
        private SpeechMikeControl speechMike;
        private int lastDeviceId;
        private bool recordingIndicator = false;
        private bool playbackIndicator = false;

        /// <summary>
        /// Constructor
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1816:CallGCSuppressFinalize", Justification = "Complex object.")]
        public SpeechMikeCtrl()
        {
            if (!InitializeSpeechMike())
            {
                GC.SuppressFinalize(this);
                return;
            }

            if (NumberOfDevices == 0)
            {
                return;
            }

            deviceInfo = speechMike.get_DeviceInfo(0);
            try
            {
                speechMike.Activate();
            }
            catch (COMException ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] SpeechMikeCtrl ctor: COM exception caught while activating SpeechMike; will not be rethrown - [message={ex.Message}]; [ErrorCode={ex.ErrorCode}]");
                // Intentionally no rethrow
            }

            speechMike.SPMEventButton += new _DSpeechMikeControlEvents_SPMEventButtonEventHandler(SpMikeSpmEventButton);
            speechMike.SPMEventDeviceDisconnected += new _DSpeechMikeControlEvents_SPMEventDeviceDisconnectedEventHandler(SpMikeSpmEventDeviceDisconnected);
            PlayAndRecordToggleModeOn = !deviceInfo.DeviceTypeName.Contains("3210");
        }

        /// <summary>
        /// Destructor
        /// </summary>
        ~SpeechMikeCtrl()
        {
            Dispose(false);
        }

        /// <summary>
        /// SpeechMike button event
        /// </summary>
        public event EventHandler<SpMikeButtonEventArgs> SpMikeButtonEvent;

        /// <summary>
        /// Gets and sets the mode for behavior of the Record and Play button
        /// </summary>
        public bool PlayAndRecordToggleModeOn { get; set; }

        /// <summary>
        /// Gets the number of connected devices
        /// </summary>
        public int NumberOfDevices { get; set; }

        public string DeviceTypeName
        {
            get
            {
                if (deviceInfo == null)
                {
                    return string.Empty;
                }

                return deviceInfo.DeviceTypeName;
            }
        }

        /// <summary>
        /// Gets and sets the recording indicator on (if set to true) and off
        /// </summary>
        public bool RecordingIndicator
        {
            get
            {
                return recordingIndicator;
            }

            set
            {
                recordingIndicator = value;

                if (speechMike != null)
                {
                    try
                    {
                        //// 2016-11-11 UNJ: switched to DisplayState() instead of SetLED() as SetLED is not supported in Citrix/WTS according to Philips documentation; use would lead to 30s hang & exception
                        //// CHM: spmDisplayStateRecordInsert - The device is recording in insert mode. The record LED and the insert/overwrite LED are static green.
                        //// CHM: spmDisplayStateRecordOverwrite - The device is recording in overwrite mode. The record LED is static red.
                        spmDisplayState spmDisplayMode = recordingIndicator ? spmDisplayState.spmDisplayStateRecordOverwrite : spmDisplayState.spmDisplayStateStop;
                        speechMike.DisplayState(lastDeviceId, spmDisplayMode);
                    }
                    catch (Exception exception)
                    {
                        // Just write a message in the log if we fail to set led. Seems unnecessary to crash the app just because we
                        // failed to update the LED on the SpeechMike.
                        System.Diagnostics.Debug.WriteLine($"[ERROR] Failed to set DisplayState. Error: {exception}");
                    }
                }
            }
        }

        /// <summary>
        /// Get and sets the status of playback
        /// </summary>
        public bool PlaybackIndicator
        {
            get
            {
                return playbackIndicator;
            }

            set
            {
                playbackIndicator = value;

                // might lite a LED here....
            }
        }

        public static string GetDeviceDllsString()
        {
            StringBuilder sb = new StringBuilder();

            List<DllInfo> dllInfoList = GetDeviceDllsList();
            foreach (DllInfo dllInfo in dllInfoList)
            {
                sb.AppendFormat("File [{0}] found at [{1}]. FileVersion={2}; ProductVersion={3}.", dllInfo.FileName, dllInfo.Folder, dllInfo.FileVersion, dllInfo.ProductVersion).AppendLine();
            }

            if (sb.Length == 0)
            {
                sb.Append("No device DLLs found.");
            }

            return sb.ToString();
        }

        public static List<DllInfo> GetDeviceDllsList()
        {
            List<DllInfo> dllData = new List<DllInfo>();

            string programFilesX86Folder = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            string programFilesFolder = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            string currentFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            string[] foldersToCheckPhilips = new string[]
            {
                Path.Combine(programFilesX86Folder, @"Common Files\Philips Speech Shared\Components"),
                Path.Combine(programFilesFolder, @"Common Files\Philips Speech Shared\Components"),
                currentFolder
            };

            string[] dllNamesPhilips = new string[] { "SpMikeCtrl.dll" };

            foreach (string folder in foldersToCheckPhilips)
            {
                foreach (string dllName in dllNamesPhilips)
                {
                    string fileNameWithPath = Path.Combine(folder, dllName);
                    if (File.Exists(fileNameWithPath))
                    {
                        var myFileVersionInfo = System.Diagnostics.FileVersionInfo.GetVersionInfo(fileNameWithPath);
                        dllData.Add(new DllInfo(folder, dllName, myFileVersionInfo.FileVersion, myFileVersionInfo.ProductVersion));
                    }
                }
            }

            return dllData;
        }

        /// <summary>
        /// Whether switchclicks is configured.
        /// </summary>
        private bool SwitchClicks
        {
            get
            {
                foreach (var mike in this.allDevices)
                {
                    try
                    {
                        if (this.speechMike.SwitchClicks[mike.InstanceID])
                        {
                            return true;
                        }
                    }
                    catch (Exception) { }
                }

                return false;
            }
        }

        public string GetDeviceDetailsString()
        {
            StringBuilder sb = new StringBuilder();

            List<CSpmDeviceInfo> deviceList = GetDeviceDetailsList();
            if (deviceList == null)
            {
                sb.Append("Failed to initialize SpeechMike.");
            } else
            {
                sb.AppendFormat("Number of devices: {0}", NumberOfDevices);
                int i = 0;
                foreach (CSpmDeviceInfo deviceInfo in deviceList)
                {
                    sb.AppendLine().AppendFormat(
                        "Device #{0}: VendorID={1}; DeviceTypeName={2}; ProductID={3}; LFHNumber={4}; InstanceID={5}; SerialNumber={6}; SerialNumberEncrypted={7}; FirmwareVersion={8}; FirmwareVersionMajor={9}; FirmwareVersionMinor={10}",
                        i++,
                        deviceInfo.VendorID,
                        deviceInfo.DeviceTypeName,
                        deviceInfo.ProductID,
                        deviceInfo.LFHNumber,
                        deviceInfo.InstanceID,
                        deviceInfo.SerialNumber,
                        deviceInfo.SerialNumberEncrypted,
                        deviceInfo.FirmwareVersion,
                        deviceInfo.FirmwareVersionMajor,
                        deviceInfo.FirmwareVersionMinor);
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Get the firmware version that contains both the major and the minor versions.
        /// </summary>
        private string GetFullFirmwareVersion()
        {
            return $"{this.deviceInfo.FirmwareVersionMajor}.{this.deviceInfo.FirmwareVersionMinor}";
        }

        /// <summary>
        /// Whether the recording device is unsupported
        /// </summary>
        private bool UnsupportedRecordingDevice()
        {
            var modelMatch = new Regex(@"[^\d]*(?<model>\d+)").Match(deviceInfo.DeviceTypeName);

            if (modelMatch.Success)
            {
                var model = modelMatch.Groups["model"].Value;

                if (model.StartsWith("32") || model.StartsWith("30") || model.StartsWith("52") || model.StartsWith("62"))
                {
                    return true;
                } else
                {
                    return false;
                }
            } else
            {
                return true;
            }
        }

        /// <summary>
        /// Returns the modelname of the speechmike, including the family.
        /// </summary>
        private string ModelName(string family)
        {
            var modelMatch = new Regex(@"(?<prefix>[^\d]*)(?<model>\d+)").Match(deviceInfo.DeviceTypeName);
            var familyMatch = new Regex(@"^(\d+- )?(?<family>.*)$").Match(family);

            if (familyMatch.Success)
            {
                family = familyMatch.Groups["family"].Value;
            }

            if (modelMatch.Success)
            {
                var model = modelMatch.Groups["model"].Value;
                var prefix = modelMatch.Groups["prefix"].Value;
                prefix = model.StartsWith("35") && string.IsNullOrEmpty(prefix) ? "LFH" : "";

                return $"{family} - {prefix}{model}";
            } else
            {
                return $"{family} - {deviceInfo.DeviceTypeName}";
            }
        }

        public List<CSpmDeviceInfo> GetDeviceDetailsList()
        {
            List<CSpmDeviceInfo> deviceDetails = new List<CSpmDeviceInfo>();

            try
            {
                if (!InitializeSpeechMike())
                {
                    return null;
                }
            }
            catch
            {
                return null;
            }

            for (int i = 0; i < NumberOfDevices; i++)
            {
                deviceDetails.Add(speechMike.get_DeviceInfo(i));
            }

            return deviceDetails;
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Event handler for SpeechMike button events
        /// </summary>
        /// <param name="deviceId">The device instance identifier that the event originates from.</param>
        /// <param name="eventId">Identifier of the event.</param>
        public void SpMikeSpmEventButton(int deviceId, spmControlDeviceEvent eventId)
        {
            lastDeviceId = deviceId;
            if (SpMikeButtonEvent != null)
            {
                switch (eventId)
                {
                    case spmControlDeviceEvent.spmRecordPressed:
                        if (PlayAndRecordToggleModeOn && recordingIndicator)
                        {
                            SpMikeButtonEvent(this, new SpMikeButtonEventArgs { EventId = SpMikeEventId.Stop });
                        } else
                        {
                            SpMikeButtonEvent(this, new SpMikeButtonEventArgs { EventId = SpMikeEventId.Record });
                        }

                        break;
                    case spmControlDeviceEvent.spmRecordReleased:
                        if (!PlayAndRecordToggleModeOn)
                        {
                            SpMikeButtonEvent(this, new SpMikeButtonEventArgs { EventId = SpMikeEventId.Stop });
                        }

                        break;
                    case spmControlDeviceEvent.spmPlayStopTogglePressed:
                        if (PlayAndRecordToggleModeOn && playbackIndicator)
                        {
                            SpMikeButtonEvent(this, new SpMikeButtonEventArgs { EventId = SpMikeEventId.Stop });
                        } else
                        {
                            SpMikeButtonEvent(this, new SpMikeButtonEventArgs { EventId = SpMikeEventId.Play });
                        }

                        break;
                    case spmControlDeviceEvent.spmPlayStopToggleReleased:
                        // Continue, we do not want to send any event when a toggle button is released.
                        break;
                    case spmControlDeviceEvent.spmPlayPressed:
                        if (PlayAndRecordToggleModeOn && playbackIndicator)
                        {
                            SpMikeButtonEvent(this, new SpMikeButtonEventArgs { EventId = SpMikeEventId.Stop });
                        } else
                        {
                            SpMikeButtonEvent(this, new SpMikeButtonEventArgs { EventId = SpMikeEventId.Play });
                        }

                        break;
                    case spmControlDeviceEvent.spmPlayReleased:
                        if (!PlayAndRecordToggleModeOn)
                        {
                            SpMikeButtonEvent(this, new SpMikeButtonEventArgs { EventId = SpMikeEventId.Stop });
                        }

                        break;
                    case spmControlDeviceEvent.spmFastForwardPressed:
                        SpMikeButtonEvent(this, new SpMikeButtonEventArgs { EventId = SpMikeEventId.FastForward });
                        break;
                    case spmControlDeviceEvent.spmFastForwardReleased:
                        SpMikeButtonEvent(this, new SpMikeButtonEventArgs { EventId = SpMikeEventId.Stop });
                        break;
                    case spmControlDeviceEvent.spmFastRewindPressed:
                        SpMikeButtonEvent(this, new SpMikeButtonEventArgs { EventId = SpMikeEventId.FastRewind });
                        break;
                    case spmControlDeviceEvent.spmFastRewindReleased:
                        SpMikeButtonEvent(this, new SpMikeButtonEventArgs { EventId = SpMikeEventId.Stop });
                        break;
                    case spmControlDeviceEvent.spmEOLReleased:
                        SpMikeButtonEvent(this, new SpMikeButtonEventArgs { EventId = SpMikeEventId.EOL });
                        break;
                    case spmControlDeviceEvent.spmFunction1Pressed:
                        SpMikeButtonEvent(this, new SpMikeButtonEventArgs { EventId = SpMikeEventId.F1 });
                        break;
                    case spmControlDeviceEvent.spmFunction2Pressed:
                        SpMikeButtonEvent(this, new SpMikeButtonEventArgs { EventId = SpMikeEventId.F2 });
                        break;
                    case spmControlDeviceEvent.spmFunction3Pressed:
                        SpMikeButtonEvent(this, new SpMikeButtonEventArgs { EventId = SpMikeEventId.F3 });
                        break;
                    case spmControlDeviceEvent.spmFunction4Pressed:
                        SpMikeButtonEvent(this, new SpMikeButtonEventArgs { EventId = SpMikeEventId.F4 });
                        break;
                    case spmControlDeviceEvent.spmInsertPressed:
                        SpMikeButtonEvent(this, new SpMikeButtonEventArgs { EventId = SpMikeEventId.InsOvr });
                        break;
                    case spmControlDeviceEvent.spmCommandPressed:
                        SpMikeButtonEvent(this, new SpMikeButtonEventArgs { EventId = SpMikeEventId.Command });
                        break;
                    case spmControlDeviceEvent.spmInstructionPressed:
                        SpMikeButtonEvent(this, new SpMikeButtonEventArgs { EventId = SpMikeEventId.Instruction });
                        break;
                    default:
                        break;
                }
            }
        }

        /// <summary>
        /// The mike SPM event device disconnected.
        /// </summary>
        /// <param name="deviceId">The l device id.</param>
        public void SpMikeSpmEventDeviceDisconnected(int deviceId)
        {
            System.Diagnostics.Debug.WriteLine($"[INFO] SpMikeSpmEventDeviceDisconnected: {deviceId}");
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <param name="disposing">True if the object is being disposed, false if the function was called by the finalizer.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (speechMike == null)
            {
                // The device was not initialized or already released.
                return;
            }

            try
            {
                speechMike.Deactivate();
                speechMike.Deinitialize();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] SpeechMikeCtrl.Dispose: exception caught while calling Deactivate() and Deinitialize() on speech mike (will NOT be rethrown) - message: {ex.Message}");

                // Intentionally no rethrow
            }
            finally
            {
                speechMike = null;
            }
        }

        private bool InitializeSpeechMike()
        {
            try
            {
                speechMike = new SpeechMikeControl();
            }
            catch (COMException comException)
            {
                speechMike = null;

                // COM Class not registered
                if (comException.ErrorCode == -2147221164)
                {
                    throw new Exception(comException.Message, comException);
                }

                System.Diagnostics.Debug.WriteLine($"[ERROR] Initializing SpeechMikeCtrl failed. Reason={comException.Message}");
                return false;
            }
            catch (Exception ex)
            {
                speechMike = null;
                System.Diagnostics.Debug.WriteLine($"[ERROR] Initializing SpeechMikeCtrl failed. Reason={ex.Message}");
                return false;
            }

            try
            {
                speechMike.AnonymousUsageTracking = false;
                speechMike.Initialize(false);
            }
            catch (COMException ex)
            {
                /* Reinitialize error */
                if (ex.ErrorCode == -2147221404)
                {
                    return false;
                }

                System.Diagnostics.Debug.WriteLine($"[ERROR] Initializing SpeechMikeCtrl failed. Reason={ex.Message}");
            }

            NumberOfDevices = speechMike.get_NumberOfDevices((int)spmDeviceID.spmAllDevices);

            for (int i = 0; i < NumberOfDevices; ++i)
            {
                allDevices.Add(speechMike.get_DeviceInfo(i));
            }

            return true;
        }

        public class DllInfo
        {
            public DllInfo(string folder, string fileName, string fileVersion, string productVersion)
            {
                Folder = folder;
                FileName = fileName;
                FileVersion = fileVersion;
                ProductVersion = productVersion;
            }

            public string Folder
            {
                get;
                private set;
            }

            public string FileName
            {
                get;
                private set;
            }

            public string FileVersion
            {
                get;
                private set;
            }

            public string ProductVersion
            {
                get;
                private set;
            }
        }
    }
}
