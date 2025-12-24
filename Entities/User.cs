using WeatherBot.Entities.ValueObjects;
using System.Text.Json.Serialization;

namespace WeatherBot.Entities;

public sealed class User
{
    [JsonInclude]
    public List<Subscription> Subscriptions { get; private set; } = new();

    [JsonInclude]
    public long Id { get; private set; }

    [JsonInclude]
    public string Username { get; private set; } = string.Empty;

    [JsonInclude]
    public bool IsMotorist { get; private set; }

    [JsonInclude]
    public int? Age { get; private set; }

    [JsonInclude]
    public string Gender { get; private set; } = "unknown";

    [JsonInclude]
    public string Hobbies { get; private set; } = "";

    public User(long id, string username)
    {
        Id = id;
        Username = username;
        Subscriptions = new List<Subscription>();
    }

    private User() { }

    public void UpdateProfile(int? age, string gender, string hobbies, bool isMotorist)
    {
        Age = age;
        Gender = gender?.ToLower() ?? "unknown";
        Hobbies = hobbies?.ToLower() ?? "";
        IsMotorist = isMotorist;
    }

    public Subscription AddSubscription(string locationName, Coordinate coordinate, bool sendDailyWeather, bool sendEmergencyAlerts)
    {
        if (Subscriptions.Any(s => s.LocationName.Equals(locationName, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException($"Already subscribed to {locationName}");

        var subscription = new Subscription(Id, locationName, coordinate, sendDailyWeather, sendEmergencyAlerts);
        Subscriptions.Add(subscription);
        return subscription;
    }

    public bool RemoveSubscription(string locationName)
    {
        var subscription = Subscriptions.FirstOrDefault(s => 
            s.LocationName.Equals(locationName, StringComparison.OrdinalIgnoreCase));
        
        return subscription != null && Subscriptions.Remove(subscription);
    }

    public List<Subscription> GetSubscriptionsForDailyWeather()
        => Subscriptions.Where(s => s.SendDailyWeather).ToList();

    public List<Subscription> GetSubscriptionsForEmergencyAlerts()
        => Subscriptions.Where(s => s.SendEmergencyAlerts).ToList();
}
