# Quick Start Guide - Windows Tray Application

Get up and running with the standalone Speech-to-Text Windows tray application in 5 minutes. **No backend or internet required!**

## Prerequisites Checklist

- [ ] Windows 11 (or Windows 10)
- [ ] .NET 8.0 Runtime or SDK installed - [Download](https://dotnet.microsoft.com/download/dotnet/8.0)
- [ ] Visual Studio 2022 (Community Edition is free) - [Download](https://visualstudio.microsoft.com/) *(only for building from source)*
- [ ] Microphone connected and working
- [ ] **ONNX model files downloaded (~640MB)** - See Step 1 below

## Step 1: Download ONNX Model Files (Required)

**CRITICAL**: The app will not work without the ONNX model files.

1. **Download the model archive** (~640MB):
   - Download: https://github.com/k2-fsa/sherpa-onnx/releases/download/asr-models/sherpa-onnx-nemo-parakeet-tdt-0.6b-v3-int8.tar.bz2

2. **Extract the archive** using 7-Zip, WinRAR, or Windows built-in extraction

3. **Place files in correct location**:
   - Navigate to: `wpf-client\SpeechToTextTray\Models\`
   - Create folder: `sherpa-onnx-nemo-parakeet-tdt-0.6b-v3-int8\`
   - Copy these 4 files into that folder:
     - `encoder.int8.onnx` (~622MB)
     - `decoder.int8.onnx` (~12MB)
     - `joiner.int8.onnx` (~6MB)
     - `tokens.txt` (~92KB)

4. **Verify file structure**:
   ```
   SpeechToTextTray\
   └── Models\
       └── sherpa-onnx-nemo-parakeet-tdt-0.6b-v3-int8\
           ├── encoder.int8.onnx
           ├── decoder.int8.onnx
           ├── joiner.int8.onnx
           └── tokens.txt
   ```

**Why separate download?** Git repositories have file size limits (100MB max per file). These model files are ~640MB total and must be downloaded separately.

## Step 2: Build and Run

### Option A: Using Visual Studio (Recommended)

1. Double-click `wpf-client\SpeechToTextTray.sln` to open in Visual Studio
2. Wait for NuGet packages to restore (happens automatically)
3. Press **F5** to build and run in debug mode
4. Look for the microphone icon in your system tray
5. First startup takes 1-2 seconds to load the ONNX model

### Option B: Using Command Line

```bash
# Navigate to project directory
cd wpf-client\SpeechToTextTray

# Restore packages and build
dotnet build

# Run the application
dotnet run
```

### Option C: Publish as Single Executable

Create a standalone .exe that includes the .NET runtime:

```bash
cd wpf-client\SpeechToTextTray
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

Executable will be in: `bin\Release\net8.0-windows\win-x64\publish\SpeechToTextTray.exe`

## Step 3: First Use

1. **Verify Tray Icon**: Look in the system tray (bottom-right corner) for a microphone icon
   - If you don't see it, click the ^ arrow to show hidden icons
   - Icon color indicates state: Blue/Gray (idle), Red (recording), Yellow (processing)

2. **Test Recording** (Default settings work immediately):
   - Click in any text field (Notepad, Word, browser, etc.)
   - Press `Ctrl + Shift + Space` to start recording
   - Speak clearly: "This is a test recording"
   - Press `Ctrl + Shift + Space` again to stop
   - Wait 1-3 seconds for transcription
   - Text should appear where your cursor is!

3. **Configure Settings** (Optional):
   - Right-click the tray icon → **Settings**
   - Review/change hotkey (default: `Ctrl + Shift + Space`)
   - Select your preferred microphone device
   - Choose transcription provider (Local is default, no setup needed)
   - Click **Save**

## Step 4: Optional - Cloud Providers

The app works perfectly offline with local transcription. If you want cloud-based transcription:

### Azure Speech Service (100+ languages)
1. Open Settings → Select "Azure Speech Service (Cloud)"
2. Enter your Azure subscription key and region
3. Click **Test** to verify connection
4. Click **Save**

### Azure OpenAI Whisper (Excellent English quality)
1. Open Settings → Select "Azure OpenAI Whisper (Cloud)"
2. Enter your endpoint URL, API key, and deployment name
3. Click **Test** to verify connection
4. Click **Save**

**Note**: Cloud providers require active internet connection and may incur costs. Local provider is free and works offline.

## Troubleshooting Quick Fixes

### "Model directory not found" error on startup?
- Verify ONNX model files are in `Models\sherpa-onnx-nemo-parakeet-tdt-0.6b-v3-int8\`
- Ensure all 4 files exist (encoder, decoder, joiner, tokens.txt)
- Check file names match exactly (case-sensitive)
- See Step 1 above for download instructions

### Hotkey not working?
- Try a different key combination in Settings
- Make sure the hotkey isn't used by another program
- Restart the app after changing the hotkey
- Hotkey must include at least one modifier key (Ctrl, Alt, Shift, or Win)

### No audio recording?
- Check Windows microphone permissions: Settings → Privacy & Security → Microphone
- Select correct device in Settings window
- Test microphone in Windows Voice Recorder first
- Verify microphone is not muted

### Text doesn't appear?
- Text is automatically copied to clipboard as fallback
- Try pressing `Ctrl + V` to paste manually
- Check if target app allows text input
- Some admin/elevated apps may block text injection (clipboard fallback works)

### Transcription returns empty text?
- Microphone may not be capturing audio (check volume levels)
- Speech may be too quiet or unclear
- Background noise may interfere
- Try speaking more clearly and closer to microphone
- Check logs in `%APPDATA%\SpeechToTextTray\logs\` for details

### Build errors?
```bash
# Clean and rebuild
dotnet clean
dotnet restore
dotnet build
```

If model files are missing:
```
Error: Model directory not found: Models\sherpa-onnx-nemo-parakeet-tdt-0.6b-v3-int8
```
→ Download model files (see Step 1)

## Common Workflows

### Dictation in Word/Notepad
1. Click in document where you want text
2. Press hotkey to start recording
3. Speak naturally (25 European languages supported)
4. Press hotkey to stop
5. Text appears automatically (1-3 seconds)

### Filling Forms
1. Click in form field
2. Press hotkey
3. Say the information (e.g., "123 Main Street")
4. Press hotkey
5. Text fills in field instantly

### Taking Notes
1. Open notes app (OneNote, Notion, etc.)
2. Use hotkey to quickly capture thoughts
3. No need to wait - start typing again immediately
4. Transcription appears automatically

### Email Composition
1. Click in email body
2. Press hotkey, speak your message
3. Press hotkey to stop
4. Edit transcribed text as needed
5. Send email

## Performance Tips

- **Keep recordings under 2-3 minutes** for best user experience
- **Speak clearly** with minimal background noise for better accuracy
- **Use a quality microphone** (USB microphones work great)
- **CPU-only transcription** - no GPU needed for local provider (~20x realtime speed)
- **First transcription** may take 1-2 seconds extra (model initialization)
- **Subsequent transcriptions** are fast (~1-3 seconds for 30-60s audio)
- **Local provider** uses ~500MB RAM with model loaded
- **Close to microphone** improves recognition quality

## Optional: SpeechMike Integration

If you have a Philips SpeechMike professional dictation device:

1. Connect your SpeechMike and install drivers (usually automatic)
2. Open Settings → Check "Enable SpeechMike integration"
3. Click **Save**
4. Press the **Record button** on your SpeechMike to start/stop recording
5. LED indicator on device syncs with recording state (red when recording)
6. Keyboard hotkey continues to work alongside hardware button

**Benefits**: Dedicated hardware button for hands-free dictation, LED feedback, professional dictation workflow.

## Understanding Provider Choices

| Feature | Local (Default) | Azure Speech | Azure OpenAI Whisper |
|---------|----------------|--------------|---------------------|
| **Cost** | Free | ~$1/hour | ~$0.006/min |
| **Internet** | Not required | Required | Required |
| **Setup** | None | Subscription | Azure OpenAI access |
| **Languages** | 25 European | 100+ | 50+ |
| **Speed** | 1-3 seconds | 2-5 seconds | 3-8 seconds |
| **Privacy** | 100% local | Cloud | Cloud |

**Recommendation**: Start with Local provider (default). It's free, fast, private, and works offline. Try cloud providers later if you need additional languages or quality improvements.

## Next Steps

- Customize your hotkey in Settings (right-click tray icon)
- Try different applications to see automatic text injection work
- Review logs if issues occur: `%APPDATA%\SpeechToTextTray\logs\`
- Read full documentation: `wpf-client\README.md`
- Explore cloud providers for additional language support

## Need Help?

1. **Check logs**: `%APPDATA%\SpeechToTextTray\logs\app_YYYYMMDD.log`
2. **Review full README**: `wpf-client\README.md`
3. **Common issues**:
   - Model not found → Download ONNX files (Step 1)
   - Text injection fails → Check clipboard, may need to paste manually
   - Empty transcription → Check microphone volume and permissions
4. **Test transcription**: Try speaking a clear sentence in a quiet environment

## Pro Tips

💡 **Set a memorable hotkey**: Something easy to reach like `Ctrl + Alt + Space`

💡 **Works everywhere**: Browser, Office, Notepad, Slack, Discord, code editors, etc.

💡 **Clipboard fallback**: If text injection fails, it's automatically copied to clipboard

💡 **Multiple languages**: Local provider auto-detects 25 European languages

💡 **Privacy first**: Local transcription means your audio never leaves your computer

💡 **Keep it running**: The app uses minimal resources when idle (<50MB RAM)

💡 **Test before important use**: Try a test recording to verify everything works

💡 **Quiet environment**: Background noise reduces transcription accuracy

---

**Congratulations!** You're now ready to use speech-to-text anywhere in Windows with complete privacy and no internet required! 🎤✨

**No backend needed. No API keys needed. Just works!**
