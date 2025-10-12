namespace WeatherBot.Entities.ValueObjects;

public sealed record Coordinate
{
    public double Latitude { get; }

    public double Longitude { get; }

    public Coordinate(double latitude, double longitude)
    {
        if (latitude < -90 || latitude > 90)
            throw new ArgumentException("Latiidute must be between -90 and 90");

        if (longitude < -180 || longitude > 180)
            throw new ArgumentException("Longitude must be between -180 and 180");

        Latitude = latitude;
        Longitude = longitude;

    }
}