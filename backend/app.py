"""
FastAPI application for Speech-to-Text service using NVIDIA Parakeet model
"""
import os
import logging
import uuid
from pathlib import Path
from typing import Dict

from fastapi import FastAPI, UploadFile, File, HTTPException
from fastapi.middleware.cors import CORSMiddleware
from fastapi.responses import JSONResponse
import uvicorn

from config import (
    HOST, PORT, ALLOWED_ORIGINS, UPLOAD_DIR, TEMP_DIR, ALLOWED_FORMATS
)
from audio_utils import convert_to_wav, validate_audio
from model_service import get_model_service

# Configure logging
logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

# Initialize FastAPI app
app = FastAPI(
    title="Speech-to-Text API",
    description="NVIDIA Parakeet-based speech recognition service",
    version="1.0.0"
)

# Add CORS middleware
app.add_middleware(
    CORSMiddleware,
    allow_origins=ALLOWED_ORIGINS,
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

# Global model service instance
model_service = None


@app.on_event("startup")
async def startup_event():
    """Initialize model on startup"""
    global model_service
    try:
        logger.info("Starting up application...")
        logger.info("Loading ASR model (this may take a few minutes)...")
        model_service = get_model_service()
        logger.info("Application ready!")
    except Exception as e:
        logger.error(f"Failed to initialize model: {str(e)}")
        raise


@app.get("/")
async def root():
    """Root endpoint"""
    return {
        "message": "Speech-to-Text API with NVIDIA Parakeet",
        "status": "online",
        "endpoints": {
            "transcribe": "/transcribe (POST)",
            "health": "/health (GET)",
            "model_info": "/model-info (GET)"
        }
    }


@app.get("/health")
async def health_check():
    """Health check endpoint"""
    if model_service is None:
        raise HTTPException(status_code=503, detail="Model not initialized")

    return {
        "status": "healthy",
        "model": model_service.get_model_info()
    }


@app.get("/model-info")
async def model_info():
    """Get model information"""
    if model_service is None:
        raise HTTPException(status_code=503, detail="Model not initialized")

    return model_service.get_model_info()


@app.post("/transcribe")
async def transcribe_audio(audio: UploadFile = File(...)):
    """
    Transcribe audio file to text

    Args:
        audio: Audio file (WAV, WebM, MP3, FLAC, M4A, OGG)

    Returns:
        JSON response with transcribed text and metadata
    """
    if model_service is None:
        raise HTTPException(status_code=503, detail="Model not initialized")

    # Generate unique filename
    file_id = str(uuid.uuid4())
    file_extension = Path(audio.filename).suffix.lower()

    # Validate file format
    if file_extension not in ALLOWED_FORMATS:
        raise HTTPException(
            status_code=400,
            detail=f"Unsupported file format. Allowed formats: {', '.join(ALLOWED_FORMATS)}"
        )

    # Save uploaded file
    input_path = os.path.join(TEMP_DIR, f"{file_id}_input{file_extension}")
    wav_path = os.path.join(TEMP_DIR, f"{file_id}_converted.wav")

    try:
        # Save uploaded file
        logger.info(f"Receiving audio file: {audio.filename}")
        with open(input_path, "wb") as f:
            content = await audio.read()
            f.write(content)

        # Convert to WAV format
        logger.info("Converting audio to 16kHz WAV format...")
        try:
            convert_to_wav(input_path, wav_path)
        except ValueError as e:
            logger.error(f"Audio conversion error: {str(e)}")
            raise HTTPException(status_code=400, detail=f"Audio conversion failed: {str(e)}")

        # Validate the converted audio
        try:
            audio_info = validate_audio(wav_path)
            logger.info(f"Audio validated: {audio_info['duration']:.2f}s")
        except ValueError as e:
            logger.error(f"Audio validation error: {str(e)}")
            raise HTTPException(status_code=400, detail=str(e))

        # Transcribe
        logger.info("Transcribing audio...")
        result = model_service.transcribe(wav_path)

        # Prepare response
        response = {
            "success": True,
            "text": result["text"],
            "language": result["language"],
            "audio_duration": audio_info["duration"],
            "original_filename": audio.filename
        }

        logger.info(f"Transcription completed: {len(result['text'])} characters")

        return JSONResponse(content=response)

    except HTTPException:
        raise
    except Exception as e:
        logger.error(f"Transcription error: {str(e)}")
        raise HTTPException(
            status_code=500,
            detail=f"Transcription failed: {str(e)}"
        )

    finally:
        # Cleanup temporary files
        try:
            if os.path.exists(input_path):
                os.remove(input_path)
            if os.path.exists(wav_path):
                os.remove(wav_path)
        except Exception as e:
            logger.warning(f"Failed to cleanup temp files: {str(e)}")


@app.delete("/cleanup")
async def cleanup_temp_files():
    """
    Cleanup all temporary files (admin endpoint)

    Returns:
        Number of files deleted
    """
    try:
        files = os.listdir(TEMP_DIR)
        for file in files:
            file_path = os.path.join(TEMP_DIR, file)
            if os.path.isfile(file_path):
                os.remove(file_path)

        return {
            "success": True,
            "files_deleted": len(files)
        }

    except Exception as e:
        raise HTTPException(
            status_code=500,
            detail=f"Cleanup failed: {str(e)}"
        )


if __name__ == "__main__":
    logger.info(f"Starting server on {HOST}:{PORT}")
    uvicorn.run(
        "app:app",
        host=HOST,
        port=PORT,
        reload=False,
        log_level="info"
    )
