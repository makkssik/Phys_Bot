const API_BASE = "https://conglobately-unempty-rosio.ngrok-free.dev/api/weather";

let forecastDays = 3;

const cityInput = document.getElementById("city-input");
const searchForm = document.getElementById("search-form");
const statusBox = document.getElementById("status");
const weatherSection = document.getElementById("weather-section");
const locationInfo = document.getElementById("location-info");

const currentIcon = document.getElementById("current-icon");
const currentCondition = document.getElementById("current-condition");
const currentTemp = document.getElementById("current-temp");
const currentWind = document.getElementById("current-wind");
const currentUpdated = document.getElementById("current-updated");

const forecastContent = document.getElementById("forecast-content");

function animateTemperature(element, target) {
  let current = 0;
  const step = Math.max(1, Math.floor(target / 60));
  const interval = setInterval(() => {
      current += step;
      if (current >= target) {
          current = target;
          clearInterval(interval);
      }
      element.textContent = current + "°C";
  }, 16);
}

function showStatus(message, type = "info") {
  statusBox.textContent = message;
  statusBox.classList.remove("hidden", "error", "info");
  statusBox.classList.add(type);
}

function hideStatus() {
  statusBox.classList.add("hidden");
}

function showWeatherSection() {
  weatherSection.classList.remove("hidden");
}

function weatherCodeToEmoji(code) {
  code = Number(code);
  if (code === 0) return "☀️ Ясно";
  if (code === 1) return "🌤️ В основном ясно";
  if (code === 2) return "⛅ Переменная облачность";
  if (code === 3) return "☁️ Пасмурно";
  if ([45, 48].includes(code)) return "🌫️ Туман";
  if ([51, 53, 55].includes(code)) return "🌦️ Моросящий дождь";
  if ([61, 63, 65].includes(code)) return "🌧️ Дождь";
  if ([71, 73, 75].includes(code)) return "❄️ Снег";
  if (code === 95) return "⛈️ Гроза";
  return "🌈 Неизвестно";
}

function formatDate(dateStr) {
  const d = new Date(dateStr);
  return d.toLocaleDateString("ru-RU", {
    weekday: "short",
    day: "2-digit",
    month: "2-digit",
  });
}

async function loadWeather(city) {
  if (!city || !city.trim()) return;

  showStatus("Загружаем погоду…", "info");
  weatherSection.classList.add("hidden");

  try {
    const url = `${API_BASE}?city=${encodeURIComponent(city)}&days=${forecastDays}&ngrok-skip-browser-warning=true`;
    const resp = await fetch(url);

    if (!resp.ok) {
      let msg = `Ошибка: ${resp.status}`;
      try {
        const errData = await resp.json();
        if (errData.detail) msg += ` — ${errData.detail}`;
      } catch {}
      showStatus(msg, "error");
      return;
    }

    const data = await resp.json();
    renderWeather(data);
    hideStatus();
    showWeatherSection();
  } catch (e) {
    console.error(e);
    showStatus("Не удалось получить данные о погоде. Попробуйте ещё раз.", "error");
  }
}

function renderWeather(data) {
  const loc = data.location;
  const current = data.current;
  const forecast = data.forecast || [];

  locationInfo.innerHTML = `
    Город: <span>${loc.name}</span>
    ${loc.country ? `, страна: <span>${loc.country}</span>` : ""}
  `;

  animateTemperature(currentTemp, Math.round(current.temperature));
  currentCondition.textContent = weatherCodeToEmoji(current.weathercode);

  const emoji = weatherCodeToEmoji(current.weathercode).split(" ")[0];
  currentIcon.src = `https://open-meteo.com/images/weather-icons/${current.weathercode}.png`;
  currentIcon.onerror = () => {
    currentIcon.src = "";
    currentIcon.style.display = "none";
  };

  function applyBackgroundByWeather(code) {
    const body = document.body;
    body.className = "";

    code = Number(code);

    if (code === 0) body.classList.add("weather-clear");
    else if (code === 1 || code === 2) body.classList.add("weather-partly");
    else if (code === 3) body.classList.add("weather-cloudy");
    else if (code >= 51 && code <= 67) body.classList.add("weather-rain");
    else if (code >= 71 && code <= 86) body.classList.add("weather-snow");
    else if (code >= 95) body.classList.add("weather-storm");
    else if ([45, 48].includes(code)) body.classList.add("weather-fog");
    else body.classList.add("weather-cloudy");
}

  function applyWeatherGradient(code) {
    const cards = document.querySelectorAll(".dynamic-weather-card");

    let themeClass = "weather-cloudy";

    code = Number(code);
    if (code === 0) themeClass = "weather-clear";
    else if (code === 1 || code === 2) themeClass = "weather-partly";
    else if (code === 3) themeClass = "weather-cloudy";
    else if (code >= 51 && code <= 67) themeClass = "weather-rain";
    else if (code >= 71 && code <= 86) themeClass = "weather-snow";
    else if (code >= 95) themeClass = "weather-storm";
    else if ([45, 48].includes(code)) themeClass = "weather-fog";

    cards.forEach(card => {
        card.classList.remove(
            "weather-clear",
            "weather-partly",
            "weather-cloudy",
            "weather-rain",
            "weather-snow",
            "weather-fog",
            "weather-storm"
        );
        card.classList.add(themeClass);
    });
}

  currentWind.textContent = `${current.windspeed} м/с`;
  currentUpdated.textContent = new Date(current.time).toLocaleString("ru-RU");

  applyWeatherGradient(current.weathercode);
  applyBackgroundByWeather(current.weathercode);

  forecastContent.innerHTML = "";
  if (!forecast.length) {
    forecastContent.textContent = "Нет данных о прогнозе.";
    return;
  }

  forecast.forEach((day) => {
    const line = document.createElement("div");
    line.className = "forecast-day";
    line.innerHTML = `
      <div class="forecast-date">${formatDate(day.date)}</div>
      <div class="forecast-temps">
        ${Math.round(day.temp_max)}° / ${Math.round(day.temp_min)}°
      </div>
      <div class="forecast-desc">${weatherCodeToEmoji(day.weathercode)}</div>
    `;
    forecastContent.appendChild(line);
  });
}

document.querySelectorAll(".forecast-btn").forEach(btn => {
  btn.addEventListener("click", () => {

    document.querySelectorAll(".forecast-btn").forEach(el => 
      el.classList.remove("active")
    );
    btn.classList.add("active");

    forecastDays = Number(btn.dataset.days);

    const titleEl = document.getElementById("forecast-title");
    if (titleEl) {
      titleEl.textContent = `Прогноз на ${forecastDays} `
        + (forecastDays === 1 ? "день" :
           forecastDays <= 4 ? "дня" : "дней");
    }

    const city = document.getElementById("city-input").value.trim();
    if (city.length > 0) {
      loadWeather(city);
    }
  });
});

searchForm.addEventListener("submit", (e) => {
  e.preventDefault();
  const city = cityInput.value;
  loadWeather(city);

  const url = new URL(window.location.href);
  url.searchParams.set("city", city);
  window.history.replaceState({}, "", url.toString());
});

document.addEventListener("DOMContentLoaded", () => {
  const params = new URLSearchParams(window.location.search);
  const city = params.get("city");
  if (city) {
    cityInput.value = city;
    loadWeather(city);
  }
});