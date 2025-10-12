namespace WeatherBot.Entities;

using WeatherBot.Entities.ValueObjects;

public sealed class Subscription
{
    public Guid Id { get; }
    public long UserId { get; }
    public string LocationName { get; }
    public Coordinate Coordinate { get; } // Добавляем координаты
    public DateTime CreatedAt { get; }
    public bool SendDailyWeather { get; private set; }
    public bool SendEmergencyAlerts { get; private set; }

    // Исправленный конструктор - теперь принимает 5 аргументов
    public Subscription(long userId, string locationName, Coordinate coordinate, bool sendDailyWeather, bool sendEmergencyAlerts)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        LocationName = locationName;
        Coordinate = coordinate; // Сохраняем координаты
        SendDailyWeather = sendDailyWeather;
        SendEmergencyAlerts = sendEmergencyAlerts;
        CreatedAt = DateTime.UtcNow;
    }

    public void UpdateSettings(bool sendDailyWeather, bool sendEmergencyAlerts)
    {
        SendDailyWeather = sendDailyWeather;
        SendEmergencyAlerts = sendEmergencyAlerts;
    }
}