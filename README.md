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

## Quick Start

### Prerequisites

- **.NET 10 SDK** or later

### Install & Run

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

---

## Endpoints

| Endpoint                      | Description                                                                  |
| ----------------------------- | ---------------------------------------------------------------------------- |
| `/`                           | Welcome message                                                              |
| `/phrase`                     | Get a random South African phrase                                            |
| `/phrase/dutch`               | Get a random phrase with Dutch explanation                                   |
| `/phrase/{term}`              | Get phrase by exact term                                                     |
| `/phrase/category/{category}` | Filter phrases by category (slang, cultural, expression, south-african)      |
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
SaffaApi/
├── Program.cs                # Main application entry point
├── Models/
│   └── Phrase.cs             # Phrase model definition
├── Services/
│   ├── IPhraseService.cs     # Service interface
│   └── PhraseService.cs      # Service implementation
└── data/
    └── phrases.json          # Phrase data store
```

---

## Technology Stack

- **.NET 10** - Latest .NET framework
- **ASP.NET Core Minimal APIs** - Lightweight API framework  
- **Built-in Rate Limiting** - Token bucket + fixed window algorithms
- **Scalar.AspNetCore** - Modern API documentation UI
- **Microsoft.AspNetCore.OpenApi** - OpenAPI specification support
- **JSON Data Store** - Simple, fast phrase storage

---

## Contribute

We love contributions!

* Add missing phrases to `data/phrases.json` 📝
* Suggest better Dutch explanations 🇳🇱
* Improve the API 🚀

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
