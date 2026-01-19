# PowerShell script to generate API metrics for testing Grafana dashboard
# This script will make HTTP requests to the SaffaApi endpoints for approximately 1 minute

param(
    [string]$BaseUrl = "http://localhost:5286",
    [int]$DurationSeconds = 60,
    [int]$RequestsPerSecond = 5
)

Write-Host "?? Starting API metrics generation..." -ForegroundColor Green
Write-Host "   Base URL: $BaseUrl" -ForegroundColor Cyan
Write-Host "   Duration: $DurationSeconds seconds" -ForegroundColor Cyan
Write-Host "   Rate: $RequestsPerSecond requests/second" -ForegroundColor Cyan
Write-Host ""

# API endpoints to test
$endpoints = @(
    "/",
    "/phrase",
    "/phrase/dutch", 
    "/phrase/braai",
    "/phrase/lekker",
    "/phrase/category/slang",
    "/phrase/category/cultural",
    "/phrase/category/expression",
    "/health"
)

# Calculate intervals
$intervalMs = 1000 / $RequestsPerSecond
$totalRequests = $DurationSeconds * $RequestsPerSecond
$requestCount = 0
$successCount = 0
$errorCount = 0

Write-Host "?? Making $totalRequests requests over $DurationSeconds seconds..." -ForegroundColor Yellow
Write-Host ""

$stopwatch = [System.Diagnostics.Stopwatch]::StartNew()

try {
    while ($stopwatch.ElapsedMilliseconds -lt ($DurationSeconds * 1000)) {
        # Pick a random endpoint
        $endpoint = $endpoints | Get-Random
        $url = "$BaseUrl$endpoint"
        
        $requestCount++
        
        try {
            # Make HTTP request
            $response = Invoke-WebRequest -Uri $url -Method GET -TimeoutSec 5 -ErrorAction Stop
            $successCount++
            
            # Log successful requests periodically
            if ($requestCount % 10 -eq 0) {
                $elapsed = [Math]::Round($stopwatch.Elapsed.TotalSeconds, 1)
                Write-Host "? [$elapsed s] Request $requestCount`: $endpoint (Status: $($response.StatusCode))" -ForegroundColor Green
            }
        }
        catch {
            $errorCount++
            $elapsed = [Math]::Round($stopwatch.Elapsed.TotalSeconds, 1)
            Write-Host "? [$elapsed s] Request $requestCount`: $endpoint (Error: $($_.Exception.Message))" -ForegroundColor Red
        }
        
        # Wait for the next request interval
        Start-Sleep -Milliseconds $intervalMs
    }
}
catch {
    Write-Host "?? Script interrupted: $($_.Exception.Message)" -ForegroundColor Red
}
finally {
    $stopwatch.Stop()
}

Write-Host ""
Write-Host "?? Metrics Generation Complete!" -ForegroundColor Green
Write-Host "   Total Time: $([Math]::Round($stopwatch.Elapsed.TotalSeconds, 1)) seconds" -ForegroundColor Cyan
Write-Host "   Total Requests: $requestCount" -ForegroundColor Cyan
Write-Host "   Successful: $successCount" -ForegroundColor Green
Write-Host "   Errors: $errorCount" -ForegroundColor $(if ($errorCount -gt 0) { "Red" } else { "Green" })
Write-Host "   Success Rate: $([Math]::Round(($successCount / $requestCount) * 100, 1))%" -ForegroundColor Cyan
Write-Host "   Actual RPS: $([Math]::Round($requestCount / $stopwatch.Elapsed.TotalSeconds, 1))" -ForegroundColor Cyan
Write-Host ""
Write-Host "?? Check your Grafana dashboard at http://localhost:3000 to see the metrics!" -ForegroundColor Yellow
Write-Host "?? The dashboard should now show realistic P95 response times (1-50ms instead of 4.75s)" -ForegroundColor Yellow