@echo off
echo.
echo ?? SaffaApi Metrics Generator for .NET Aspire
echo ============================================
echo.
echo ?? This script generates HTTP requests to test your Grafana dashboard
echo ?? Run this from the AppHost/tools directory while your Aspire application is running
echo.
echo Press Ctrl+C to stop at any time
echo.

REM Check if PowerShell is available
powershell -Command "Write-Host 'PowerShell is available'" >nul 2>&1
if %errorlevel% neq 0 (
    echo ? PowerShell is not available. Please install PowerShell to run this script.
    pause
    exit /b 1
)

REM Check if we're in the correct tools directory within AppHost
if not exist "..\AppHost.cs" (
    echo ? This script should be run from the AppHost\tools directory
    echo    Navigate to the AppHost\tools folder and run this script again
    echo    Expected: AppHost.cs should be in the parent directory
    pause
    exit /b 1
)

echo ? Prerequisites check passed
echo.
echo ?? Choose a load pattern to test different metrics scenarios:
echo.
echo 1. ?? Steady load (5 RPS for 60 seconds) - Basic metrics testing
echo 2. ? Burst load (alternating high/low traffic) - Load spike testing  
echo 3. ?? Mixed load (random patterns with errors) - Comprehensive testing
echo 4. ?? Ramp-up load (increasing intensity) - Gradual load testing
echo 5. ??  Quick test (steady load for 30 seconds) - Fast verification
echo.

set /p choice="Enter your choice (1-5): "

echo.
echo ?? Starting metrics generation...

if "%choice%"=="1" (
    echo ?? Running steady load pattern for comprehensive baseline metrics...
    powershell -ExecutionPolicy Bypass -File "generate-metrics-advanced.ps1" -Pattern "steady" -DurationSeconds 60
) else if "%choice%"=="2" (
    echo ? Running burst load pattern to test performance under load spikes...
    powershell -ExecutionPolicy Bypass -File "generate-metrics-advanced.ps1" -Pattern "burst" -DurationSeconds 60
) else if "%choice%"=="3" (
    echo ?? Running mixed load pattern with error scenarios for full testing...
    powershell -ExecutionPolicy Bypass -File "generate-metrics-advanced.ps1" -Pattern "mixed" -DurationSeconds 60 -IncludeErrors
) else if "%choice%"=="4" (
    echo ?? Running ramp-up load pattern to test scaling behavior...
    powershell -ExecutionPolicy Bypass -File "generate-metrics-advanced.ps1" -Pattern "ramp" -DurationSeconds 60
) else if "%choice%"=="5" (
    echo ??  Running quick test for rapid dashboard verification...
    powershell -ExecutionPolicy Bypass -File "generate-metrics-advanced.ps1" -Pattern "steady" -DurationSeconds 30
) else (
    echo ??  Invalid choice. Running default steady load pattern...
    powershell -ExecutionPolicy Bypass -File "generate-metrics-advanced.ps1" -Pattern "steady" -DurationSeconds 60
)

echo.
echo ? Metrics generation complete!
echo.
echo ?? Next Steps:
echo    1. Open Grafana dashboard: http://localhost:3000
echo    2. Login with: admin / admin  
echo    3. Look for: "?? SaffaApi - Metrics Dashboard"
echo    4. Verify P95 response times are realistic (1-50ms)
echo    5. Check that metrics are no longer showing NaN values
echo.
echo ?? Expected Results:
echo    - Request rate graphs should show data
echo    - P95 response times should be 1-50ms (not 4.75s)
echo    - Status code distribution should show 200s
echo    - Error rates should be low or zero
echo.
pause