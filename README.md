# Speech-to-Text Application

A web-based speech-to-text application powered by **NVIDIA Parakeet-TDT-0.6b-v3** model. Record audio directly in your browser and get accurate transcriptions in 25 European languages with automatic language detection.

## Features

- 🎤 **Browser-based Recording**: Record audio directly from your microphone
- 🌍 **Multilingual Support**: Supports 25 European languages with automatic detection
- ⚡ **Fast Transcription**: Powered by NVIDIA's state-of-the-art Parakeet model
- 📝 **Text Export**: Copy to clipboard or download as text file
- 🎨 **Modern UI**: Clean, responsive interface with real-time feedback
- ⏱️ **Long Audio Support**: Handles recordings up to 24 minutes

## Architecture

```
┌─────────────────┐         HTTP POST          ┌──────────────────┐
│   Frontend      │  ─────────────────────────> │   Backend        │
│   (Browser)     │                             │   (FastAPI)      │
│                 │  <─────────────────────────  │                  │
│ - HTML/CSS/JS   │         JSON Response       │ - Python         │
│ - MediaRecorder │                             │ - NeMo           │
│ - Fetch API     │                             │ - Parakeet Model │
└─────────────────┘                             └──────────────────┘
```

## Prerequisites

- Python 3.8 or higher
- Modern web browser (Chrome, Firefox, Edge, Safari)
- Microphone access
- (Optional) NVIDIA GPU for faster transcription

## Installation

### 1. Clone or Download

Navigate to the project directory:
```bash
cd NVidiaSpeech
```

### 2. Set up Python Virtual Environment

```bash
# Create virtual environment
python -m venv venv

# Activate virtual environment
# On Windows:
venv\Scripts\activate

# On macOS/Linux:
source venv/bin/activate
```

### 3. Install Backend Dependencies

```bash
cd backend
pip install -r requirements.txt
```

**Note**: First installation will take several minutes as it downloads the NVIDIA Parakeet model (~600MB) and dependencies.

### 4. Install PyTorch (if needed)

If you have an NVIDIA GPU, install the CUDA version of PyTorch for better performance:

```bash
# For CUDA 11.8
pip install torch torchaudio --index-url https://download.pytorch.org/whl/cu118

# For CUDA 12.1
pip install torch torchaudio --index-url https://download.pytorch.org/whl/cu121

# For CPU only
pip install torch torchaudio
```

## Usage

### 1. Start the Backend Server

```bash
cd backend
python app.py
```

The server will start on `http://localhost:8000`. First startup will take a few minutes to load the model.

You should see:
```
Loading NVIDIA Parakeet model...
Model loaded successfully!
Starting server on 0.0.0.0:8000
```

### 2. Open the Frontend

Open a new terminal and serve the frontend:

```bash
cd frontend

# Using Python's built-in server
python -m http.server 3000

# Or use any static file server
# npx http-server -p 3000
# npx live-server --port=3000
```

### 3. Access the Application

Open your browser and navigate to:
```
http://localhost:3000
```

### 4. Use the Application

1. **Click the microphone button** to start recording
2. **Speak clearly** into your microphone
3. **Click again to stop** recording
4. Wait for transcription (usually 1-3 seconds)
5. **View the transcribed text** in the output area
6. **Copy or download** the transcription if needed

## API Endpoints

### `POST /transcribe`

Transcribe audio file to text.

**Request:**
- Method: `POST`
- Content-Type: `multipart/form-data`
- Body: Audio file (WAV, WebM, MP3, FLAC, M4A, OGG)

**Response:**
```json
{
  "success": true,
  "text": "Transcribed text content",
  "language": "auto-detected",
  "audio_duration": 5.23,
  "original_filename": "recording.webm"
}
```

### `GET /health`

Check API health and model status.

**Response:**
```json
{
  "status": "healthy",
  "model": {
    "model_name": "nvidia/parakeet-tdt-0.6b-v3",
    "device": "cuda",
    "status": "loaded"
  }
}
```

### `GET /model-info`

Get detailed model information.

**Response:**
```json
{
  "model_name": "nvidia/parakeet-tdt-0.6b-v3",
  "device": "cuda",
  "status": "loaded"
}
```

## Project Structure

```
NVidiaSpeech/
│
├── backend/
│   ├── app.py                 # FastAPI application
│   ├── model_service.py       # NeMo model wrapper
│   ├── audio_utils.py         # Audio processing utilities
│   ├── config.py              # Configuration settings
│   ├── requirements.txt       # Python dependencies
│   └── uploads/               # Temporary file storage
│       └── temp/
│
├── frontend/
│   ├── index.html             # Main UI
│   ├── styles.css             # Styling
│   ├── app.js                 # Recording & API logic
│   └── assets/
│
└── README.md                  # This file
```

## Configuration

Edit `backend/config.py` to customize:

- **Model name**: Change to different NeMo model
- **Sample rate**: Audio sample rate (default: 16000Hz)
- **Max duration**: Maximum audio length (default: 24 minutes)
- **Upload directory**: Temporary file storage location
- **CORS origins**: Allowed frontend origins
- **Server host/port**: Backend server address

## Troubleshooting

### Backend won't start

- **Check Python version**: Ensure Python 3.8+
- **Install dependencies**: Run `pip install -r requirements.txt`
- **Check port availability**: Ensure port 8000 is not in use
- **GPU issues**: If CUDA errors occur, try CPU mode by installing CPU-only PyTorch

### Frontend can't connect

- **Check backend is running**: Visit `http://localhost:8000` and check for JSON response
- **CORS issues**: Ensure frontend URL is in `ALLOWED_ORIGINS` in `config.py`
- **Port conflicts**: Change frontend port if 3000 is in use

### Microphone not working

- **Browser permissions**: Allow microphone access when prompted
- **HTTPS requirement**: Some browsers require HTTPS for microphone access (use `localhost` for development)
- **Device selection**: Ensure correct microphone is selected in system settings

### Transcription errors

- **Audio too long**: Limit recordings to under 24 minutes
- **Low audio quality**: Ensure clear audio with minimal background noise
- **Model loading**: Wait for "Model loaded successfully!" message before transcribing

### Out of memory

- **Reduce audio length**: Try shorter recordings
- **Close other applications**: Free up system memory
- **Use GPU**: If available, GPU processing uses less system RAM

## Performance

- **Model Loading**: 5-10 seconds (first time: 2-5 minutes for download)
- **Transcription Speed**: ~0.1-0.3x real-time (10s audio = 1-3s processing)
- **Memory Usage**: ~2-4GB RAM (model loaded in memory)
- **GPU Acceleration**: 3-5x faster than CPU

## Supported Languages

The NVIDIA Parakeet model supports 25 European languages:

English, German, French, Spanish, Italian, Portuguese, Dutch, Polish, Russian, Czech, Slovak, Romanian, Hungarian, Bulgarian, Croatian, Danish, Finnish, Swedish, Norwegian, Greek, Turkish, Ukrainian, Estonian, Lithuanian, Latvian

## License

This project uses the NVIDIA Parakeet-TDT-0.6b-v3 model, which is licensed under CC-BY-4.0.

## Acknowledgments

- **NVIDIA NeMo**: For the excellent ASR toolkit
- **Parakeet Model**: NVIDIA's state-of-the-art multilingual ASR model
- **FastAPI**: For the modern Python web framework

## Support

For issues, questions, or contributions, please refer to:
- [NVIDIA NeMo Documentation](https://docs.nvidia.com/deeplearning/nemo/user-guide/docs/en/stable/)
- [Parakeet Model Card](https://huggingface.co/nvidia/parakeet-tdt-0.6b-v3)

---

**Built with ❤️ using NVIDIA Parakeet**
