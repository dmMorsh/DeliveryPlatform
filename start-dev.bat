@echo off
REM Delivery Tracking Platform - Local Development Setup for Windows

echo.
echo ===================================================================
echo   Delivery Tracking Platform - Local Development Setup
echo ===================================================================
echo.

REM Check Docker
docker --version >nul 2>&1
if errorlevel 1 (
    echo WARNING: Docker is not installed. Please install Docker Desktop first.
    exit /b 1
)

REM Build Docker images
echo [1/5] Building Docker images...
docker-compose build --no-cache
if errorlevel 1 goto error

REM Start infrastructure
echo.
echo [2/5] Starting infrastructure ^(PostgreSQL, Redis, Kafka^)...
docker-compose up -d postgres redis zookeeper kafka
timeout /t 5 /nobreak

REM Build .NET projects
echo.
echo [3/5] Building .NET projects...
dotnet build
if errorlevel 1 goto error

REM Run database migrations
echo.
echo [4/5] Running database migrations...
dotnet ef database update --project src/OrderService
dotnet ef database update --project src/CourierService
if errorlevel 1 goto error

REM Start microservices
echo.
echo [5/5] Starting microservices...
docker-compose up -d gateway-api order-service courier-service
timeout /t 3 /nobreak

REM Health checks
echo.
echo Checking service health...
for %%s in ("gateway-api:5000" "order-service:5001" "courier-service:5002") do (
    for /f "tokens=1,2 delims=:" %%a in (%%s) do (
        echo Checking %%a on port %%b...
    )
)

echo.
echo ===================================================================
echo All services are running!
echo ===================================================================
echo.
echo Service URLs:
echo   Gateway API:        http://localhost:5000
echo   Order Service:      http://localhost:5001
echo   Courier Service:    http://localhost:5002
echo   PostgreSQL:         localhost:5432
echo   Redis:              localhost:6379
echo   Kafka:              localhost:9092
echo   Zookeeper:          localhost:2181
echo.
echo Health Check:
echo   curl http://localhost:5000/health
echo   curl http://localhost:5000/api/services-health
echo.
echo View logs:
echo   docker-compose logs -f gateway-api
echo   docker-compose logs -f order-service
echo   docker-compose logs -f courier-service
echo.
echo Stop services:
echo   docker-compose down
echo.
exit /b 0

:error
echo.
echo ERROR: Setup failed!
exit /b 1
