# Speech-to-Text Windows Tray Application (Standalone)

A Windows 11 system tray application for real-time speech-to-text transcription using NVIDIA Parakeet ONNX model. Record audio with a global hotkey and automatically insert transcribed text into any application. **Completely standalone - no backend or internet required (for local provider)!**

## Table of Contents

- [Features](#features)
- [Quick Start](#quick-start)
- [Prerequisites](#prerequisites)
- [Installation](#installation)
- [Usage](#usage)
- [Configuration](#configuration)
  - [Choosing a Provider](#choosing-a-provider)
  - [Configuring Transcription Provider](#configuring-transcription-provider)
- [Security Considerations](#security-considerations)
- [Performance Notes](#performance-notes)
- [Architecture](#architecture)
- [Troubleshooting](#troubleshooting)
- [Development](#development)
- [Known Issues](#known-issues)
- [Future Enhancements](#future-enhancements)
- [Credits](#credits)

## Features

- 🎯 **Global Hotkey**: Toggle recording from anywhere with a configurable keyboard shortcut
- 🎤 **Audio Device Selection**: Choose from any Windows recording device with automatic fallback
- ⚡ **Automatic Text Injection**: Transcribed text is automatically inserted into the focused window using optimized Win32 SendInput
- 🔔 **Visual Indicators**: System tray icon changes color based on state (idle/recording/processing)
- ⚙️ **GUI Configuration**: User-friendly settings window with provider selection and test connections
- 🔄 **Clipboard Fallback**: Automatically copies to clipboard if text injection fails
- 📝 **Logging**: Detailed logs for debugging and troubleshooting
- 🛡️ **Resilient Operation**: Automatic audio device fallback prevents crashes when devices are unplugged
- ⚠️ **Error Notifications**: Clear user notifications for transcription failures with specific error messages
- 🔌 **Multi-Provider Support**: Choose your transcription provider
  - **Local (Default)**: Offline ONNX transcription using sherpa-onnx, 25 European languages, no internet required
  - **Azure Speech Service**: Cloud-based recognition, 100+ languages with auto-detection, requires subscription
  - **Azure OpenAI Whisper**: OpenAI's Whisper model on Azure, excellent English transcription, 50+ languages with translation to English
- ⚙️ **CPU-Only (Local)**: Local provider works on all Windows machines without GPU requirements
- 🎙️ **Philips SpeechMike Integration**: Optional hardware button support for professional dictation devices
  - Record/Stop button hardware integration (no keyboard hotkey needed)
  - LED indicator syncs with recording state
  - Automatic COM device detection and registration-free COM
  - Enable/disable in settings
- 🔮 **Extensible**: Easy to add future providers (Google Cloud, AWS)

## Prerequisites

### Required Software
- **Windows 11** (or Windows 10)
- **.NET 8.0 SDK or Runtime** - [Download](https://dotnet.microsoft.com/download/dotnet/8.0)
- **Visual Studio 2022** (for building from source) - Community Edition is free

### System Requirements
- Microphone/audio input device
- Minimum 4GB RAM (8GB recommended)
- ~1GB disk space (640MB for local ONNX model)
- Windows with .NET 8.0 runtime installed
- **No GPU required** - local provider runs on CPU
- **Internet optional** - Local provider works offline; Azure providers require internet connection
- **Azure Subscription (Optional)** - Required only if using Azure Speech Service or Azure OpenAI Whisper providers

## Quick Start

Want to get started immediately? Here's the 5-minute guide:

1. **Install .NET 8.0 Runtime** - [Download here](https://dotnet.microsoft.com/download/dotnet/8.0)
2. **Download ONNX model files** - See [Prerequisites: ONNX Model Files](#prerequisites-onnx-model-files) (~640MB download required)
3. **Build or download** `SpeechToTextTray.exe` (see [Installation](#installation) below)
4. **Run the application** - A microphone icon appears in your system tray
5. **Press Ctrl+Shift+Space** to start recording
6. **Speak clearly** into your microphone
7. **Press Ctrl+Shift+Space again** to stop
8. **Text appears automatically** in your active window!

**That's it!** The local provider works offline with zero configuration. No accounts, no API keys, no internet needed.

To use cloud providers or customize settings, right-click the tray icon → Settings.

## Installation

### Prerequisites: ONNX Model Files

**IMPORTANT:** Before building, you MUST download the ONNX model files (~640MB). The application will not work without these files.

#### Download Instructions

1. **Download the model archive:**

   **Option A - Direct download (Recommended for Windows):**
   - Download: https://github.com/k2-fsa/sherpa-onnx/releases/download/asr-models/sherpa-onnx-nemo-parakeet-tdt-0.6b-v3-int8.tar.bz2
   - Extract using 7-Zip, WinRAR, or Windows built-in extraction

   **Option B - Command line (with wget/tar):**
   ```bash
   cd wpf-client/SpeechToTextTray
   wget https://github.com/k2-fsa/sherpa-onnx/releases/download/asr-models/sherpa-onnx-nemo-parakeet-tdt-0.6b-v3-int8.tar.bz2
   tar xvf sherpa-onnx-nemo-parakeet-tdt-0.6b-v3-int8.tar.bz2
   rm sherpa-onnx-nemo-parakeet-tdt-0.6b-v3-int8.tar.bz2
   ```

2. **Place model files in correct location:**
   - Create directory: `wpf-client/SpeechToTextTray/Models/sherpa-onnx-nemo-parakeet-tdt-0.6b-v3-int8/`
   - Copy these 4 files from the extracted archive into the directory:
     - `encoder.int8.onnx` (~622MB)
     - `decoder.int8.onnx` (~12MB)
     - `joiner.int8.onnx` (~6MB)
     - `tokens.txt` (~92KB)

3. **Verify files are in correct location:**
   ```
   SpeechToTextTray/
   └── Models/
       └── sherpa-onnx-nemo-parakeet-tdt-0.6b-v3-int8/
           ├── encoder.int8.onnx
           ├── decoder.int8.onnx
           ├── joiner.int8.onnx
           └── tokens.txt
   ```

**Total size:** ~640MB

**Why separate download?**
- Git repositories have file size limits (GitHub max: 100MB per file)
- Model files are too large to commit to version control
- Allows users to update models independently of application code
- Keeps repository size small and clone times fast

**Model Information:**
- **Model**: NVIDIA NeMo Parakeet-TDT 0.6b v3 (INT8 quantized)
- **Languages**: 25 European languages with automatic language detection
- **Source**: sherpa-onnx project (Next-gen Kaldi)
- **License**: See sherpa-onnx repository for model license details

**Troubleshooting:**
- If app crashes on startup with "Model directory not found" error, verify model files are in `Models/sherpa-onnx-nemo-parakeet-tdt-0.6b-v3-int8/`
- If app crashes with "Encoder/Decoder/Joiner model not found" error, verify all 4 files exist
- Model files are automatically copied to output directory during build (configured in `.csproj`)
- Check logs in `%APPDATA%\SpeechToTextTray\logs\` for detailed error messages

### Option 1: Build from Source

1. **Open the Solution**
   ```bash
   cd wpf-client
   # Open SpeechToTextTray.sln in Visual Studio 2022
   ```

2. **Restore NuGet Packages**
   - Visual Studio will automatically restore packages on first build
   - Or manually: Right-click solution → Restore NuGet Packages

3. **Add Icon Resources** (Important!)
   - Navigate to `SpeechToTextTray/Resources/Icons/`
   - Add three `.ico` files: `tray-icon-idle.ico`, `tray-icon-recording.ico`, `tray-icon-processing.ico`
   - See `Resources/Icons/README.md` for details on creating/obtaining icons

4. **Build the Project**
   - Press `F6` or Build → Build Solution
   - Or use command line:
     ```bash
     dotnet build SpeechToTextTray.sln --configuration Release
     ```

5. **Run the Application**
   - Press `F5` in Visual Studio (Debug mode)
   - Or run the executable: `bin\Release\net8.0-windows\SpeechToTextTray.exe`

### Option 2: Publish as Single Executable

Create a self-contained executable that includes the .NET runtime:

```bash
cd wpf-client/SpeechToTextTray
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

The executable will be in: `bin\Release\net8.0-windows\win-x64\publish\SpeechToTextTray.exe`

## Usage

### First Launch

1. **Launch the Application**
   - Run `SpeechToTextTray.exe`
   - The app will appear in the system tray (look for microphone icon)
   - First startup takes 1-2 seconds to load the ONNX model
   - No main window will appear (it's a tray-only application)

2. **Configure Settings** (Optional)
   - Right-click the tray icon → Settings
   - Configure your preferred hotkey (default: Ctrl+Shift+Space)
   - Select your microphone device
   - Adjust other options as needed

### Recording and Transcription

1. **Start Recording**
   - Press your configured hotkey (default: `Ctrl + Shift + Space`)
   - Tray icon will change to indicate recording state (red icon)
   - Speak clearly into your microphone

2. **Stop Recording**
   - Press the hotkey again
   - Tray icon will show processing state (yellow/orange icon)
   - Wait for transcription (usually 1-3 seconds)

3. **Text Insertion**
   - Transcribed text will automatically appear in the focused window
   - If injection fails, text is copied to clipboard (press Ctrl+V to paste)
   - A notification will appear when transcription is complete

### Tray Icon Menu

Right-click the system tray icon for options:
- **Status**: Shows current application state
- **Settings**: Open configuration window
- **About**: View application information
- **Exit**: Close the application

## Configuration

Settings are stored in: `%APPDATA%\SpeechToTextTray\settings.json`

### Available Settings

| Setting | Description | Default |
|---------|-------------|---------|
| **Hotkey** | Global keyboard shortcut to toggle recording | Ctrl + Shift + Space |
| **Audio Device** | Microphone/input device ID | Default device |
| **Show Notifications** | Display toast notifications | True |
| **Auto-inject Text** | Automatically insert transcribed text | True |
| **Fallback to Clipboard** | Copy to clipboard if injection fails | True |
| **Enable SpeechMike** | Enable Philips SpeechMike hardware button integration | False |

### Changing the Hotkey

1. Open Settings window (right-click tray icon → Settings)
2. Click in the "Recording Hotkey" textbox
3. Press your desired key combination (must include at least one modifier: Ctrl, Alt, Shift, or Win)
4. Click "Save"

**Note**: If the hotkey is already in use by another application, registration will fail and a warning notification will appear.

### Philips SpeechMike Integration

The application supports optional integration with Philips SpeechMike professional dictation devices.

**Features:**
- Hardware Record/Stop button triggers recording (no keyboard hotkey needed)
- LED indicator on device syncs with recording state (red when recording)
- Automatic device detection at startup
- Works alongside keyboard hotkey - both methods can be used

**Requirements:**
- Philips SpeechMike device (tested with SpeechMike Premium)
- Device drivers installed (usually automatic via Windows Update)
- 64-bit Windows (registration-free COM components included)

**Configuration:**
1. Open Settings window (right-click tray icon → Settings)
2. Check "Enable SpeechMike integration"
3. Click "Save"
4. If device is connected, you'll see a log message: "SpeechMike integration enabled"
5. If device is not found, integration will be disabled (no error - it's optional)

**Usage:**
- Press the **Record button** on your SpeechMike to start recording
- LED turns red to indicate recording
- Press **Record button again** or **Stop button** to stop recording
- LED turns off when idle
- Keyboard hotkey continues to work normally

**Troubleshooting:**
- If SpeechMike buttons don't work, verify device is connected and drivers are installed
- Check Windows Device Manager for "Philips SpeechMike" under "Human Interface Devices"
- Restart application after connecting device
- Check logs in `%APPDATA%\SpeechToTextTray\logs\` for device detection messages
- If you don't have a SpeechMike, simply leave the checkbox unchecked

**Note**: SpeechMike integration is completely optional. The application works perfectly without it using keyboard hotkeys.

### Choosing a Provider

The application supports three transcription providers. Choose the one that best fits your needs:

| Feature | Local (sherpa-onnx) | Azure Speech Service | Azure OpenAI Whisper |
|---------|---------------------|----------------------|---------------------|
| **Cost** | Free | Pay per use (~$1/hour) | Pay per use (~$0.006/min) |
| **Internet Required** | ❌ No | ✅ Yes | ✅ Yes |
| **Setup Complexity** | None | Azure account needed | Azure OpenAI access needed |
| **Languages Supported** | 25 European | 100+ with auto-detect | 50+ (translates to English) |
| **Transcription Time** | 1-3 seconds | 2-5 seconds | 3-8 seconds |
| **Quality (English)** | Very Good | Excellent | Excellent |
| **Quality (Other Lang)** | Good | Excellent | Good (via translation) |
| **File Size Limit** | No limit | No limit | 25MB max |
| **Privacy** | 100% local | Cloud (Microsoft) | Cloud (OpenAI/Microsoft) |
| **Best For** | Offline, privacy, cost | Production, multilingual | English focus, accuracy |

**Recommendations:**
- **Use Local** if you want privacy, work offline, or don't want recurring costs
- **Use Azure Speech** for production apps needing many languages with best quality
- **Use Azure OpenAI Whisper** for the highest English transcription quality

### Configuring Transcription Provider

The application supports three transcription providers:

#### Local Provider (Default)
- **No configuration required** - works out of the box
- Offline transcription using bundled ONNX model
- Supports 25 European languages
- No internet or subscription required

#### Azure Speech Service Provider
1. **Get Azure Subscription**:
   - Visit [Azure Speech Service](https://azure.microsoft.com/services/cognitive-services/speech-services/)
   - Create a Speech resource in Azure Portal
   - Copy your subscription key and region

2. **Configure in Settings**:
   - Open Settings window (right-click tray icon → Settings)
   - Select "Azure Speech Service (Cloud)" provider
   - Enter your subscription key
   - Select your Azure region (e.g., "East US", "West Europe")
   - (Optional) Select a specific language or leave as "Auto-detect"
   - Click "Test" to verify connection
   - Click "Save"

3. **Requirements**:
   - Active internet connection
   - Valid Azure subscription key
   - Correct Azure region selected

**Note**: Azure Speech Service may incur costs based on usage. Check Azure pricing for details.

#### Azure OpenAI Whisper Provider
1. **Get Azure OpenAI Access**:
   - Visit [Azure OpenAI Service](https://azure.microsoft.com/products/ai-services/openai-service/)
   - Create an Azure OpenAI resource in Azure Portal
   - Deploy a Whisper model in Azure OpenAI Studio
   - Copy your endpoint URL, API key, and deployment name

2. **Configure in Settings**:
   - Open Settings window (right-click tray icon → Settings)
   - Select "Azure OpenAI Whisper (Cloud)" provider
   - Enter your endpoint URL (e.g., https://your-resource.openai.azure.com/)
   - Enter your API key
   - Enter your deployment name (from Azure OpenAI Studio)
   - (Optional) Select a specific language or leave as "Auto-detect"
   - (Optional) Enter a prompt to guide transcription
   - Click "Test" to verify connection
   - Click "Save"

3. **Requirements**:
   - Active internet connection
   - Valid Azure OpenAI API key and endpoint
   - Deployed Whisper model in Azure OpenAI
   - Audio files must be under 25MB

4. **Advantages**:
   - Excellent transcription quality for English
   - Supports 50+ languages with translation to English
   - Can use prompt to improve accuracy for specific terminology

**Note**: Azure OpenAI may incur costs based on usage. Check Azure OpenAI pricing for details.

## Security Considerations

### API Key Storage

**🔒 SECURE:** API keys for Azure providers are encrypted using Windows Data Protection API (DPAPI).

- **File Location**: `%APPDATA%\SpeechToTextTray\settings.json`
- **Encryption**: Uses Windows DPAPI with user-specific, machine-bound encryption
- **Protection**: API keys can only be decrypted by the same Windows user account on the same machine
- **Storage Format**: Keys stored as encrypted Base64 strings prefixed with `encrypted:`
- **Fallback**: Legacy plain text keys are automatically upgraded to encrypted format on save

**Security Benefits:**
- ✅ API keys are **not stored in plain text**
- ✅ **Machine and user-specific** - keys cannot be moved to another computer
- ✅ **No master password needed** - managed by Windows
- ✅ **Transparent encryption** - automatically encrypted on save, decrypted on load

**Recommendations:**
- ✅ **Do NOT share** your `settings.json` file with others (even encrypted, it may expose other settings)
- ✅ **Protect** your Windows user account with a strong password (DPAPI relies on Windows account security)
- ✅ **Use separate Azure subscriptions** for development and production
- ✅ **Monitor your Azure billing** regularly for unexpected usage
- ✅ **Revoke and regenerate** API keys if you suspect compromise
- ❌ **Do NOT commit** settings.json to version control systems

**Note**: If you move settings.json to another computer or Windows user account, encrypted API keys will fail to decrypt and will need to be re-entered.

### Cloud Provider Costs

Azure providers charge based on usage. Protect yourself from unexpected bills:

- **Azure Speech Service**: ~$1 per hour of audio transcribed
- **Azure OpenAI Whisper**: ~$0.006 per minute of audio
- **Set up billing alerts** in Azure Portal
- **Set spending limits** if available for your subscription type
- **Review usage** in Azure Cost Management regularly

### Network Security

- Cloud providers require outbound HTTPS connections
- Corporate firewalls/proxies may block Azure services
- All communication with Azure uses encrypted HTTPS
- Audio data is transmitted to Microsoft/OpenAI servers

### Process Security

- The app uses Win32 `SendInput` API for text injection
- Some antivirus software may flag this as suspicious behavior (false positive)
- **No administrator rights required** - app runs as normal user
- Text injection respects Windows security boundaries (cannot inject into elevated apps)
- Local transcription model processes audio entirely on your computer (no network transmission)

### Audio Device Fallback

- If a configured audio device is disconnected, the app automatically switches to the default device
- You'll receive a notification showing which device is being used
- The new device ID is automatically saved to settings
- This prevents application crashes but may record from an unexpected device

## Architecture

### Project Structure

```
SpeechToTextTray/
├── Core/
│   ├── Services/           # Core business logic
│   │   ├── Transcription/
│   │   │   ├── ITranscriptionService.cs           (Interface for all providers)
│   │   │   ├── LocalTranscriptionService.cs       (sherpa-onnx ONNX Runtime)
│   │   │   ├── AzureTranscriptionService.cs       (Azure Speech SDK)
│   │   │   ├── AzureOpenAITranscriptionService.cs (Azure OpenAI Whisper)
│   │   │   └── TranscriptionServiceFactory.cs     (Provider factory)
│   │   ├── AudioRecordingService.cs       (NAudio integration)
│   │   ├── TextInjectionService.cs        (Win32 SendInput API)
│   │   ├── GlobalHotkeyService.cs         (Hotkey registration)
│   │   └── SettingsService.cs             (Configuration persistence)
│   ├── Models/             # Data models
│   │   ├── AppSettings.cs
│   │   ├── TranscriptionResponse.cs
│   │   ├── AudioDevice.cs
│   │   └── RecordingState.cs
│   └── Helpers/            # Utility classes
│       └── TempFileManager.cs
├── UI/
│   ├── TrayIcon/           # System tray integration
│   │   └── TrayIconManager.cs
│   ├── Windows/            # Dialogs
│   │   └── SettingsWindow.xaml/cs
│   └── Controls/           # Custom controls
│       └── HotkeyTextBox.xaml/cs
├── Utils/                  # Utilities
│   ├── Logger.cs
│   └── NotificationHelper.cs
├── Models/                 # ONNX model files (~640MB)
│   └── sherpa-onnx-nemo-parakeet-tdt-0.6b-v3-int8/
│       ├── encoder.int8.onnx
│       ├── decoder.int8.onnx
│       ├── joiner.int8.onnx
│       └── tokens.txt
├── Resources/              # Assets
│   └── Icons/              # Tray icons (.ico files)
├── App.xaml/cs             # Application entry point
└── MainWindow.xaml/cs      # Hidden main window
```

### Key Technologies

- **WPF (.NET 8.0)**: UI framework
- **sherpa-onnx (1.12.15)**: ONNX Runtime for local speech recognition
- **Azure.AI.OpenAI (2.1.0)**: Azure OpenAI Whisper client library
- **Microsoft.CognitiveServices.Speech (1.43.0)**: Azure Speech Service SDK
- **Hardcodet.NotifyIcon.Wpf**: System tray functionality
- **NHotkey.Wpf**: Global hotkey registration (no admin required)
- **NAudio (2.2.1)** and **NAudio.WinMM (2.2.1)**: Audio capture from Windows devices
- **Win32 SendInput API**: Text injection via P/Invoke (optimized structure marshalling)
- **Parakeet-TDT ONNX Model**: INT8 quantized model (~640MB)

### Data Flow

```
User presses hotkey
  → GlobalHotkeyService raises event
  → App.xaml.cs: StartRecording()
  → AudioRecordingService captures audio (16kHz WAV)
  → User presses hotkey again
  → App.xaml.cs: StopRecordingAsync()
  → LocalTranscriptionService processes WAV locally
  → sherpa-onnx ONNX Runtime performs inference
  → Receive transcription text
  → TextInjectionService injects text into active window
  → Clean up temp file
  → Return to idle state
```

## Performance Notes

### Local Provider (sherpa-onnx)

**Transcription Performance:**
- Typical: **1-3 seconds** for 30-60 second audio recording
- Speed: Approximately **20x realtime** (30s audio = ~1.5s processing)
- First transcription: Add 1-2 seconds for initial model loading
- CPU-only inference, no GPU required
- Memory usage: ~300-500MB with model loaded in RAM

**Optimizations:**
- **Audio device pre-initialization** (reduces recording startup delay to near-zero)
- **Optimized Win32 SendInput** structure marshalling (commit 577bc89) for instant text injection
- Model kept in memory for fast subsequent transcriptions
- INT8 quantization reduces model size from ~1.2GB to ~640MB with minimal quality loss

### Azure Speech Service

- Typical: **2-5 seconds** depending on network latency and audio length
- Network latency adds 1-3 seconds compared to local
- Performance varies by Azure region (choose closest region)
- Concurrent request limits apply (check your subscription tier)

### Azure OpenAI Whisper

- Typical: **3-8 seconds** (higher API latency than Azure Speech)
- Network and API processing time
- 25MB file size limit (enforce in app, line 25 of `AzureOpenAITranscriptionService.cs`)
- Quality/latency tradeoff: highest English accuracy but slower

### General Performance Tips

- **Keep recordings under 2-3 minutes** for best user experience
- **Pre-opened audio device** eliminates capture delay (feature added in commit 1ce691f)
- **Automatic cleanup** of temp files prevents disk bloat
- **Log rotation** keeps last 7 days of logs to manage storage

### Hardware Requirements

- **CPU**: Any modern x64 processor (local provider uses CPU only)
- **RAM**: 4GB minimum, 8GB recommended (ONNX model uses ~500MB)
- **Disk**: ~1GB total (640MB model + temp files + logs)
- **Network**: Not required for local provider; broadband recommended for Azure providers

## Troubleshooting

### Application Won't Start

**Issue**: Application crashes on startup or doesn't appear in tray

**Solutions**:
- Ensure .NET 8.0 runtime is installed
- Check logs in `%APPDATA%\SpeechToTextTray\logs\`
- Try running from command line to see error messages
- Verify icon files exist in `Resources/Icons/`

### Hotkey Not Working

**Issue**: Global hotkey doesn't trigger recording

**Solutions**:
- Check if hotkey is already used by another application
- Try a different key combination
- Restart the application after changing hotkey
- Check Windows permissions (no admin required for RegisterHotKey API)

### Text Injection Fails

**Issue**: Transcribed text doesn't appear in target application

**Solutions**:
- Ensure "Automatically inject text" is enabled in settings
- Check if text was copied to clipboard (fallback behavior)
- Some elevated applications (running as Admin) may block text injection
- Try manually pasting with Ctrl+V
- Check logs for injection errors

### No Audio Recording

**Issue**: Recording doesn't capture audio

**Solutions**:
- Check microphone permissions in Windows Settings → Privacy → Microphone
- Verify correct device selected in Settings
- Test microphone in other applications (Voice Recorder, Discord, etc.)
- Check device is not muted or disabled
- Try selecting a different audio device

### Audio Device Changed Automatically

**Issue**: You see a notification that your audio device changed, or a different device is being used than configured

**Explanation**: This is expected behavior when your configured audio device is unavailable (unplugged, disabled, etc.)

**What Happens**:
- The app detects the configured device is missing on startup or when changing settings
- Automatically falls back to the default audio device (device 0)
- Shows a warning notification with the name of the device being used
- Saves the new device ID to settings to prevent repeated warnings

**Solutions**:
- If the new device is acceptable, no action needed - the app will continue using it
- If you want to use a different device, go to Settings and select your preferred device
- If your original device becomes available again, select it in Settings

**Note**: This fallback mechanism prevents the application from crashing when audio devices are unplugged.

### Transcription Returns Empty Text

**Issue**: Recording completes but no text appears

**Possible Causes**:
- No speech detected in the audio (background noise, silence, or very quiet speech)
- Microphone not picking up sound (muted, wrong device, low volume)
- Audio quality too poor for transcription
- Language mismatch (recording in language not supported by selected provider)

**Solutions**:
- Check the notification message - it will say "No Speech Detected" if this is the issue
- Verify microphone is working (test in Windows Voice Recorder)
- Speak more clearly and closer to the microphone
- Check microphone volume levels in Windows
- Try the local provider (more forgiving of audio quality)
- Check logs in `%APPDATA%\SpeechToTextTray\logs\` for detailed error messages

### Transcription Failed Error

**Issue**: You receive a "Transcription Failed" notification with an error message

**Common Causes**:
- **Network error** (Azure providers): Check internet connection, firewall, proxy settings
- **Invalid credentials** (Azure providers): Verify API key, region, or deployment name in Settings
- **Service unavailable** (Azure providers): Azure service may be down or experiencing issues
- **File too large** (Azure OpenAI): Audio file exceeds 25MB limit
- **Model not found** (Local provider): ONNX model files missing or corrupted

**Solutions**:
- Click "Test" button in Settings to verify your provider configuration
- Check the specific error message in the notification for details
- Review logs in `%APPDATA%\SpeechToTextTray\logs\` for detailed error information
- For Azure providers, verify your subscription is active and has available quota
- For local provider, verify model files exist in the Models/ directory

### High Memory Usage

**Issue**: Application uses excessive RAM

**Solutions**:
- Old temp files accumulate: Check `%TEMP%\SpeechToTextTray\`
- Application auto-cleans on startup (keeps last 10 recordings)
- Manually delete temp files if needed
- Restart application periodically

## Logs and Debugging

### Log Files

Logs are stored in: `%APPDATA%\SpeechToTextTray\logs\`

- Format: `app_YYYYMMDD.log`
- Logs are automatically rotated (kept for 7 days)
- Contains detailed error messages and stack traces

### Viewing Logs

```bash
# Windows Explorer
explorer %APPDATA%\SpeechToTextTray\logs

# Command line
type "%APPDATA%\SpeechToTextTray\logs\app_20250101.log"
```

### Common Log Messages

- `Services initialized (Local transcription)` - Application started successfully
- `Hotkey registered: Ctrl + Shift + Space` - Hotkey setup successful
- `Transcribing audio locally...` - Local ONNX inference in progress
- `Text injection failed, copied to clipboard` - Fallback behavior activated
- `Failed to register hotkey` - Hotkey conflict detected

## Development

### Adding New Features

1. **New Service**: Add to `Core/Services/`
2. **New Model**: Add to `Core/Models/`
3. **UI Changes**: Modify `UI/Windows/` or `UI/Controls/`
4. **Wire Up**: Update `App.xaml.cs` to initialize and connect

### Debugging in Visual Studio

1. Set breakpoints in code
2. Press F5 to run in debug mode
3. Use Output window for Debug.WriteLine messages
4. Watch variables and inspect state

### Building for Release

```bash
dotnet build -c Release
```

Release build is optimized and located in: `bin\Release\net8.0-windows\`

## Known Issues

1. **Elevated Applications**: Text injection may fail for applications running as Administrator
   - **Workaround**: Run SpeechToTextTray as Administrator (not recommended for security)
   - **Alternative**: Text is copied to clipboard, paste manually
   - **Reason**: Windows security prevents normal user processes from injecting input into elevated processes

2. **Long Recordings**: Transcription time increases with recording length
   - **Recommendation**: Keep recordings under 2-3 minutes for best experience
   - **Local provider**: CPU transcription ~20x realtime (60s audio = ~3s processing)
   - **Note**: Works on all machines without GPU, just takes longer

3. **Some Applications**: Certain applications (games, IDEs) may not accept SendInput
   - **Examples**: Full-screen games, certain protected text fields
   - **Workaround**: Use clipboard fallback (enabled by default)
   - **Alternative**: Click in text field before recording to ensure focus

4. **First Run**: Initial model loading takes 1-2 seconds
   - **Normal**: sherpa-onnx loads ONNX model into memory on startup
   - **Subsequent runs**: Model kept in memory, no delay

5. **Azure OpenAI Whisper File Size Limit**: Audio files over 25MB will fail
   - **Enforced by**: Azure OpenAI API (not configurable)
   - **Typical**: ~3 minutes at 16kHz mono WAV ≈ 3MB, so rarely an issue
   - **Workaround**: Keep recordings under 10 minutes to stay well below limit

6. **API Key Encryption - Machine and User Bound**: Azure provider API keys are encrypted with DPAPI
   - **Behavior**: Encrypted keys only work on the same machine and Windows user account
   - **Settings File Portability**: If you copy settings.json to another computer or user, encrypted API keys will fail to decrypt
   - **Solution**: Re-enter your API keys in Settings window on the new machine/user account
   - **Security**: Keys are protected by Windows DPAPI - no plain text storage

7. **Antivirus False Positives**: Some AV software may flag SendInput usage
   - **Reason**: Win32 SendInput API can be used maliciously (we use it legitimately)
   - **Solution**: Add exception for SpeechToTextTray.exe in your antivirus
   - **Safe**: Code is open source, no malicious behavior

8. **Network/Firewall Issues with Azure Providers**
   - **Corporate networks**: May block Azure services
   - **Proxy**: May require configuration (not currently supported)
   - **Solution**: Use local provider on restricted networks

## Future Enhancements

- [ ] GPU acceleration support via CUDA
- [ ] Windows 10 Toast Notifications integration
- [ ] Visual overlay indicator (floating window)
- [ ] Recording history viewer
- [ ] Custom text post-processing rules
- [ ] Multiple hotkey support (e.g., one for dictation, one for commands)
- [ ] Support for other ONNX ASR models
- [ ] Auto-update mechanism
- [ ] Model downloader/updater

## License

See main project README for license information.

## Support

For issues, questions, or feature requests:
1. Check logs: `%APPDATA%\SpeechToTextTray\logs\`
2. Verify ONNX model files are present in Models/ directory
3. Ensure .NET 8.0 runtime is installed
4. Open an issue in the project repository

## Credits

- **NVIDIA NeMo & Parakeet**: ASR model
- **sherpa-onnx (Next-gen Kaldi)**: ONNX Runtime framework
- **Azure OpenAI Whisper**: OpenAI's speech recognition model on Azure
- **Azure Speech Service**: Microsoft's cloud speech recognition
- **NAudio**: Audio recording library
- **Hardcodet.NotifyIcon.Wpf**: System tray functionality
- **NHotkey**: Global hotkey registration
