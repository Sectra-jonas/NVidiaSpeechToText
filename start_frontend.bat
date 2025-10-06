@echo off
echo Starting Speech-to-Text Frontend...
echo.

cd frontend

echo Starting web server on http://localhost:3000
echo Press Ctrl+C to stop the server
echo.
echo Open your browser and navigate to: http://localhost:3000
echo.

python -m http.server 3000

pause
