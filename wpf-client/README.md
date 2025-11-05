# Speech-to-Text Windows Tray Application

A Windows 11 system tray application for real-time speech-to-text transcription using NVIDIA Parakeet model. Record audio with a global hotkey and automatically insert transcribed text into any application.

## Features

- 🎯 **Global Hotkey**: Toggle recording from anywhere with a configurable keyboard shortcut
- 🎤 **Audio Device Selection**: Choose from any Windows recording device
- ⚡ **Automatic Text Injection**: Transcribed text is automatically inserted into the focused window
- 🔔 **Visual Indicators**: System tray icon changes color based on state (idle/recording/processing)
- ⚙️ **GUI Configuration**: User-friendly settings window
- 🔄 **Clipboard Fallback**: Automatically copies to clipboard if text injection fails
- 📝 **Logging**: Detailed logs for debugging and troubleshooting

## Prerequisites

### Required Software
- **Windows 11** (or Windows 10)
- **.NET 8.0 SDK or Runtime** - [Download](https://dotnet.microsoft.com/download/dotnet/8.0)
- **Visual Studio 2022** (for building from source) - Community Edition is free
- **Backend Server**: The FastAPI backend must be running (see main README)

### System Requirements
- Microphone/audio input device
- Minimum 4GB RAM
- Windows with .NET 8.0 runtime installed

## Installation

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
   - Or run the executable: `bin\Release\net6.0-windows\SpeechToTextTray.exe`

### Option 2: Publish as Single Executable

Create a self-contained executable that includes the .NET runtime:

```bash
cd wpf-client/SpeechToTextTray
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

The executable will be in: `bin\Release\net8.0-windows\win-x64\publish\SpeechToTextTray.exe`

## Usage

### First Launch

1. **Start the Backend Server**
   - Make sure the FastAPI backend is running at `http://localhost:8000`
   - See main project README for backend setup

2. **Launch the Application**
   - Run `SpeechToTextTray.exe`
   - The app will appear in the system tray (look for microphone icon)
   - No main window will appear (it's a tray-only application)

3. **Configure Settings** (Optional)
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
| **Backend URL** | FastAPI server address | http://localhost:8000 |
| **Start with Windows** | Launch on Windows startup | False |
| **Show Notifications** | Display toast notifications | True |
| **Auto-inject Text** | Automatically insert transcribed text | True |
| **Fallback to Clipboard** | Copy to clipboard if injection fails | True |
| **Play Sound Effects** | Audio feedback on recording start/stop | False |
| **Timeout (seconds)** | API request timeout | 120 |

### Changing the Hotkey

1. Open Settings window (right-click tray icon → Settings)
2. Click in the "Recording Hotkey" textbox
3. Press your desired key combination (must include at least one modifier: Ctrl, Alt, Shift, or Win)
4. Click "Save"

**Note**: If the hotkey is already in use by another application, registration will fail and a warning notification will appear.

## Architecture

### Project Structure

```
SpeechToTextTray/
├── Core/
│   ├── Services/           # Core business logic
│   │   ├── AudioRecordingService.cs       (NAudio integration)
│   │   ├── BackendApiClient.cs            (FastAPI HTTP client)
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
├── Resources/              # Assets
│   └── Icons/              # Tray icons (.ico files)
├── App.xaml/cs             # Application entry point
└── MainWindow.xaml/cs      # Hidden main window
```

### Key Technologies

- **WPF (.NET 8.0)**: UI framework
- **Hardcodet.NotifyIcon.Wpf**: System tray functionality
- **NHotkey.Wpf**: Global hotkey registration (no admin required)
- **NAudio**: Audio capture from Windows devices
- **Win32 SendInput API**: Text injection via P/Invoke
- **HttpClient**: REST API communication with FastAPI backend

### Data Flow

```
User presses hotkey
  → GlobalHotkeyService raises event
  → App.xaml.cs: StartRecording()
  → AudioRecordingService captures audio (16kHz WAV)
  → User presses hotkey again
  → App.xaml.cs: StopRecordingAsync()
  → BackendApiClient uploads WAV to FastAPI
  → Receive transcription JSON response
  → TextInjectionService injects text into active window
  → Clean up temp file
  → Return to idle state
```

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

### Backend Connection Failed

**Issue**: "Backend Not Available" notification appears

**Solutions**:
- Ensure FastAPI backend is running: `python backend/app.py`
- Check backend URL is correct in settings (default: http://localhost:8000)
- Test backend manually: Open http://localhost:8000 in browser
- Check firewall isn't blocking connections
- Use "Test" button in Settings to diagnose

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

- `Hotkey registered: Ctrl + Shift + Space` - Hotkey setup successful
- `Backend is not reachable` - Backend server offline
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

2. **Long Recordings**: Very long recordings (>5 minutes) may timeout
   - **Workaround**: Increase timeout in settings
   - **Workaround**: Keep recordings under 3-4 minutes

3. **Some Applications**: Certain applications (games, IDEs) may not accept SendInput
   - **Workaround**: Use clipboard fallback (enabled by default)

## Future Enhancements

- [ ] Windows 10 Toast Notifications integration
- [ ] Sound effects for recording start/stop
- [ ] Visual overlay indicator (floating window)
- [ ] Recording history viewer
- [ ] Custom text post-processing rules
- [ ] Multiple hotkey support (e.g., one for dictation, one for commands)
- [ ] Support for other ASR backends
- [ ] Auto-update mechanism

## License

See main project README for license information.

## Support

For issues, questions, or feature requests:
1. Check logs: `%APPDATA%\SpeechToTextTray\logs\`
2. Review backend logs: `backend/` directory
3. Ensure backend is running and healthy
4. Open an issue in the project repository

## Credits

- **NVIDIA NeMo & Parakeet**: ASR model
- **NAudio**: Audio recording library
- **Hardcodet.NotifyIcon.Wpf**: System tray functionality
- **NHotkey**: Global hotkey registration
