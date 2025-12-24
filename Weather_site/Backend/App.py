from fastapi import FastAPI, HTTPException
from fastapi.middleware.cors import CORSMiddleware
from fastapi.staticfiles import StaticFiles
from fastapi.responses import FileResponse
from pydantic import BaseModel
import httpx
import os

from ml_engine import recommender

app = FastAPI(title="WeatherBot Web API + ML")

app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

FRONTEND_DIR = os.path.join(os.path.dirname(__file__), "..", "Frontend")
if not os.path.exists(FRONTEND_DIR):
    FRONTEND_DIR = os.path.join(os.getcwd(), "Frontend")

if os.path.exists(FRONTEND_DIR):
    app.mount("/static", StaticFiles(directory=FRONTEND_DIR), name="static")

@app.on_event("startup")
async def startup_event():
    recommender.load_and_train()

class WeatherRequest(BaseModel):
    temperature: float
    wind_speed: float
    weather_code: int
    hobbies: str = ""

@app.post("/api/recommend")
async def recommend_clothing(data: WeatherRequest):
    clothes_advice = recommender.predict_clothing(
        data.temperature, data.wind_speed, data.weather_code
    )
    
    hobby_advice_list = []
    if data.hobbies:
        user_hobbies = [h.strip() for h in data.hobbies.split(',')]
        
        for hobby in user_hobbies:
            advice = recommender.predict_hobby(
                data.temperature, data.wind_speed, data.weather_code, hobby
            )
            if advice:
                hobby_advice_list.append(f"🎯 <b>{hobby.capitalize()}:</b> {advice}")

    full_advice = clothes_advice
    if hobby_advice_list:
        full_advice += "\n\n" + "\n".join(hobby_advice_list)

    return {"recommendation": full_advice}

OPEN_METEO_GEOCODE_URL = "https://geocoding-api.open-meteo.com/v1/search"
OPEN_METEO_FORECAST_URL = "https://api.open-meteo.com/v1/forecast"

@app.get("/", response_class=FileResponse)
def serve_index():
    index_path = os.path.join(FRONTEND_DIR, "index.html")
    if os.path.exists(index_path): return index_path
    return {"error": "Frontend files not found"}

async def geocode_city(city: str):
    city = city.strip()
    if not city: raise HTTPException(status_code=400, detail="Empty city")
    params = {"name": city, "count": 1, "language": "en", "format": "json"}
    async with httpx.AsyncClient(timeout=5.0) as client:
        resp = await client.get(OPEN_METEO_GEOCODE_URL, params=params)
    if resp.status_code != 200 or not resp.json().get("results"):
        raise HTTPException(status_code=404, detail="City not found")
    r = resp.json()["results"][0]
    return (r["latitude"], r["longitude"], r.get("name", city), r.get("country", ""))

@app.get("/api/weather")
async def get_weather(city: str, days: int = 3):
    days = max(1, min(days, 16))
    lat, lon, loc_name, country = await geocode_city(city)
    params = {
        "latitude": lat, "longitude": lon, "current_weather": "true",
        "temperature_unit": "celsius", "windspeed_unit": "ms", "timezone": "auto",
        "daily": "temperature_2m_max,temperature_2m_min,weathercode", "forecast_days": days,
    }
    async with httpx.AsyncClient(timeout=5.0) as client:
        resp = await client.get(OPEN_METEO_FORECAST_URL, params=params)
    if resp.status_code != 200: raise HTTPException(status_code=502, detail="Weather API error")
    
    data = resp.json()
    current = data.get("current_weather")
    if not current: raise HTTPException(status_code=502, detail="No weather data")

    ai_advice = recommender.predict_clothing(current["temperature"], current["windspeed"], current["weathercode"])

    forecast = []
    if data.get("daily"):
        d = data["daily"]
        for i in range(min(days, len(d.get("time", [])))):
            forecast.append({
                "date": d["time"][i],
                "temp_max": d["temperature_2m_max"][i],
                "temp_min": d["temperature_2m_min"][i],
                "weathercode": d["weathercode"][i],
            })

    return {
        "location": {"name": loc_name, "country": country, "latitude": lat, "longitude": lon},
        "current": current, "forecast": forecast, "ai_advice": ai_advice
    }