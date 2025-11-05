# Quick Start Guide - Windows Tray Application

Get up and running with the Speech-to-Text Windows tray application in 5 minutes.

## Prerequisites Checklist

- [ ] Windows 11 (or Windows 10)
- [ ] .NET 8.0 SDK installed - [Download](https://dotnet.microsoft.com/download/dotnet/8.0)
- [ ] Visual Studio 2022 (Community Edition is free) - [Download](https://visualstudio.microsoft.com/)
- [ ] Microphone connected and working
- [ ] Backend server requirements met (Python, venv, etc.)

## Step 1: Start the Backend Server

The backend **must** be running before the tray app can work.

```bash
# Navigate to backend directory
cd ..\backend

# Activate Python virtual environment
..\venv\Scripts\activate

# Start the server
python app.py
```

Wait for the message: **"Model loaded successfully!"**

The backend will be available at: `http://localhost:8000`

## Step 2: Add Icon Files (Important!)

Before building, you need to add icon files:

1. Navigate to: `wpf-client\SpeechToTextTray\Resources\Icons\`
2. Add these three icon files:
   - `tray-icon-idle.ico` (microphone icon, gray/blue)
   - `tray-icon-recording.ico` (microphone icon, red)
   - `tray-icon-processing.ico` (microphone icon, yellow/orange)

**Don't have icons?** See `Resources/Icons/README.md` for quick solutions:
- Download free icons from Icons8 or Flaticon
- Use an online converter to create .ico files from PNG images
- Temporarily use any .ico file (app will work but won't have proper visual indicators)

## Step 3: Build and Run

### Option A: Using Visual Studio (Recommended)

1. Double-click `SpeechToTextTray.sln` to open in Visual Studio
2. Wait for NuGet packages to restore (happens automatically)
3. Press **F5** to build and run in debug mode
4. Look for the microphone icon in your system tray

### Option B: Using Command Line

```bash
# Navigate to project directory
cd wpf-client\SpeechToTextTray

# Restore packages and build
dotnet build

# Run the application
dotnet run
```

## Step 4: First Use

1. **Verify Tray Icon**: Look in the system tray (bottom-right corner) for a microphone icon
   - If you don't see it, click the ^ arrow to show hidden icons

2. **Configure Settings** (Optional):
   - Right-click the tray icon
   - Select **Settings**
   - Review default hotkey: `Ctrl + Shift + Space`
   - Select your microphone device
   - Click **Test** button to verify backend connection
   - Click **Save**

3. **Test Recording**:
   - Press your hotkey (`Ctrl + Shift + Space`)
   - Speak: "This is a test recording"
   - Press hotkey again to stop
   - Wait 2-3 seconds for transcription
   - Text should appear where your cursor is!

## Troubleshooting Quick Fixes

### "Backend Not Available" notification?
```bash
# Check if backend is running
curl http://localhost:8000/health

# If not, start it:
cd backend
python app.py
```

### Hotkey not working?
- Try a different key combination in Settings
- Make sure the hotkey isn't used by another program
- Restart the app after changing the hotkey

### No audio recording?
- Check Windows microphone permissions: Settings → Privacy → Microphone
- Select correct device in Settings window
- Test microphone in Windows Voice Recorder first

### Text doesn't appear?
- Text is automatically copied to clipboard as fallback
- Try pressing `Ctrl + V` to paste manually
- Check if target app allows text input
- Some admin apps may block text injection

### Build errors?
```bash
# Clean and rebuild
dotnet clean
dotnet restore
dotnet build
```

## Next Steps

- Customize your hotkey in Settings
- Enable "Start with Windows" to launch automatically
- Review logs if issues occur: `%APPDATA%\SpeechToTextTray\logs\`
- Read full documentation: `wpf-client\README.md`

## Common Workflows

### Dictation in Word/Notepad
1. Click in document where you want text
2. Press hotkey to start recording
3. Speak naturally
4. Press hotkey to stop
5. Text appears automatically

### Filling Forms
1. Click in form field
2. Press hotkey
3. Say the information (e.g., "123 Main Street")
4. Press hotkey
5. Text fills in field

### Taking Notes
1. Open notes app (OneNote, Notion, etc.)
2. Use hotkey to quickly capture thoughts
3. Text appears immediately

## Performance Tips

- Keep recordings under 2-3 minutes for best performance
- Speak clearly with minimal background noise
- Use a good quality microphone
- Ensure backend has enough RAM (2-4GB)
- GPU greatly improves transcription speed

## Need Help?

1. Check logs: `%APPDATA%\SpeechToTextTray\logs\app_YYYYMMDD.log`
2. Review full README: `wpf-client\README.md`
3. Verify backend is healthy: Open `http://localhost:8000` in browser
4. Check backend logs in `backend\` directory

## Pro Tips

💡 **Set a memorable hotkey**: Something easy to reach like `Ctrl + Alt + Space`

💡 **Start with Windows**: Enable in Settings for instant availability

💡 **Test backend connection**: Use the "Test" button in Settings before first use

💡 **Use good equipment**: A decent USB microphone improves transcription quality

💡 **Keep it running**: The app uses minimal resources when idle

---

**Congratulations!** You're now ready to use speech-to-text anywhere in Windows! 🎤✨
