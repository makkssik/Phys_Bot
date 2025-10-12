using WeatherBot.Interfaces.Services;
using WeatherBot.Entities.ValueObjects;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace WeatherBot.Services;

public class LocationService : ILocationService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<LocationService> _logger;

    public LocationService(HttpClient httpClient, ILogger<LocationService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<Coordinate?> FindCoordinateAsync(string locationName)
    {
        if (string.IsNullOrWhiteSpace(locationName))
        {
            _logger.LogWarning("Location name is empty");
            return null;
        }

        try
        {
            _logger.LogInformation("🔍 Searching coordinates for: {LocationName}", locationName);

            var url = $"https://geocoding-api.open-meteo.com/v1/search?name={Uri.EscapeDataString(locationName)}&count=1&format=json";
            
            var response = await _httpClient.GetAsync(url);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("❌ Geocoding API returned status: {StatusCode}", response.StatusCode);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            _logger.LogDebug("📄 Geocoding API response: {Json}", json);

            var geocodingData = JsonSerializer.Deserialize<GeocodingResponse>(json, new JsonSerializerOptions 
            { 
                PropertyNameCaseInsensitive = true 
            });

            var location = geocodingData?.Results?.FirstOrDefault();
            if (location == null)
            {
                _logger.LogWarning("❌ No coordinates found for: {LocationName}", locationName);
                return null;
            }

            _logger.LogInformation("✅ Found coordinates: {LocationName} -> {Lat}, {Lon}", 
                locationName, location.Latitude, location.Longitude);
                
            return new Coordinate(location.Latitude, location.Longitude);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "❌ Network error while searching coordinates for: {LocationName}", locationName);
            return null;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "❌ JSON parsing error for location: {LocationName}", locationName);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Unexpected error finding coordinates for: {LocationName}", locationName);
            return null;
        }
    }

    private class GeocodingResponse
    {
        public List<GeocodingResult> Results { get; set; } = new();
    }

    private class GeocodingResult
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
    }
}