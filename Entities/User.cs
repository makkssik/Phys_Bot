using WeatherBot.Entities.ValueObjects;

namespace WeatherBot.Entities;

public sealed class User
{
    private readonly List<Subscription> _subscriptions = new();

    public long Id { get; }

    public string Username { get; }

    public IReadOnlyList<Subscription> Subscriptions => _subscriptions;

    public User(long id, string username)
    {
        Id = id;
        Username = username;
    }

    // Исправленный метод - теперь принимает Coordinate
    public Subscription AddSubscription(string locationName, Coordinate coordinate, bool sendDailyWeather, bool sendEmergencyAlerts)
    {
        if (_subscriptions.Any(s => s.LocationName.Equals(locationName, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException($"Already subscribed to {locationName}");

        var subscription = new Subscription(Id, locationName, coordinate, sendDailyWeather, sendEmergencyAlerts);
        _subscriptions.Add(subscription);
        return subscription;
    }

    public bool RemoveSubscription(string locationName)
    {
        var subscription = _subscriptions.FirstOrDefault(s => 
            s.LocationName.Equals(locationName, StringComparison.OrdinalIgnoreCase));
        
        return subscription != null && _subscriptions.Remove(subscription);
    }

    public List<Subscription> GetSubscriptionsForDailyWeather()
        => _subscriptions.Where(s => s.SendDailyWeather).ToList();

    public List<Subscription> GetSubscriptionsForEmergencyAlerts()
        => _subscriptions.Where(s => s.SendEmergencyAlerts).ToList();
}