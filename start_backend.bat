@echo off
echo Starting Speech-to-Text Backend Server...
echo.

cd backend

echo Activating virtual environment...
if exist ..\venv\Scripts\activate.bat (
    call ..\venv\Scripts\activate.bat
) else (
    echo WARNING: Virtual environment not found. Please run 'python -m venv venv' first.
    echo Continuing without virtual environment...
)

echo.
echo Starting server on http://localhost:8000
echo Press Ctrl+C to stop the server
echo.

python app.py

pause
