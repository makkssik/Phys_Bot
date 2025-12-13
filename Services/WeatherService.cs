using WeatherBot.Interfaces.Services;
using WeatherBot.Entities.ValueObjects;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace WeatherBot.Services;

public class WeatherService : IWeatherService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<WeatherService> _logger;
    private readonly ILocationService _locationService;

    private const string ApiKey = "9ac9f622f4c04980947184305251312"; 

    public WeatherService(HttpClient httpClient, ILogger<WeatherService> logger, ILocationService locationService)
    {
        _httpClient = httpClient;
        _logger = logger;
        _locationService = locationService;
    }

    public async Task<WeatherData?> GetCurrentWeatherAsync(string locationName)
    {
        if (string.IsNullOrWhiteSpace(locationName)) return null;

        try
        {
            var coordinate = await _locationService.FindCoordinateAsync(locationName);
            if (coordinate == null) return null;

            return await GetWeatherFromApi(coordinate);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting weather for: {LocationName}", locationName);
            return null;
        }
    }

    public async Task<WeatherData?> GetCurrentWeatherAsync(Coordinate coordinate)
    {
        return await GetWeatherFromApi(coordinate);
    }

    public async Task<List<WeatherAlert>> GetAlertsAsync(string locationName)
    {
        if (string.IsNullOrWhiteSpace(ApiKey) || ApiKey.Contains("ВСТАВЬ"))
        {
            _logger.LogError("❌ WeatherAPI Key is missing!");
            return new List<WeatherAlert>();
        }

        var url = $"http://api.weatherapi.com/v1/forecast.json?key={ApiKey}&q={locationName}&days=1&aqi=no&alerts=yes";

        try
        {
            var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("⚠️ WeatherAPI returned {Status}", response.StatusCode);
                return new List<WeatherAlert>();
            }

            var json = await response.Content.ReadAsStringAsync();
            var data = JsonSerializer.Deserialize<WeatherApiResponse>(json);

            return data?.Alerts?.AlertList ?? new List<WeatherAlert>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error fetching alerts for {Location}", locationName);
            return new List<WeatherAlert>();
        }
    }

    private async Task<WeatherData?> GetWeatherFromApi(Coordinate coordinate)
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
}