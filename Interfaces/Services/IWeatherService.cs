using WeatherBot.Entities.ValueObjects;

namespace WeatherBot.Interfaces.Services;

public interface IWeatherService
{
    Task<WeatherData?> GetCurrentWeatherAsync(string locationName);
    Task<WeatherData?> GetCurrentWeatherAsync(Coordinate coordinate);
    Task<List<WeatherAlert>> GetAlertsAsync(string locationName);
    
    Task<string> GetClothingRecommendationAsync(double temp, double wind, int code, string hobbies = "");
}