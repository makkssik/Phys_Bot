using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Globalization;
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

    public WeatherService(
        HttpClient httpClient, 
        ILogger<WeatherService> logger, 
        ILocationService locationService,
        IMemoryCache cache)
    {
        _httpClient = httpClient;
        _logger = logger;
        _locationService = locationService;
        _cache = cache;
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
        return weatherCode switch
        {
            0 => new WeatherCondition("0", "☀️ Clear sky"),
            1 => new WeatherCondition("1", "🌤️ Mainly clear"),
            2 => new WeatherCondition("2", "⛅ Partly cloudy"),
            3 => new WeatherCondition("3", "☁️ Overcast"),
            45 => new WeatherCondition("45", "🌫️ Fog"),
            48 => new WeatherCondition("48", "🌫️ Depositing rime fog"),
            51 => new WeatherCondition("51", "🌧️ Light drizzle"),
            53 => new WeatherCondition("53", "🌧️ Moderate drizzle"),
            55 => new WeatherCondition("55", "🌧️ Dense drizzle"),
            61 => new WeatherCondition("61", "🌧️ Slight rain"),
            63 => new WeatherCondition("63", "🌧️ Moderate rain"),
            65 => new WeatherCondition("65", "🌧️ Heavy rain"),
            71 => new WeatherCondition("71", "🌨️ Slight snow fall"),
            73 => new WeatherCondition("73", "🌨️ Moderate snow fall"),
            75 => new WeatherCondition("75", "🌨️ Heavy snow fall"),
            80 => new WeatherCondition("80", "🌦️ Slight rain showers"),
            81 => new WeatherCondition("81", "🌦️ Moderate rain showers"),
            82 => new WeatherCondition("82", "🌦️ Violent rain showers"),
            95 => new WeatherCondition("95", "⛈️ Thunderstorm"),
            96 => new WeatherCondition("96", "⛈️ Thunderstorm with hail"),
            99 => new WeatherCondition("99", "⛈️ Thunderstorm with heavy hail"),
            _ => new WeatherCondition(weatherCode.ToString(), "❓ Unknown condition")
        };
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
        
        [JsonPropertyName("sender_name")]
        public string SenderName { get; set; } = "";
    }
}