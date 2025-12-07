using WeatherBot.Interfaces.Services;
using WeatherBot.Entities.ValueObjects;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace WeatherBot.Services;

public class WeatherService : IWeatherService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<WeatherService> _logger;
    private readonly ILocationService _locationService;

    public WeatherService(HttpClient httpClient, ILogger<WeatherService> logger, ILocationService locationService)
    {
        _httpClient = httpClient;
        _logger = logger;
        _locationService = locationService;
    }

    public async Task<WeatherData?> GetCurrentWeatherAsync(string locationName)
    {
        if (string.IsNullOrWhiteSpace(locationName))
        {
            _logger.LogWarning("Location name is empty");
            return null;
        }

        try
        {
            _logger.LogInformation("🌤️ Getting weather for: {LocationName}", locationName);

            var coordinate = await _locationService.FindCoordinateAsync(locationName);
            if (coordinate == null)
            {
                _logger.LogWarning("❌ Cannot get weather without coordinates for: {LocationName}", locationName);
                return null;
            }

            return await GetWeatherFromApi(coordinate);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error getting weather for: {LocationName}", locationName);
            return null;
        }
    }

    public async Task<WeatherData?> GetCurrentWeatherAsync(Coordinate coordinate)
    {
        return await GetWeatherFromApi(coordinate);
    }

    private async Task<WeatherData?> GetWeatherFromApi(Coordinate coordinate)
    {
        try
        {
            var url = $"https://api.open-meteo.com/v1/forecast?" +
                     $"latitude={coordinate.Latitude}&longitude={coordinate.Longitude}&" +
                     $"current_weather=true&temperature_unit=celsius&windspeed_unit=ms&timezone=auto";

            _logger.LogDebug("🌐 Weather API URL: {Url}", url);

            var response = await _httpClient.GetAsync(url);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("❌ Weather API returned status: {StatusCode}", response.StatusCode);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            _logger.LogDebug("📄 Weather API response: {Json}", json);

            var weatherResponse = JsonSerializer.Deserialize<OpenMeteoResponse>(json, new JsonSerializerOptions 
            { 
                PropertyNameCaseInsensitive = true 
            });

            if (weatherResponse?.CurrentWeather == null)
            {
                _logger.LogWarning("❌ No current_weather data in API response");
                return null;
            }

            var condition = GetWeatherCondition(weatherResponse.CurrentWeather.WeatherCode);
            
            var weatherData = new WeatherData(
                new Temperature(weatherResponse.CurrentWeather.Temperature),
                condition,
                (double)weatherResponse.CurrentWeather.WindSpeed,
                weatherResponse.CurrentWeather.Time
            );

            _logger.LogInformation("✅ Weather data retrieved: {Temp}°C, {Condition}", 
                weatherResponse.CurrentWeather.Temperature, condition.Description);

            return weatherData;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "❌ Network error while getting weather data");
            return null;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "❌ JSON parsing error for weather data");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Unexpected error getting weather data");
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
            56 => new WeatherCondition("56", "🌧️ Light freezing drizzle"),
            57 => new WeatherCondition("57", "🌧️ Dense freezing drizzle"),
            61 => new WeatherCondition("61", "🌧️ Slight rain"),
            63 => new WeatherCondition("63", "🌧️ Moderate rain"),
            65 => new WeatherCondition("65", "🌧️ Heavy rain"),
            66 => new WeatherCondition("66", "🌧️ Light freezing rain"),
            67 => new WeatherCondition("67", "🌧️ Heavy freezing rain"),
            71 => new WeatherCondition("71", "🌨️ Slight snow fall"),
            73 => new WeatherCondition("73", "🌨️ Moderate snow fall"),
            75 => new WeatherCondition("75", "🌨️ Heavy snow fall"),
            77 => new WeatherCondition("77", "🌨️ Snow grains"),
            80 => new WeatherCondition("80", "🌦️ Slight rain showers"),
            81 => new WeatherCondition("81", "🌦️ Moderate rain showers"),
            82 => new WeatherCondition("82", "🌦️ Violent rain showers"),
            85 => new WeatherCondition("85", "🌨️ Slight snow showers"),
            86 => new WeatherCondition("86", "🌨️ Heavy snow showers"),
            95 => new WeatherCondition("95", "⛈️ Thunderstorm"),
            96 => new WeatherCondition("96", "⛈️ Thunderstorm with slight hail"),
            99 => new WeatherCondition("99", "⛈️ Thunderstorm with heavy hail"),
            _ => new WeatherCondition(weatherCode.ToString(), "❓ Unknown weather condition")
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
