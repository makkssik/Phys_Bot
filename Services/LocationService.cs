using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using WeatherBot.Entities.ValueObjects;
using WeatherBot.Interfaces.Services;

namespace WeatherBot.Services;

public class LocationService : ILocationService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<LocationService> _logger;
    private readonly IMemoryCache _cache;

    public LocationService(HttpClient httpClient, ILogger<LocationService> logger, IMemoryCache cache)
    {
        _httpClient = httpClient;
        _logger = logger;
        _cache = cache;
    }

    public async Task<Coordinate?> FindCoordinateAsync(string locationName)
    {
        if (string.IsNullOrWhiteSpace(locationName)) return null;

        var cacheKey = $"geo_{locationName.ToLowerInvariant()}";

        if (_cache.TryGetValue(cacheKey, out Coordinate? cachedCoordinate))
        {
            return cachedCoordinate;
        }

        try
        {
            var searchVariants = new[]
            {
                locationName,
                locationName.Replace("-", " "),
            }.Distinct(StringComparer.OrdinalIgnoreCase);

            foreach (var variant in searchVariants)
            {
                var url = $"https://geocoding-api.open-meteo.com/v1/search?name={Uri.EscapeDataString(variant)}&count=1&format=json";

                var response = await _httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode) continue;

                var json = await response.Content.ReadAsStringAsync();
                var geocodingData = JsonSerializer.Deserialize<GeocodingResponse>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (geocodingData?.Results?.Any() == true)
                {
                    var location = geocodingData.Results.First();
                    var coordinate = new Coordinate(location.Latitude, location.Longitude);

                    _cache.Set(cacheKey, coordinate, TimeSpan.FromHours(24));
                    return coordinate;
                }
            }
            
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finding coordinates for: {LocationName}", locationName);
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