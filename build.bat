@echo off
echo ========================================
echo Building Asterix Reader Project
echo ========================================
echo.

REM Check if Docker is running
docker info >nul 2>&1
if errorlevel 1 (
    echo ERROR: Docker is not running or not installed!
    echo Please start Docker Desktop and try again.
    exit /b 1
)

echo [1/3] Building Backend Docker image...
cd AsterixReader.Backend
docker build -t asterix-reader-backend:latest .
if errorlevel 1 (
    echo ERROR: Backend build failed!
    exit /b 1
)
cd ..
echo Backend build completed successfully!
echo.

echo [2/3] Building Frontend Docker image...
cd asterix-reader-frontend
docker build -t asterix-reader-frontend:latest .
if errorlevel 1 (
    echo ERROR: Frontend build failed!
    exit /b 1
)
cd ..
echo Frontend build completed successfully!
echo.

echo [3/3] Building with Docker Compose...
docker-compose build
if errorlevel 1 (
    echo ERROR: Docker Compose build failed!
    exit /b 1
)
echo.

echo ========================================
echo Build completed successfully!
echo ========================================
echo.
echo To start the containers, run:
echo   docker-compose up -d
echo.
echo To view logs:
echo   docker-compose logs -f
echo.
echo To stop the containers:
echo   docker-compose down
echo.

