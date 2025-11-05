# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Speech-to-text application using NVIDIA Parakeet-TDT-0.6b-v3 model with multiple frontends:
- **Browser Frontend**: Web-based audio recording for portable use
- **Windows Tray App**: Native Windows 11 application with global hotkey support and automatic text injection
- **Shared Backend**: FastAPI server for transcription (supports both frontends)

Supports 25 European languages with automatic detection.

## Development Commands

### Backend Setup
```bash
# Create and activate virtual environment
python -m venv venv
venv\Scripts\activate  # Windows
source venv/bin/activate  # macOS/Linux

# Install dependencies
cd backend
pip install -r requirements.txt

# Optional: Install PyTorch with CUDA for GPU acceleration
pip install torch torchaudio --index-url https://download.pytorch.org/whl/cu118  # CUDA 11.8
pip install torch torchaudio --index-url https://download.pytorch.org/whl/cu121  # CUDA 12.1
```

### Running the Application

#### Backend (Required for all frontends)
```bash
cd backend
python app.py
# Server runs on http://localhost:8000
# First startup takes 2-5 minutes to download Parakeet model (~600MB)
```

#### Browser Frontend (Option 1)
```bash
cd frontend
python -m http.server 3000
# Open http://localhost:3000 in browser
```

#### Windows Tray App (Option 2)
```bash
cd wpf-client/SpeechToTextTray
dotnet build
dotnet run
# Or open SpeechToTextTray.sln in Visual Studio 2022
```

#### Alternative: Windows Batch Scripts
```bash
start_backend.bat   # Start Python backend
start_frontend.bat  # Start browser frontend
```

### Testing API Endpoints
```bash
# Health check
curl http://localhost:8000/health

# Model info
curl http://localhost:8000/model-info

# Transcribe audio file
curl -X POST http://localhost:8000/transcribe -F "audio=@recording.webm"
```

## Architecture

### Backend (FastAPI + NeMo)
- **app.py**: FastAPI application with CORS middleware, handles HTTP endpoints
- **model_service.py**: Singleton service managing NVIDIA Parakeet ASR model lifecycle. Loads model on startup, handles single/batch transcription
- **audio_utils.py**: Audio preprocessing (convert to 16kHz mono WAV using librosa, validate duration/format)
- **config.py**: Central configuration (model name, sample rate, file paths, CORS origins)

**Key Pattern**: Model loaded once at startup via singleton pattern (`get_model_service()`). Audio files converted to 16kHz WAV before transcription, then temp files cleaned up.

### Browser Frontend (Vanilla JS)
- **app.js**: MediaRecorder API for browser audio capture, FormData for multipart upload, state management for recording/processing/idle
- **index.html**: UI with microphone button, status indicators, transcription display
- **styles.css**: Modern responsive styling

**Flow**: Record via MediaRecorder → Create audio blob → POST to `/transcribe` → Display transcription with metadata (language, duration)

### Windows Tray App (C# WPF .NET 8)
- **App.xaml.cs**: Application lifecycle, service initialization, hotkey event handling
- **Core/Services**:
  - `GlobalHotkeyService` (NHotkey.Wpf - RegisterHotKey API, no admin needed)
  - `AudioRecordingService` (NAudio - 16kHz WAV recording from Windows devices)
  - `BackendApiClient` (HttpClient - multipart upload to FastAPI)
  - `TextInjectionService` (Win32 SendInput API via P/Invoke, clipboard fallback)
  - `SettingsService` (JSON persistence in %APPDATA%)
- **UI/TrayIcon**: System tray icon with state-based visuals (idle/recording/processing)
- **UI/Windows/SettingsWindow**: GUI for hotkey, device, backend URL configuration

**Key Features**:
- Global hotkey toggle (default: Ctrl+Shift+Space)
- Automatic text injection into focused window
- Device selection from Windows audio devices
- Tray icon color changes by state

**Flow**: Hotkey pressed → NAudio records WAV → POST to `/transcribe` → SendInput injects text → Cleanup temp files

### Data Flow
```
Browser (MediaRecorder) → Audio Blob (WebM)
  → POST /transcribe (multipart/form-data)
  → Audio conversion (librosa → 16kHz WAV)
  → Validation (duration check)
  → NeMo Parakeet transcription
  → JSON response {text, language, duration}
  → Display + cleanup temp files
```

## Configuration

All settings in `backend/config.py`:
- `MODEL_NAME`: "nvidia/parakeet-tdt-0.6b-v3" (can be changed to other NeMo ASR models)
- `SAMPLE_RATE`: 16000 (required by Parakeet)
- `MAX_AUDIO_DURATION`: 24 minutes (1440 seconds)
- `ALLOWED_FORMATS`: ['.wav', '.webm', '.mp3', '.flac', '.m4a', '.ogg']
- `ALLOWED_ORIGINS`: CORS whitelist for frontend URLs (localhost:3000, localhost:8000, 127.0.0.1 variants)

## Important Notes

- **First run**: Model downloads automatically from HuggingFace on first startup (~600MB, 2-5 minutes)
- **GPU vs CPU**: Model auto-detects CUDA. GPU provides 3-5x speedup. CPU works but slower
- **Temporary files**: Created in `backend/uploads/temp/`, auto-cleaned after each transcription
- **CORS**: Frontend must be in `ALLOWED_ORIGINS` or requests will fail
- **Browser requirements**: Microphone access requires HTTPS (localhost exempt) and user permission
- **Error handling**: Audio validation errors return 400, model errors return 503, transcription errors return 500

## File Structure Context

```
backend/
  app.py                 # FastAPI routes and lifecycle
  model_service.py       # NeMo ASR model singleton
  audio_utils.py         # Audio preprocessing with librosa/soundfile
  config.py              # All configuration constants
  requirements.txt       # Python dependencies
  uploads/temp/          # Temporary audio storage (auto-created)

frontend/
  index.html             # UI structure
  app.js                 # Recording and API logic
  styles.css             # Modern styling

wpf-client/
  SpeechToTextTray/
    Core/
      Services/          # Business logic (audio, API, hotkeys, text injection, settings)
      Models/            # Data models (AppSettings, AudioDevice, RecordingState)
      Helpers/           # Utilities (TempFileManager)
    UI/
      TrayIcon/          # System tray manager
      Windows/           # Settings dialog
      Controls/          # HotkeyTextBox custom control
    Utils/               # Logger, NotificationHelper
    App.xaml/cs          # WPF application entry point
    Resources/Icons/     # Tray icons (.ico files - idle, recording, processing)
  README.md              # WPF client documentation
```

## WPF Client Notes

- **Prerequisites**: .NET 8.0 SDK, Visual Studio 2022, icon files (see wpf-client/SpeechToTextTray/Resources/Icons/README.md)
- **NuGet Packages**: Hardcodet.NotifyIcon.Wpf, NHotkey.Wpf, NAudio 2.2.1
- **Settings Location**: %APPDATA%\SpeechToTextTray\settings.json
- **Logs**: %APPDATA%\SpeechToTextTray\logs\app_YYYYMMDD.log
- **Temp Files**: %TEMP%\SpeechToTextTray\ (auto-cleanup keeps last 10)
- **Text Injection**: Multi-tier approach (SendInput → SendKeys → Clipboard)
- **No Admin Required**: Uses RegisterHotKey API (not low-level hooks)
- **Elevated Apps**: Text injection may fail for apps running as Administrator (clipboard fallback works)
