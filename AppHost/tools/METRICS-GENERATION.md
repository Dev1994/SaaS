# 📊 Metrics Generation Tools

This directory contains tools to generate HTTP traffic for testing your Grafana dashboard and validating the corrected P95 response time metrics.

## 🎯 Purpose

These scripts help you:
- **Test the fixed Grafana dashboard** after correcting the query issues
- **Generate realistic metrics** to replace the erroneous 4.75s P95 response times
- **Validate dashboard functionality** with various load patterns
- **Verify OpenTelemetry data flow** from API → OTLP → Prometheus → Grafana

## 📁 Files Overview

### 🚀 `run-metrics-generator.bat`
**Windows batch file with interactive menu**
- Easy-to-use interface for selecting load patterns
- Validates prerequisites (PowerShell, correct directory structure)
- Provides guided next steps after completion

### ⚡ `generate-metrics-advanced.ps1`
**Advanced PowerShell script with multiple load patterns**
- `steady`: Consistent 5 RPS load
- `burst`: Alternating high/low traffic bursts
- `mixed`: Random patterns with optional error simulation
- `ramp`: Gradually increasing load intensity

### 📈 `generate-metrics.ps1`
**Simple PowerShell script for basic load testing**
- Straightforward steady load generation
- Configurable duration and request rate
- Good for basic dashboard testing

## 🚀 Quick Start

### Option 1: Interactive Menu (Recommended)
```cmd
# From the AppHost/tools directory:
cd AppHost/tools
run-metrics-generator.bat
```

### Option 2: Direct PowerShell
```powershell
# From AppHost/tools directory:
cd AppHost/tools

# Steady load for 60 seconds
.\generate-metrics-advanced.ps1 -Pattern "steady"

# Mixed load with error scenarios
.\generate-metrics-advanced.ps1 -Pattern "mixed" -IncludeErrors

# Quick 30-second test
.\generate-metrics-advanced.ps1 -Pattern "steady" -DurationSeconds 30
```

### Option 3: Custom Parameters
```powershell
# Custom API endpoint and duration
.\generate-metrics.ps1 -BaseUrl "https://your-api.com" -DurationSeconds 120 -RequestsPerSecond 10
```

## 📊 API Endpoints Tested

The scripts target various SaffaApi endpoints:

### Core Endpoints
- `/` - Root endpoint
- `/phrase` - Random phrase (main performance target)
- `/phrase/dutch` - Dutch explanation phrases
- `/health` - Health check

### Specific Phrases
- `/phrase/braai` - Specific phrase lookup
- `/phrase/lekker` - Another phrase test
- `/phrase/voetsek` - Additional phrase validation

### Category Endpoints
- `/phrase/category/slang` - Category filtering
- `/phrase/category/cultural` - Cultural phrases
- `/phrase/category/expression` - Expression phrases

### Error Scenarios (when enabled)
- `/phrase/nonexistent` - 404 testing
- `/phrase/category/invalid` - Invalid category testing

## 📈 Expected Dashboard Results

After running the metrics generation, your Grafana dashboard should show:

### ✅ Fixed Metrics
- **P95 Response Time**: 1-50ms (instead of 4.75s)
- **Request Rate**: Actual RPS values (not NaN)
- **Status Codes**: Proper 200/404 distribution
- **Error Rate**: Low percentage or zero

### 📊 Dashboard Panels
- **API Health Overview**: Real-time stats
- **Traffic Analysis**: Request patterns by route
- **Performance Deep Dive**: Response time percentiles
- **Route Breakdown**: Per-endpoint performance

## 🔧 Troubleshooting

### Common Issues

**"PowerShell is not available"**
- Install PowerShell 7+ from Microsoft
- Or use Windows PowerShell (built-in)

**"This script should be run from the AppHost\tools directory"**
- Navigate to the AppHost/tools folder first
- Ensure `AppHost.cs` exists in the parent directory (`../AppHost.cs`)

**"Connection refused" errors**
- Ensure your Aspire application is running (`dotnet run` from AppHost directory)
- Verify API is accessible at http://localhost:5286
- Check that all containers (Grafana, Prometheus, OTLP) are running

**Dashboard not showing data**
- Wait 15-30 seconds for metrics to flow through the pipeline
- Check Prometheus targets are healthy at http://localhost:9090/targets
- Verify OTLP collector is receiving data

### Script Parameters

| Parameter | Default | Description |
|-----------|---------|-------------|
| `BaseUrl` | `http://localhost:5286` | API base URL |
| `DurationSeconds` | `60` | How long to run tests |
| `RequestsPerSecond` | `5` | Rate for simple script |
| `Pattern` | `steady` | Load pattern type |
| `IncludeErrors` | `false` | Include 404 scenarios |
