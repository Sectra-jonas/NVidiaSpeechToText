"""
Configuration settings for the Speech-to-Text application
"""
import os

# Model configuration
MODEL_NAME = "nvidia/parakeet-tdt-0.6b-v3"

# Audio configuration
SAMPLE_RATE = 16000  # Required by Parakeet model
MAX_AUDIO_DURATION = 24 * 60  # 24 minutes in seconds
ALLOWED_FORMATS = ['.wav', '.webm', '.mp3', '.flac', '.m4a', '.ogg']

# File paths
BASE_DIR = os.path.dirname(os.path.abspath(__file__))
UPLOAD_DIR = os.path.join(BASE_DIR, "uploads")
TEMP_DIR = os.path.join(UPLOAD_DIR, "temp")

# Server configuration
HOST = "0.0.0.0"
PORT = 8000

# CORS settings
ALLOWED_ORIGINS = [
    "http://localhost:3000",
    "http://localhost:8000",
    "http://127.0.0.1:3000",
    "http://127.0.0.1:8000",
]

# Create necessary directories
os.makedirs(UPLOAD_DIR, exist_ok=True)
os.makedirs(TEMP_DIR, exist_ok=True)
