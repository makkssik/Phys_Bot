using WeatherBot.Entities.ValueObjects;

namespace WeatherBot.Interfaces.Services;

public interface IWeatherService
{
    Task<WeatherData?> GetCurrentWeatherAsync(string locationName);
    Task<WeatherData?> GetCurrentWeatherAsync(Coordinate coordinate);
    
    Task<List<WeatherAlert>> GetAlertsAsync(string locationName);
}

public record WeatherData(
    Temperature Temperature,
    WeatherCondition Condition,
    double WindSpeed,
    DateTime Timestamp
)
{
    public string Description => Condition.Description;
}