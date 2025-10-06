"""
Audio processing utilities for converting and validating audio files
"""
import os
import librosa
import soundfile as sf
import tempfile
from pathlib import Path
from config import SAMPLE_RATE, MAX_AUDIO_DURATION


def convert_to_wav(input_path: str, output_path: str = None) -> str:
    """
    Convert audio file to 16kHz mono WAV format required by the model

    Args:
        input_path: Path to input audio file
        output_path: Path to output WAV file (optional, creates temp file if not provided)

    Returns:
        Path to converted WAV file
    """
    try:
        # Load audio file and convert to target sample rate
        audio, sr = librosa.load(input_path, sr=SAMPLE_RATE, mono=True)

        # Create output path if not provided
        if output_path is None:
            output_path = input_path.rsplit('.', 1)[0] + '_converted.wav'

        # Save as WAV file
        sf.write(output_path, audio, SAMPLE_RATE)

        return output_path

    except Exception as e:
        raise ValueError(f"Failed to convert audio file: {str(e)}")


def validate_audio(file_path: str) -> dict:
    """
    Validate audio file and return metadata

    Args:
        file_path: Path to audio file

    Returns:
        Dictionary with audio metadata (duration, sample_rate, channels)

    Raises:
        ValueError: If audio is invalid or too long
    """
    try:
        # Load audio to check properties
        audio, sr = librosa.load(file_path, sr=None, mono=False)

        # Calculate duration
        duration = librosa.get_duration(y=audio, sr=sr)

        # Check if duration exceeds maximum
        if duration > MAX_AUDIO_DURATION:
            raise ValueError(
                f"Audio duration ({duration:.1f}s) exceeds maximum allowed "
                f"duration ({MAX_AUDIO_DURATION}s)"
            )

        # Get number of channels
        channels = 1 if audio.ndim == 1 else audio.shape[0]

        return {
            "duration": duration,
            "sample_rate": sr,
            "channels": channels,
            "valid": True
        }

    except Exception as e:
        raise ValueError(f"Audio validation failed: {str(e)}")


def get_audio_info(file_path: str) -> str:
    """
    Get human-readable audio file information

    Args:
        file_path: Path to audio file

    Returns:
        Formatted string with audio information
    """
    info = validate_audio(file_path)
    return (
        f"Duration: {info['duration']:.2f}s, "
        f"Sample Rate: {info['sample_rate']}Hz, "
        f"Channels: {info['channels']}"
    )
