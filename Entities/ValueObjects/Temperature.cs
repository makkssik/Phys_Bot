namespace WeatherBot.Entities.ValueObjects;

public sealed record Temperature
{
    public decimal Value { get; }

    public string Unit { get; } = "C";

    public Temperature(decimal value)
    {
        Value = value;
    }

    public override string ToString()
        => $"{Value}°{Unit}";
}