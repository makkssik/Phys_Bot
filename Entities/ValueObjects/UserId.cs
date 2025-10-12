namespace WeatherBot.Entities.ValueObjects;

public sealed record UserId
{
    public long Value { get; }

    public UserId(long value)
    {
        if (value <= 0)
            throw new ArgumentException("UserId must be > 0");

        Value = value;
    }

    public static implicit operator long(UserId userId)
        => userId.Value;

    public override string ToString()
        => Value.ToString();
}