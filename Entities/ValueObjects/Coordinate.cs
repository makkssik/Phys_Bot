namespace WeatherBot.Entities.ValueObjects;

using System.Text.Json.Serialization;

public sealed record Coordinate
{
    [JsonInclude]
    public double Latitude { get; private set; }

    [JsonInclude]
    public double Longitude { get; private set; }

    [JsonConstructor]
    public Coordinate(double latitude, double longitude)
    {
        if (latitude < -90 || latitude > 90)
            throw new ArgumentException("Latitude must be between -90 and 90");

        if (longitude < -180 || longitude > 180)
            throw new ArgumentException("Longitude must be between -180 and 180");

        Latitude = latitude;
        Longitude = longitude;

    }

    private Coordinate() { }
}