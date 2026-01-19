# AppHost - .NET Aspire Observability Stack

This directory contains the .NET Aspire application host configuration and observability infrastructure for the SaffaApi project.

## 📁 Directory Structure

```
AppHost/
├── AppHost.cs              # Aspire application host configuration
├── grafana/                # Grafana configuration and dashboards
│   ├── grafana.ini         # Grafana server configuration
│   ├── grafana-datasources.yml  # Data source configuration
│   └── dashboards/         # Pre-built Grafana dashboards
├── prometheus/             # Prometheus configuration
│   ├── prometheus.yml      # Prometheus server config
│   └── otel-config.yml     # OpenTelemetry Collector config
└── tools/                  # 🔧 Testing and metrics generation tools
    ├── run-metrics-generator.bat       # Interactive metrics generator
    ├── generate-metrics-advanced.ps1   # Advanced load testing
    ├── generate-metrics.ps1            # Simple metrics generator
    └── METRICS-GENERATION.md          # Tools documentation
```

## 🚀 Quick Start

### Start the Aspire Stack
```bash
# From AppHost directory
dotnet run
```

This starts:
- **SaffaApi**: Your API with OpenTelemetry instrumentation
- **OTLP Collector**: Receives telemetry data
- **Prometheus**: Stores metrics
- **Grafana**: Visualizes metrics and traces
- **Jaeger**: Distributed tracing UI

### Access Services
- **Aspire Dashboard**: http://localhost:15888 (or as shown in console)
- **Grafana**: http://localhost:3000 (admin/admin)
- **Prometheus**: http://localhost:9090
- **Jaeger**: http://localhost:16686
- **API**: http://localhost:5286

### Test Dashboard Metrics
```bash
# Navigate to tools directory
cd tools

# Run interactive metrics generator
run-metrics-generator.bat

# Or use PowerShell directly
.\generate-metrics-advanced.ps1 -Pattern "mixed" -IncludeErrors
```

## 🎯 Key Features

### Fixed Grafana Queries
- ✅ Corrected P95 response time calculations
- ✅ Proper ASP.NET Core OpenTelemetry metric names
- ✅ Accurate request rate and error rate queries
- ✅ Fixed status code label mappings

### Observability Stack
- **Distributed Tracing**: Jaeger integration
- **Metrics Collection**: Prometheus + OTLP
- **Visualization**: Custom Grafana dashboards
- **Health Monitoring**: Built-in health checks

### Testing Tools
- **Load Testing**: Multiple traffic patterns
- **Error Simulation**: 404 scenarios for error rate testing
- **Performance Validation**: Verify corrected P95 metrics
- **Interactive Interface**: Easy-to-use batch scripts

## 📊 Expected Results

After fixing the dashboard queries, you should see:
- **P95 Response Times**: 1-50ms (not 4.75s)
- **Request Rates**: Actual RPS values (no NaN)
- **Status Codes**: Proper distribution
- **Error Rates**: Low or zero percentages

## 🔧 Troubleshooting

### Common Issues
1. **Containers not starting**: Check Docker is running
2. **Dashboard not loading**: Wait 30-60 seconds for provisioning
3. **No metrics data**: Run tools/run-metrics-generator.bat
4. **Permission errors**: Run PowerShell as Administrator

### Useful URLs
- **Check Prometheus targets**: http://localhost:9090/targets
- **OTLP collector status**: Check Aspire dashboard
- **API health**: http://localhost:5286/health