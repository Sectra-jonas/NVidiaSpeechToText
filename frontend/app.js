/**
 * Speech-to-Text Application
 * Frontend JavaScript for audio recording and transcription
 */

// Configuration
const API_BASE_URL = 'http://localhost:8000';

// State management
let mediaRecorder = null;
let audioChunks = [];
let isRecording = false;
let audioStream = null;

// DOM elements
const recordButton = document.getElementById('recordButton');
const statusText = document.getElementById('statusText');
const recordingIndicator = document.getElementById('recordingIndicator');
const processingIndicator = document.getElementById('processingIndicator');
const transcriptionOutput = document.getElementById('transcriptionOutput');
const transcriptionMeta = document.getElementById('transcriptionMeta');
const languageInfo = document.getElementById('languageInfo');
const durationInfo = document.getElementById('durationInfo');
const copyButton = document.getElementById('copyButton');
const downloadButton = document.getElementById('downloadButton');
const errorMessage = document.getElementById('errorMessage');
const errorText = document.getElementById('errorText');

// Current transcription data
let currentTranscription = {
    text: '',
    language: '',
    duration: 0
};

/**
 * Initialize the application
 */
function init() {
    // Check browser support
    if (!navigator.mediaDevices || !navigator.mediaDevices.getUserMedia) {
        showError('Your browser does not support audio recording. Please use a modern browser like Chrome, Firefox, or Edge.');
        recordButton.disabled = true;
        return;
    }

    // Attach event listeners
    recordButton.addEventListener('click', toggleRecording);
    copyButton.addEventListener('click', copyToClipboard);
    downloadButton.addEventListener('click', downloadTranscription);

    console.log('Speech-to-Text app initialized');
}

/**
 * Toggle recording on/off
 */
async function toggleRecording() {
    if (!isRecording) {
        await startRecording();
    } else {
        await stopRecording();
    }
}

/**
 * Start audio recording
 */
async function startRecording() {
    try {
        hideError();

        // Request microphone access
        audioStream = await navigator.mediaDevices.getUserMedia({ audio: true });

        // Create MediaRecorder instance
        mediaRecorder = new MediaRecorder(audioStream);

        // Reset audio chunks
        audioChunks = [];

        // Handle data available event
        mediaRecorder.ondataavailable = (event) => {
            if (event.data.size > 0) {
                audioChunks.push(event.data);
            }
        };

        // Handle stop event
        mediaRecorder.onstop = async () => {
            await processRecording();
        };

        // Start recording
        mediaRecorder.start();
        isRecording = true;

        // Update UI
        updateUIState('recording');

        console.log('Recording started');

    } catch (error) {
        console.error('Error starting recording:', error);

        if (error.name === 'NotAllowedError') {
            showError('Microphone access denied. Please allow microphone access in your browser settings.');
        } else if (error.name === 'NotFoundError') {
            showError('No microphone found. Please connect a microphone and try again.');
        } else {
            showError(`Failed to start recording: ${error.message}`);
        }

        isRecording = false;
        updateUIState('idle');
    }
}

/**
 * Stop audio recording
 */
async function stopRecording() {
    if (mediaRecorder && mediaRecorder.state !== 'inactive') {
        mediaRecorder.stop();
        isRecording = false;

        // Stop all audio tracks
        if (audioStream) {
            audioStream.getTracks().forEach(track => track.stop());
        }

        console.log('Recording stopped');
    }
}

/**
 * Process recorded audio and send to backend
 */
async function processRecording() {
    try {
        // Update UI to show processing state
        updateUIState('processing');

        // Create blob from audio chunks
        const audioBlob = new Blob(audioChunks, { type: 'audio/webm' });

        console.log(`Audio blob created: ${audioBlob.size} bytes`);

        // Send to backend for transcription
        await sendToBackend(audioBlob);

    } catch (error) {
        console.error('Error processing recording:', error);
        showError(`Failed to process recording: ${error.message}`);
        updateUIState('idle');
    }
}

/**
 * Send audio to backend API for transcription
 */
async function sendToBackend(audioBlob) {
    try {
        // Create FormData
        const formData = new FormData();
        formData.append('audio', audioBlob, 'recording.webm');

        console.log('Sending audio to backend...');

        // Send POST request
        const response = await fetch(`${API_BASE_URL}/transcribe`, {
            method: 'POST',
            body: formData
        });

        if (!response.ok) {
            const errorData = await response.json();
            throw new Error(errorData.detail || `Server error: ${response.status}`);
        }

        // Parse response
        const result = await response.json();

        console.log('Transcription received:', result);

        // Display transcription
        displayTranscription(result);

        // Update UI to idle state
        updateUIState('idle');

    } catch (error) {
        console.error('Error sending to backend:', error);

        if (error.message.includes('Failed to fetch')) {
            showError('Cannot connect to server. Please make sure the backend is running on http://localhost:8000');
        } else {
            showError(`Transcription failed: ${error.message}`);
        }

        updateUIState('idle');
    }
}

/**
 * Display transcription result
 */
function displayTranscription(result) {
    // Store current transcription
    currentTranscription = {
        text: result.text || 'No speech detected',
        language: result.language || 'Unknown',
        duration: result.audio_duration || 0
    };

    // Update transcription output
    transcriptionOutput.innerHTML = `<p>${currentTranscription.text}</p>`;
    transcriptionOutput.classList.add('has-content');

    // Update metadata
    languageInfo.textContent = `Language: ${currentTranscription.language}`;
    durationInfo.textContent = `Duration: ${currentTranscription.duration.toFixed(2)}s`;
    transcriptionMeta.classList.remove('hidden');

    // Show action buttons
    copyButton.classList.remove('hidden');
    downloadButton.classList.remove('hidden');

    console.log('Transcription displayed');
}

/**
 * Update UI state
 */
function updateUIState(state) {
    // Remove all state classes
    recordButton.classList.remove('recording', 'processing');
    recordingIndicator.classList.add('hidden');
    processingIndicator.classList.add('hidden');

    switch (state) {
        case 'idle':
            statusText.textContent = 'Click to Record';
            recordButton.disabled = false;
            break;

        case 'recording':
            statusText.textContent = 'Recording...';
            recordButton.classList.add('recording');
            recordingIndicator.classList.remove('hidden');
            recordButton.disabled = false;
            break;

        case 'processing':
            statusText.textContent = 'Transcribing...';
            recordButton.classList.add('processing');
            processingIndicator.classList.remove('hidden');
            recordButton.disabled = true;
            break;
    }
}

/**
 * Copy transcription to clipboard
 */
async function copyToClipboard() {
    try {
        await navigator.clipboard.writeText(currentTranscription.text);

        // Show feedback
        const originalText = copyButton.innerHTML;
        copyButton.innerHTML = `
            <svg width="20" height="20" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
                <path d="M20 6L9 17l-5-5" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
            </svg>
            Copied!
        `;

        setTimeout(() => {
            copyButton.innerHTML = originalText;
        }, 2000);

        console.log('Copied to clipboard');

    } catch (error) {
        console.error('Error copying to clipboard:', error);
        showError('Failed to copy to clipboard');
    }
}

/**
 * Download transcription as text file
 */
function downloadTranscription() {
    try {
        // Create text content
        const content = `Speech-to-Text Transcription\n\n` +
                       `Language: ${currentTranscription.language}\n` +
                       `Duration: ${currentTranscription.duration.toFixed(2)}s\n` +
                       `Date: ${new Date().toLocaleString()}\n\n` +
                       `---\n\n` +
                       `${currentTranscription.text}`;

        // Create blob and download
        const blob = new Blob([content], { type: 'text/plain' });
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `transcription_${Date.now()}.txt`;
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
        URL.revokeObjectURL(url);

        console.log('Transcription downloaded');

    } catch (error) {
        console.error('Error downloading transcription:', error);
        showError('Failed to download transcription');
    }
}

/**
 * Show error message
 */
function showError(message) {
    errorText.textContent = message;
    errorMessage.classList.remove('hidden');

    // Auto-hide after 10 seconds
    setTimeout(() => {
        hideError();
    }, 10000);
}

/**
 * Hide error message
 */
function hideError() {
    errorMessage.classList.add('hidden');
}

// Initialize app when DOM is ready
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', init);
} else {
    init();
}
