"""
Model service for loading and running NVIDIA Parakeet ASR model
"""
import logging
from typing import Dict, List, Optional
import nemo.collections.asr as nemo_asr
from config import MODEL_NAME

# Configure logging
logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)


class ModelService:
    """
    Singleton service for managing the NVIDIA Parakeet ASR model
    """

    def __init__(self):
        """Initialize and load the ASR model"""
        self.model = None
        self.model_name = MODEL_NAME
        self._load_model()

    def _load_model(self):
        """Load the NeMo ASR model from HuggingFace"""
        try:
            logger.info(f"Loading NVIDIA Parakeet model: {self.model_name}")
            logger.info("This may take a few minutes on first run (downloading model)...")

            self.model = nemo_asr.models.ASRModel.from_pretrained(
                self.model_name
            )

            logger.info("Model loaded successfully!")

            # Check if CUDA is available
            import torch
            if torch.cuda.is_available():
                logger.info(f"Using GPU: {torch.cuda.get_device_name(0)}")
                self.model = self.model.cuda()
            else:
                logger.info("Using CPU (consider using GPU for better performance)")

        except Exception as e:
            logger.error(f"Failed to load model: {str(e)}")
            raise RuntimeError(f"Model initialization failed: {str(e)}")

    def transcribe(self, audio_path: str) -> Dict[str, any]:
        """
        Transcribe audio file to text

        Args:
            audio_path: Path to audio file (WAV format, 16kHz recommended)

        Returns:
            Dictionary containing transcription results:
            - text: Transcribed text
            - language: Detected language (if available)
            - confidence: Confidence score (if available)

        Raises:
            RuntimeError: If transcription fails
        """
        if self.model is None:
            raise RuntimeError("Model not loaded")

        try:
            logger.info(f"Transcribing audio file: {audio_path}")

            # Perform transcription
            output = self.model.transcribe([audio_path])

            # Extract transcription text
            text = output[0] if isinstance(output[0], str) else output[0].text

            # Try to extract additional metadata if available
            result = {
                "text": text,
                "language": "auto-detected",  # Parakeet auto-detects language
                "confidence": None
            }

            # Check if output has additional attributes
            if hasattr(output[0], 'language'):
                result["language"] = output[0].language

            logger.info(f"Transcription successful: '{text[:50]}...'")

            return result

        except Exception as e:
            logger.error(f"Transcription failed: {str(e)}")
            raise RuntimeError(f"Transcription failed: {str(e)}")

    def transcribe_batch(self, audio_paths: List[str]) -> List[Dict[str, any]]:
        """
        Transcribe multiple audio files in batch

        Args:
            audio_paths: List of paths to audio files

        Returns:
            List of transcription result dictionaries
        """
        if self.model is None:
            raise RuntimeError("Model not loaded")

        try:
            logger.info(f"Batch transcribing {len(audio_paths)} files")

            # Perform batch transcription
            outputs = self.model.transcribe(audio_paths)

            results = []
            for output in outputs:
                text = output if isinstance(output, str) else output.text
                results.append({
                    "text": text,
                    "language": getattr(output, 'language', 'auto-detected'),
                    "confidence": None
                })

            logger.info(f"Batch transcription completed: {len(results)} files")

            return results

        except Exception as e:
            logger.error(f"Batch transcription failed: {str(e)}")
            raise RuntimeError(f"Batch transcription failed: {str(e)}")

    def get_model_info(self) -> Dict[str, str]:
        """
        Get information about the loaded model

        Returns:
            Dictionary with model information
        """
        import torch

        return {
            "model_name": self.model_name,
            "device": "cuda" if torch.cuda.is_available() else "cpu",
            "status": "loaded" if self.model is not None else "not loaded"
        }


# Global model instance (singleton pattern)
_model_service_instance: Optional[ModelService] = None


def get_model_service() -> ModelService:
    """
    Get or create the global model service instance

    Returns:
        ModelService instance
    """
    global _model_service_instance

    if _model_service_instance is None:
        _model_service_instance = ModelService()

    return _model_service_instance
