# SaaS – Saffa as a Service 🥩🇿🇦

<p align="center">
  <img src="assets/SaaS.png" alt="South African Phrases Illustration" width="600"/>
</p>

**Sharp sharp!** South African phrases, slang, and culture served fresh via a simple API. Perfect for Dutchies, expats, or anyone who wants to shout *voetsek* in style.

SaaS is the little API that makes South African culture accessible, fun, and sometimes downright cheeky. Whether you're testing your API skills or educating Dutch colleagues, we've got your back.

---

## Why SaaS?

Ever wondered what your Dutch friends would think if you said **"Just now"** or shouted **"Laduma!"**?
SaaS brings the **essence of South Africa** to your fingertips:

* Random South African phrases
* Slang, expressions, and cultural gems
* Dutch-friendly explanations so nobody gets confused
* A touch of humour and a lot of *lekker*

---

## Key Features

* 🌍 Get **all South African phrases**
* 🎲 Get a **random phrase**
* 🇳🇱 Get **phrases explained for Dutch colleagues**
* 📂 Filter by **category**: slang, expression, cultural, south-african
* 🖥️ Tiny, clean **.NET 10 Minimal API**
* 🟨 Beautiful **Scalar UI** & OpenAPI docs
* 🚦 **Smart rate limiting** with chained policies
* ⚡ **Token bucket** algorithm for smooth request handling
* 📊 **Full OpenTelemetry integration** for observability
* 🏥 **Health checks** for monitoring
* 🐳 **Docker & .NET Aspire ready**
* 📈 **Prometheus metrics & Grafana dashboards**

---

## Sample Phrases

| Phrase   | Category      | Actual Meaning                                      | Afrikaans Influence | Dutch Explanation                                                |
| -------- | ------------- | --------------------------------------------------- | ------------------- | ---------------------------------------------------------------- |
| Ag, man! | expression    | Oh man! Expresses pity, resignation, or irritation  | Yes                 | This is not 'ach man' in Dutch; it's more like 'oh jee, man'.    |
| Babbelas | slang         | A hangover; feeling rough after drinking            | Yes                 | Not 'babbelaz'. It means you are hungover, not talking too much. |
| Voetsek  | slang         | Go away! Shoo!                                      | Yes                 | Not polite! Used to tell someone (or something) to get lost.     |
| Lekker   | expression    | Good, nice, fun, or tasty — always positive         | Yes                 | Not just 'lekker' in Dutch; can describe mood, food, or event.   |
| Just now | south-african | Eventually. Not today energy.                       | Yes                 | This is not 'zo meteen'. Do not wait.                            |

> And many more! Check the `/phrase` endpoint for the full collection.

---

## 🚀 Live API

The API is deployed and ready to use! No need to run it locally:

**🌐 Live API:** https://saas.volkwyn.nl  
**📖 Interactive Documentation:** https://saas.volkwyn.nl/scalar/

Try it out directly in your browser with the Scalar UI for testing requests and exploring all available endpoints!

---

## 🏃‍♂️ Quick Start

### Prerequisites

- **.NET 10 SDK** or later
- **Docker** (optional, for containerized deployment)
- **.NET Aspire** (optional, for full observability stack)

### Option 1: Standard Development

```bash
git clone https://github.com/Dev1994/SaaS.git
cd SaaS/SaffaApi
dotnet run
```

API will be live at:
```
HTTP:  http://localhost:5286
HTTPS: https://localhost:7023
```

### Option 2: Docker Deployment

```bash
git clone https://github.com/Dev1994/SaaS.git
cd SaaS
docker-compose up -d
```

API will be available at: http://localhost:8083

### Option 3: Full Observability Stack (.NET Aspire)

```bash
git clone https://github.com/Dev1994/SaaS.git
cd SaaS/AppHost
dotnet run
```

This starts the complete monitoring stack:
- **API**: SaffaApi with full telemetry
- **Metrics**: Prometheus (http://localhost:9090)
- **Dashboards**: Grafana (http://localhost:3000, admin/admin)
- **Telemetry**: OpenTelemetry Collector

---

## Endpoints

| Endpoint                      | Description                                                                  |
| ----------------------------- | ---------------------------------------------------------------------------- |
| `/`                           | Welcome message                                                              |
| `/phrase`                     | Get a random South African phrase                                            |
| `/phrase/dutch`               | Get a random phrase with Dutch explanation                                   |
| `/phrase/{term}`              | Get phrase by exact term                                                     |
| `/phrase/category/{category}` | Filter phrases by category (slang, cultural, expression, south-african)      |
| `/health`                     | Health check endpoint for monitoring                                         |
| `/openapi`                    | OpenAPI / Swagger specification                                              |
| `/scalar`                     | Beautiful Scalar UI with interactive documentation                           |

---

## 🚦 Rate Limiting

To keep the API running smoothly for everyone, we've implemented **intelligent chained rate limits**:

### **Dual-Layer Protection**
* **🏃‍♂️ Burst Protection**: 100 requests per minute per IP (token bucket algorithm)
* **🛡️ Abuse Prevention**: 500 requests per hour per IP (fixed window)
* **⚡ Smart Queuing**: Brief request queuing when approaching limits

### **How It Works**
- **Token Bucket**: Provides smooth rate limiting with automatic token replenishment every minute
- **Chained Enforcement**: Both limits are checked simultaneously - you must pass BOTH
- **Per-IP Tracking**: Limits are applied individually per IP address
- **Graceful Degradation**: Requests are queued briefly before rejection when limits are approached

### **Rate Limit Response**
If you hit either limit, you'll get this friendly South African response:
```http
HTTP/1.1 429 Too Many Requests

Eish! You're going too fast there, boet! Slow down a bit and try again later. 🐢
```

### **Usage Guidelines**
- **Normal usage**: You'll never hit these limits
- **Testing/Development**: Plenty of headroom for API exploration
- **Production apps**: Generous limits for real-world usage
- **Need more?** Reach out if you have legitimate high-volume needs

**Technical Note**: The API uses a combination of `TokenBucketRateLimiter` for minute-based smoothing and `FixedWindowRateLimiter` for hourly enforcement.

---

## 📊 Monitoring & Observability

SaaS now includes comprehensive observability features:

### **OpenTelemetry Integration**
- **Traces**: Full request tracing across the API
- **Metrics**: Performance counters and custom metrics
- **Logs**: Structured logging with correlation IDs
- **Standards**: OTLP protocol for vendor-neutral telemetry

### **Health Monitoring**
- **Health Checks**: `/health` endpoint for service monitoring
- **Docker Health**: Container health checks with curl
- **Startup Probes**: Graceful startup detection

### **Metrics Dashboard**
When running with .NET Aspire, you get:
- **Prometheus**: Metrics collection and storage
- **Grafana**: Pre-configured dashboards for API monitoring
- **Real-time Monitoring**: Request rates, error rates, response times

### **Production Deployment**
- **Container Ready**: Optimized Docker configuration
- **Network Monitoring**: External monitoring network support
- **Data Persistence**: Volume mapping for application data
- **Auto-restart**: Container restart policies for reliability

---

## Example Requests

### Get a random phrase
```bash
curl https://saas.volkwyn.nl/phrase
```

### Get a phrase with Dutch explanation
```bash
curl https://saas.volkwyn.nl/phrase/dutch
```

### Get a specific phrase
```bash
curl https://saas.volkwyn.nl/phrase/braai
```

### Get phrases by category
```bash
curl https://saas.volkwyn.nl/phrase/category/slang
```

### Check API health
```bash
curl https://saas.volkwyn.nl/health
```

**Or try it locally:**
```bash
curl http://localhost:5286/phrase
```

**Sample Response:**

```json
{
  "text": "Braai",
  "category": "cultural",
  "actualMeaning": "Barbecue / social gathering; more than just food.",
  "afrikaansInfluence": true,
  "explainLikeImDutch": "Not a 'barbecue' only; it's an event with friends, drinks, and meat.",
  "misunderstandingProbability": 0.95,
  "confidence": "High"
}
```

---

## Project Structure

```
SaaS/
├── SaffaApi/                 # Main API project
│   ├── Program.cs            # Application entry point
│   ├── Models/
│   │   └── Phrase.cs         # Phrase model definition
│   ├── Services/
│   │   ├── IPhraseService.cs # Service interface
│   │   └── PhraseService.cs  # Service implementation
│   ├── Extensions/           # Extension methods for clean configuration
│   │   ├── ApiExtensions.cs          # API configuration
│   │   ├── CorsExtensions.cs         # CORS setup
│   │   ├── OpenTelemetryExtensions.cs # Observability
│   │   ├── RateLimitingExtensions.cs  # Rate limiting
│   │   └── SecurityExtensions.cs     # Security headers
│   ├── data/
│   │   └── phrases.json      # Phrase data store
│   └── Dockerfile            # Container configuration
├── AppHost/                  # .NET Aspire orchestration
│   ├── AppHost.cs            # Aspire host configuration
│   ├── prometheus/           # Prometheus configuration
│   └── grafana/              # Grafana dashboards
└── docker-compose.yml        # Container deployment
```

---

## Technology Stack

### **Core Framework**
- **.NET 10** - Latest .NET framework
- **ASP.NET Core Minimal APIs** - Lightweight API framework
- **JSON Data Store** - Simple, fast phrase storage

### **API Features**
- **Scalar.AspNetCore** - Modern API documentation UI
- **Microsoft.AspNetCore.OpenApi** - OpenAPI specification support
- **Built-in Rate Limiting** - Token bucket + fixed window algorithms

### **Observability Stack**
- **OpenTelemetry** - Industry-standard telemetry
- **Prometheus** - Metrics collection and storage
- **Grafana** - Visualization and dashboards
- **Health Checks** - Service monitoring

### **Deployment**
- **Docker** - Containerization
- **.NET Aspire** - Cloud-native orchestration
- **Container Health Checks** - Automated monitoring

---

## 🐳 Docker Configuration

The API includes production-ready Docker configuration:

### **Features**
- Multi-stage build optimization
- Health check integration
- Volume persistence
- External network support
- Automatic restart policies

### **Environment Variables**
- `ASPNETCORE_ENVIRONMENT`: Runtime environment
- `OpenTelemetry__*`: Telemetry configuration
- `OTEL_*`: Standard OpenTelemetry variables

---

## Contribute

We love contributions!

* Add missing phrases to `data/phrases.json` 📝
* Suggest better Dutch explanations 🇳🇱
* Improve the API 🚀
* Enhance monitoring dashboards 📊
* Add new observability features 🔍

Pull requests welcome — fork, fix, submit, **sharp sharp!**

---

## License

MIT © Have fun with it! Just don't do anything too shady, hey?

---

![Made with 🥩](https://img.shields.io/badge/Made%20with-%F0%9F%A5%A9-red)
![API Ready](https://img.shields.io/badge/API-Ready-blue)
![SaaS](https://img.shields.io/badge/SaaS-Yes-green)
![South Africa](https://img.shields.io/badge/Culture-SA-yellow)
![.NET 10](https://img.shields.io/badge/.NET-10-purple)
![OpenTelemetry](https://img.shields.io/badge/OpenTelemetry-Enabled-orange)
![Docker](https://img.shields.io/badge/Docker-Ready-blue)
![Monitored](https://img.shields.io/badge/Monitoring-Full-green)
