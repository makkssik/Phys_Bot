namespace WeatherBot.Entities.ValueObjects;

public sealed record Username
{
    public string Value { get; }

    public Username(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Username cannot be empty");

        if (value.Length < 3 || value.Length > 32)
            throw new ArgumentException("Username must be between 3 and 32 characters");

        if (!value.All(c => char.IsLetterOrDigit(c) || c == '_'))
            throw new ArgumentException("Username can only contain letters, digits and underscores");

        Value = value;
    }

    public static implicit operator string(Username username)
        => username.Value;

    public override string ToString()
        => Value;
}