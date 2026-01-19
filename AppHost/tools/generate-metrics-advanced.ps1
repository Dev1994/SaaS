# Advanced PowerShell script to generate comprehensive API metrics
# This script creates various load patterns to test different scenarios

param(
    [string]$BaseUrl = "http://localhost:5286",
    [int]$DurationSeconds = 60,
    [string]$Pattern = "steady", # steady, burst, mixed, ramp
    [switch]$IncludeErrors
)

Write-Host "?? Advanced API Metrics Generator" -ForegroundColor Magenta
Write-Host "================================" -ForegroundColor Magenta
Write-Host ""

# Define endpoint groups for different testing scenarios
$coreEndpoints = @("/", "/phrase", "/phrase/dutch", "/health")
$phraseEndpoints = @("/phrase/braai", "/phrase/lekker", "/phrase/voetsek", "/phrase/babbelas")
$categoryEndpoints = @("/phrase/category/slang", "/phrase/category/cultural", "/phrase/category/expression")
$errorEndpoints = @("/phrase/nonexistent", "/phrase/category/invalid") # These should return 404

function Write-Progress-Info {
    param($Message, $Color = "White")
    $timestamp = Get-Date -Format "HH:mm:ss"
    Write-Host "[$timestamp] $Message" -ForegroundColor $Color
}

function Invoke-RequestBatch {
    param(
        [array]$Endpoints,
        [int]$Count,
        [int]$DelayMs = 100
    )
    
    $results = @{ Success = 0; Error = 0; Total = 0 }
    
    for ($i = 0; $i -lt $Count; $i++) {
        $endpoint = $Endpoints | Get-Random
        $url = "$BaseUrl$endpoint"
        $results.Total++
        
        try {
            $response = Invoke-WebRequest -Uri $url -Method GET -TimeoutSec 5 -ErrorAction Stop
            $results.Success++
        }
        catch {
            $results.Error++
        }
        
        if ($DelayMs -gt 0) {
            Start-Sleep -Milliseconds $DelayMs
        }
    }
    
    return $results
}

Write-Progress-Info "Starting pattern: $Pattern for $DurationSeconds seconds" "Green"
Write-Progress-Info "Base URL: $BaseUrl" "Cyan"
if ($IncludeErrors) {
    Write-Progress-Info "Including error scenarios (404s)" "Yellow"
}
Write-Host ""

$totalStats = @{ Success = 0; Error = 0; Total = 0 }
$stopwatch = [System.Diagnostics.Stopwatch]::StartNew()

try {
    switch ($Pattern) {
        "steady" {
            Write-Progress-Info "?? Steady load pattern - 5 RPS" "Blue"
            
            while ($stopwatch.ElapsedMilliseconds -lt ($DurationSeconds * 1000)) {
                $endpoints = $coreEndpoints + $phraseEndpoints
                if ($IncludeErrors -and (Get-Random -Minimum 1 -Maximum 10) -eq 1) {
                    $endpoints += $errorEndpoints
                }
                
                $result = Invoke-RequestBatch -Endpoints $endpoints -Count 1 -DelayMs 200
                $totalStats.Success += $result.Success
                $totalStats.Error += $result.Error
                $totalStats.Total += $result.Total
                
                if ($totalStats.Total % 20 -eq 0) {
                    $elapsed = [Math]::Round($stopwatch.Elapsed.TotalSeconds, 1)
                    Write-Progress-Info "Requests: $($totalStats.Total) | Success: $($totalStats.Success) | Errors: $($totalStats.Error)" "Green"
                }
            }
        }
        
        "burst" {
            Write-Progress-Info "?? Burst load pattern - alternating high/low" "Blue"
            
            $burstCycle = 0
            while ($stopwatch.ElapsedMilliseconds -lt ($DurationSeconds * 1000)) {
                $burstCycle++
                
                if ($burstCycle % 2 -eq 1) {
                    # High burst - 20 requests quickly
                    Write-Progress-Info "?? High burst phase" "Red"
                    $endpoints = $coreEndpoints + $phraseEndpoints + $categoryEndpoints
                    $result = Invoke-RequestBatch -Endpoints $endpoints -Count 20 -DelayMs 50
                } else {
                    # Low phase - 5 requests slowly
                    Write-Progress-Info "?? Low phase" "Green"
                    $result = Invoke-RequestBatch -Endpoints $coreEndpoints -Count 5 -DelayMs 400
                }
                
                $totalStats.Success += $result.Success
                $totalStats.Error += $result.Error
                $totalStats.Total += $result.Total
                
                Write-Progress-Info "Burst $burstCycle complete - Total: $($totalStats.Total)" "Cyan"
                Start-Sleep -Milliseconds 1000
            }
        }
        
        "mixed" {
            Write-Progress-Info "?? Mixed load pattern - random intervals" "Blue"
            
            while ($stopwatch.ElapsedMilliseconds -lt ($DurationSeconds * 1000)) {
                # Random burst size between 1-10 requests
                $burstSize = Get-Random -Minimum 1 -Maximum 11
                # Random delay between 50-500ms
                $delay = Get-Random -Minimum 50 -Maximum 501
                
                $endpoints = $coreEndpoints + $phraseEndpoints + $categoryEndpoints
                if ($IncludeErrors -and (Get-Random -Minimum 1 -Maximum 8) -eq 1) {
                    $endpoints += $errorEndpoints
                }
                
                $result = Invoke-RequestBatch -Endpoints $endpoints -Count $burstSize -DelayMs $delay
                $totalStats.Success += $result.Success
                $totalStats.Error += $result.Error
                $totalStats.Total += $result.Total
                
                if ($totalStats.Total % 25 -eq 0) {
                    $elapsed = [Math]::Round($stopwatch.Elapsed.TotalSeconds, 1)
                    $rps = [Math]::Round($totalStats.Total / $stopwatch.Elapsed.TotalSeconds, 1)
                    Write-Progress-Info "[$elapsed s] Total: $($totalStats.Total) | RPS: $rps" "Green"
                }
            }
        }
        
        "ramp" {
            Write-Progress-Info "?? Ramp-up load pattern - increasing intensity" "Blue"
            
            $segment = $DurationSeconds / 4
            $phase = 1
            
            while ($stopwatch.ElapsedMilliseconds -lt ($DurationSeconds * 1000)) {
                $currentPhase = [Math]::Min(4, [Math]::Ceiling($stopwatch.Elapsed.TotalSeconds / $segment))
                
                if ($currentPhase -ne $phase) {
                    $phase = $currentPhase
                    Write-Progress-Info "?? Phase $phase - Intensity increasing" "Yellow"
                }
                
                # Increase request count and decrease delay each phase
                $requestCount = $phase * 2
                $delay = 500 / $phase
                
                $endpoints = $coreEndpoints
                if ($phase -ge 2) { $endpoints += $phraseEndpoints }
                if ($phase -ge 3) { $endpoints += $categoryEndpoints }
                if ($phase -eq 4 -and $IncludeErrors) { $endpoints += $errorEndpoints }
                
                $result = Invoke-RequestBatch -Endpoints $endpoints -Count $requestCount -DelayMs $delay
                $totalStats.Success += $result.Success
                $totalStats.Error += $result.Error
                $totalStats.Total += $result.Total
            }
        }
        
        default {
            Write-Error "Unknown pattern: $Pattern. Use: steady, burst, mixed, or ramp"
            return
        }
    }
}
catch {
    Write-Progress-Info "Script interrupted: $($_.Exception.Message)" "Red"
}
finally {
    $stopwatch.Stop()
}

Write-Host ""
Write-Host "?? Metrics Generation Complete!" -ForegroundColor Green
Write-Host "================================" -ForegroundColor Magenta
Write-Host "?? Final Statistics:" -ForegroundColor White
Write-Host "   Pattern: $Pattern" -ForegroundColor Cyan
Write-Host "   Duration: $([Math]::Round($stopwatch.Elapsed.TotalSeconds, 1)) seconds" -ForegroundColor Cyan
Write-Host "   Total Requests: $($totalStats.Total)" -ForegroundColor Cyan
Write-Host "   Successful: $($totalStats.Success)" -ForegroundColor Green
Write-Host "   Errors: $($totalStats.Error)" -ForegroundColor $(if ($totalStats.Error -gt 0) { "Red" } else { "Green" })
Write-Host "   Success Rate: $([Math]::Round(($totalStats.Success / $totalStats.Total) * 100, 1))%" -ForegroundColor Cyan
Write-Host "   Average RPS: $([Math]::Round($totalStats.Total / $stopwatch.Elapsed.TotalSeconds, 1))" -ForegroundColor Cyan
Write-Host ""
Write-Host "?? Next Steps:" -ForegroundColor Yellow
Write-Host "   1. Check Grafana dashboard: http://localhost:3000" -ForegroundColor White
Write-Host "   2. Look for '?? SaffaApi - Metrics Dashboard'" -ForegroundColor White
Write-Host "   3. Verify P95 response times are now realistic (1-50ms)" -ForegroundColor White
Write-Host "   4. Check that other metrics are no longer NaN" -ForegroundColor White