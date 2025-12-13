from fastapi import FastAPI, HTTPException
from fastapi.middleware.cors import CORSMiddleware
from fastapi.staticfiles import StaticFiles
from fastapi.responses import FileResponse
import httpx
import os

app = FastAPI(
    title="WeatherBot Web API",
    description="Backend для сайта с погодой, работающий вместе с Telegram-ботом",
    version="1.0.0",
)

app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

FRONTEND_DIR = os.path.join(os.path.dirname(__file__), "..", "Frontend")

app.mount(
    "/static",
    StaticFiles(directory=FRONTEND_DIR),
    name="static"
)

@app.get("/", response_class=FileResponse)
def serve_index():
    index_path = os.path.join(FRONTEND_DIR, "index.html")
    return index_path

OPEN_METEO_GEOCODE_URL = "https://geocoding-api.open-meteo.com/v1/search"
OPEN_METEO_FORECAST_URL = "https://api.open-meteo.com/v1/forecast"


async def geocode_city(city: str):
    city = city.strip()
    if not city:
        raise HTTPException(status_code=400, detail="City name is empty")

    params = {
        "name": city,
        "count": 1,
        "language": "en",
        "format": "json",
    }

    async with httpx.AsyncClient(timeout=5.0) as client:
        resp = await client.get(OPEN_METEO_GEOCODE_URL, params=params)

    if resp.status_code != 200:
        raise HTTPException(status_code=502, detail="Geocoding service error")

    data = resp.json()
    results = data.get("results")
    if not results:
        raise HTTPException(status_code=404, detail="City not found")

    r = results[0]
    return (
        r["latitude"],
        r["longitude"],
        r.get("name", city),
        r.get("country", "")
    )


@app.get("/api/weather")
async def get_weather(city: str, days: int = 3):
    days = max(1, min(days, 16))

    lat, lon, loc_name, country = await geocode_city(city)

    params = {
        "latitude": lat,
        "longitude": lon,
        "current_weather": "true",
        "temperature_unit": "celsius",
        "windspeed_unit": "ms",
        "timezone": "auto",
        "daily": "temperature_2m_max,temperature_2m_min,weathercode",
        "forecast_days": days,
    }

    async with httpx.AsyncClient(timeout=5.0) as client:
        resp = await client.get(OPEN_METEO_FORECAST_URL, params=params)

    if resp.status_code != 200:
        raise HTTPException(status_code=502, detail="Weather service error")

    data = resp.json()
    current = data.get("current_weather")
    daily = data.get("daily")

    if not current:
        raise HTTPException(status_code=502, detail="No current weather data")

    forecast = []
    if daily:
        for i in range(min(days, len(daily.get("time", [])))):
            forecast.append(
                {
                    "date": daily["time"][i],
                    "temp_max": daily["temperature_2m_max"][i],
                    "temp_min": daily["temperature_2m_min"][i],
                    "weathercode": daily["weathercode"][i],
                }
            )

    return {
        "location": {
            "query": city,
            "name": loc_name,
            "country": country,
            "latitude": lat,
            "longitude": lon,
        },
        "current": current,
        "forecast": forecast,
    }

@app.get("/health")
async def health():
    return {"status": "ok"}