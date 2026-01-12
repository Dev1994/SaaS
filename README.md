# SaaS – South African as a Service 🥩🇿🇦

**Sharp sharp!** South African phrases, slang, and culture served fresh via a simple API. Perfect for Dutchies, expats, or anyone who wants to shout *voetsek* in style.

SaaS is the little API that makes South African culture accessible, fun, and sometimes downright cheeky. Whether you’re testing your API skills or educating Dutch colleagues, we’ve got your back.

---

## Why SaaS?

Ever wondered what your Dutch friends would think if you said **“Just now”** or shouted **“Laduma!”**?
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
* 📂 Filter by **category**: slang, expression, cultural
* 🖥️ Tiny, clean **.NET Minimal API**
* 🟨 Beautiful **Scalar UI** & OpenAPI docs

---

## Sample Phrases

| Phrase   | Category      | Actual Meaning                              | Afrikaans Influence | Dutch Explanation                                                |
| -------- | ------------- | ------------------------------------------- | ------------------- | ---------------------------------------------------------------- |
| Ag, man! | expression    | Expresses pity, resignation, or irritation  | Yes                 | This is not 'ach man' in Dutch; it’s more like 'oh jee, man'.    |
| Babbelas | slang         | Hangover; feeling rough after drinking      | Yes                 | Not 'babbelaz'. It means you are hungover, not talking too much. |
| Voetsek  | slang         | Go away! Shoo!                              | Yes                 | Not polite! Used to tell someone (or something) to get lost.     |
| Lekker   | expression    | Good, nice, fun, or tasty — always positive | Yes                 | Not just 'lekker' in Dutch; can describe mood, food, or event.   |
| Just now | south-african | Eventually. Not today energy.               | Yes                 | This is not 'zo meteen'. Do not wait.                            |

> And many more! Check the `/phrase` endpoint for the full collection.

---

## Quick Start

### Install & Run

```bash
git clone https://github.com/your-username/SaaS.git
cd SaaS
dotnet run
```

API will be live at:

```
http://localhost:5000
```

---

## Endpoints

| Endpoint                      | Description                                                                  |
| ----------------------------- | ---------------------------------------------------------------------------- |
| `/`                           | Welcome message                                                              |
| `/phrase`                     | Get a random SA phrase (optional query `?category=slang&language=afrikaans`) |
| `/phrase/random`              | Random South African phrase                                                  |
| `/phrase/dutch`               | All phrases with Dutch explanations                                          |
| `/phrase/random/dutch`        | Random Dutch-explained phrase                                                |
| `/phrase/{term}`              | Get phrase by exact term                                                     |
| `/phrase/category/{category}` | Filter phrases by category                                                   |
| `/scalar`                     | Beautiful Scalar UI with documentation                                       |
| `/openapi`                    | OpenAPI / Swagger UI                                                         |

---

## Example Request

```bash
curl http://localhost:5000/phrase/random
```

**Sample Response:**

```json
{
  "phrase": "Braai",
  "category": "cultural",
  "actual_meaning": "Barbecue / social gathering; more than just food.",
  "afrikaans_influence": true,
  "explain_like_im_dutch": "Not a 'barbecue' only; it’s an event with friends, drinks, and meat.",
  "misunderstanding_probability": 0.95,
  "confidence": "High"
}
```

---

## Contribute

We love contributions!

* Add missing phrases 📝
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
