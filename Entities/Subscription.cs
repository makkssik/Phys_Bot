namespace WeatherBot.Entities;

using WeatherBot.Entities.ValueObjects;
using System.Text.Json.Serialization;

public sealed class Subscription
{
    [JsonInclude]
    public Guid Id { get; private set; }

    [JsonInclude]
    public long UserId { get; private set; }

    [JsonInclude]
    public string LocationName { get; private set; } = string.Empty;

    [JsonInclude]
    public Coordinate Coordinate { get; private set; } = default!;

    [JsonInclude]
    public DateTime CreatedAt { get; private set; }

    [JsonInclude]
    public bool SendDailyWeather { get; private set; }

    [JsonInclude]
    public bool SendEmergencyAlerts { get; private set; }

    [JsonConstructor]
    public Subscription(Guid id, long userId, string locationName, Coordinate coordinate, bool sendDailyWeather, bool sendEmergencyAlerts, DateTime createdAt)
    {
        Id = id;
        UserId = userId;
        LocationName = locationName;
        Coordinate = coordinate;
        SendDailyWeather = sendDailyWeather;
        SendEmergencyAlerts = sendEmergencyAlerts;
        CreatedAt = createdAt;
    }

    public Subscription(long userId, string locationName, Coordinate coordinate, bool sendDailyWeather, bool sendEmergencyAlerts)
        : this(Guid.NewGuid(), userId, locationName, coordinate, sendDailyWeather, sendEmergencyAlerts, DateTime.UtcNow)
    { }

    private Subscription() { }

    public void UpdateSettings(bool sendDailyWeather, bool sendEmergencyAlerts)
    {
        SendDailyWeather = sendDailyWeather;
        SendEmergencyAlerts = sendEmergencyAlerts;
    }
}
