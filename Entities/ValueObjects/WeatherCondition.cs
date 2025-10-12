namespace WeatherBot.Entities.ValueObjects;

public sealed record WeatherCondition
{
    public string Description { get; }

    public string Code { get; }

    public WeatherCondition(string code, string description)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Weather code cannot be empty");

        Code = code;
        Description = description;
    }
}