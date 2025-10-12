using WeatherBot.Entities.ValueObjects;

namespace WeatherBot.Interfaces.Services;

public interface ILocationService
{
    Task<Coordinate?> FindCoordinateAsync(string locationName);
}