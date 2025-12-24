namespace WeatherBot.Entities.ValueObjects;

public sealed record WeatherData(
    Temperature Temperature,
    WeatherCondition Condition,
    double WindSpeed,
    DateTime Time
)
{
    public string Description => Condition.Description;
}