using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration; 
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using WeatherBot.Entities.ValueObjects;
using WeatherBot.Interfaces.Services;

namespace WeatherBot.Services;

public class WeatherService : IWeatherService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<WeatherService> _logger;
    private readonly ILocationService _locationService;
    private readonly IMemoryCache _cache;
    private readonly string _mlApiUrl; 
    
    public WeatherService(
        HttpClient httpClient, 
        ILogger<WeatherService> logger, 
        ILocationService locationService,
        IMemoryCache cache,
        IConfiguration configuration) 
    {
        _httpClient = httpClient;
        _logger = logger;
        _locationService = locationService;
        _cache = cache;
        
        _mlApiUrl = configuration["AppConfig:MlApiUrl"] 
                    ?? throw new Exception("URL ML-сервиса не найден в appsettings.json");
    }

    public async Task<WeatherData?> GetCurrentWeatherAsync(string locationName)
    {
        var coordinate = await _locationService.FindCoordinateAsync(locationName);
        if (coordinate == null) return null;

        return await GetCurrentWeatherAsync(coordinate);
    }

    public async Task<WeatherData?> GetCurrentWeatherAsync(Coordinate coordinate)
    {
        string cacheKey = $"weather_{coordinate.Latitude}_{coordinate.Longitude}";
        
        if (_cache.TryGetValue(cacheKey, out WeatherData? cachedWeather))
        {
            return cachedWeather;
        }

        var weather = await FetchWeatherFromApi(coordinate);
        if (weather != null)
        {
            _cache.Set(cacheKey, weather, TimeSpan.FromMinutes(15));
        }
        return weather;
    }

    public async Task<string> GetClothingRecommendationAsync(double temp, double wind, int code, string hobbies = "")
    {
        try
        {
            var payload = new
            {
                temperature = temp,
                wind_speed = wind,
                weather_code = code,
                hobbies = hobbies
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            
            var response = await _httpClient.PostAsync($"{_mlApiUrl}/api/recommend", content, cts.Token);
            
            if (response.IsSuccessStatusCode)
            {
                var responseString = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(responseString);
                if (doc.RootElement.TryGetProperty("recommendation", out var recElement))
                {
                    return recElement.GetString() ?? "";
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"ML Recommendation failed: {ex.Message}. Make sure Python backend is running.");
        }

        return "";
    }

    public async Task<List<WeatherAlert>> GetAlertsAsync(string locationName)
    {
        var coordinate = await _locationService.FindCoordinateAsync(locationName);
        if (coordinate == null) return new List<WeatherAlert>();

        string cacheKey = $"alerts_{locationName.ToLowerInvariant()}";
        if (_cache.TryGetValue(cacheKey, out List<WeatherAlert>? cachedAlerts))
        {
            return cachedAlerts ?? new List<WeatherAlert>();
        }

        try
        {
            var lat = coordinate.Latitude.ToString(CultureInfo.InvariantCulture);
            var lon = coordinate.Longitude.ToString(CultureInfo.InvariantCulture);
            
            var url = $"https://api.open-meteo.com/v1/forecast?latitude={lat}&longitude={lon}&alerts=yes&forecast_days=1";

            var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode) return new List<WeatherAlert>();

            var json = await response.Content.ReadAsStringAsync();
            var omData = JsonSerializer.Deserialize<OpenMeteoAlertResponse>(json);

            var alerts = new List<WeatherAlert>();
            if (omData?.Alerts != null)
            {
                foreach (var omAlert in omData.Alerts)
                {
                    alerts.Add(new WeatherAlert(
                        Headline: omAlert.Event,
                        Severity: "Unknown",
                        Event: omAlert.Event,
                        Description: omAlert.Description,
                        Instruction: "" 
                    ));
                }
            }

            _cache.Set(cacheKey, alerts, TimeSpan.FromMinutes(10));
            return alerts;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get alerts for {Location}", locationName);
            return new List<WeatherAlert>();
        }
    }

    private async Task<WeatherData?> FetchWeatherFromApi(Coordinate coordinate)
    {
        try
        {
            var latitude = coordinate.Latitude.ToString(CultureInfo.InvariantCulture);
            var longitude = coordinate.Longitude.ToString(CultureInfo.InvariantCulture);

            var url = $"https://api.open-meteo.com/v1/forecast?" +
                     $"latitude={latitude}&longitude={longitude}&" +
                     $"current_weather=true&temperature_unit=celsius&windspeed_unit=ms&timezone=auto";

            var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync();
            var weatherResponse = JsonSerializer.Deserialize<OpenMeteoResponse>(json, new JsonSerializerOptions 
            { 
                PropertyNameCaseInsensitive = true 
            });

            if (weatherResponse?.CurrentWeather == null) return null;

            var condition = GetWeatherCondition(weatherResponse.CurrentWeather.WeatherCode);
            
            return new WeatherData(
                new Temperature(weatherResponse.CurrentWeather.Temperature),
                condition,
                (double)weatherResponse.CurrentWeather.WindSpeed,
                weatherResponse.CurrentWeather.Time
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Open-Meteo data");
            return null;
        }
    }

    private static WeatherCondition GetWeatherCondition(int weatherCode)
    {
        string desc = weatherCode switch
        {
            0 => "☀️ Clear sky",
            1 => "🌤️ Mainly clear",
            2 => "⛅ Partly cloudy",
            3 => "☁️ Overcast",
            45 => "🌫️ Fog",
            48 => "🌫️ Depositing rime fog",
            51 => "🌧️ Light drizzle",
            53 => "🌧️ Moderate drizzle",
            55 => "🌧️ Dense drizzle",
            61 => "🌧️ Slight rain",
            63 => "🌧️ Moderate rain",
            65 => "🌧️ Heavy rain",
            71 => "🌨️ Slight snow fall",
            73 => "🌨️ Moderate snow fall",
            75 => "🌨️ Heavy snow fall",
            80 => "🌦️ Slight rain showers",
            81 => "🌦️ Moderate rain showers",
            82 => "🌦️ Violent rain showers",
            95 => "⛈️ Thunderstorm",
            _ => "❓ Unknown condition"
        };
        return new WeatherCondition(weatherCode.ToString(), desc);
    }
    
    private class OpenMeteoResponse
    {
        [JsonPropertyName("current_weather")]
        public CurrentWeatherData? CurrentWeather { get; set; }
    }

    private class CurrentWeatherData
    {
        [JsonPropertyName("temperature")]
        public decimal Temperature { get; set; }
        
        [JsonPropertyName("windspeed")]
        public decimal WindSpeed { get; set; }
        
        [JsonPropertyName("weathercode")]
        public int WeatherCode { get; set; }
        
        [JsonPropertyName("time")]
        public DateTime Time { get; set; }
    }

    private class OpenMeteoAlertResponse
    {
        [JsonPropertyName("alerts")]
        public List<OpenMeteoAlertItem>? Alerts { get; set; }
    }

    private class OpenMeteoAlertItem
    {
        [JsonPropertyName("event")]
        public string Event { get; set; } = "";
        
        [JsonPropertyName("description")]
        public string Description { get; set; } = "";
    }
}