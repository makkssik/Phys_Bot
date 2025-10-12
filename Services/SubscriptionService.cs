namespace WeatherBot.Services;

using WeatherBot.Interfaces.Repositories;
using WeatherBot.Interfaces.Services;
using WeatherBot.Entities;

public sealed class SubscriptionService
{
    private readonly IUserRepository _userRepository;
    private readonly ILocationService _locationService;

    public SubscriptionService(IUserRepository userRepository, ILocationService locationService)
    {
        _userRepository = userRepository;
        _locationService = locationService;
    }

    public async Task<string> SubscribeAsync(long userId, string locationName, bool dailyWeather, bool emergencyAlerts)
    {
        var coordinate = await _locationService.FindCoordinateAsync(locationName);
        if (coordinate == null)
            return $"❌ Location '{locationName}' not found";

        // Используем GetUserAsync который создаст пользователя если не найден
        var user = await _userRepository.GetUserAsync(userId);

        try
        {
            user.AddSubscription(locationName, coordinate, dailyWeather, emergencyAlerts);
            await _userRepository.UpdateUserAsync(user);

            var message = $"✅ Subscribed to {locationName}";
            if (dailyWeather) message += "\n📅 Daily weather: ON";
            if (emergencyAlerts) message += "\n🚨 Emergency alerts: ON";
            
            return message;
        }
        catch (InvalidOperationException ex)
        {
            return $"❌ {ex.Message}";
        }
    }

    public async Task<string> UnsubscribeAsync(long userId, string locationName)
    {
        var user = await _userRepository.FindUserAsync(userId);
        if (user == null)
            return "❌ User not found";

        var removed = user.RemoveSubscription(locationName);
        if (removed)
        {
            await _userRepository.UpdateUserAsync(user);
            return $"✅ Unsubscribed from {locationName}";
        }

        return $"❌ Subscription to '{locationName}' not found";
    }

    public async Task<string> ListSubscriptionsAsync(long userId)
    {
        var user = await _userRepository.FindUserAsync(userId);
        if (user == null || !user.Subscriptions.Any())
            return "You don't have any subscriptions yet.";

        var message = "Your subscriptions:\n\n";
        foreach (var sub in user.Subscriptions)
        {
            message += $"📍 {sub.LocationName}\n";
            message += sub.SendDailyWeather ? "   📅 Daily weather\n" : "";
            message += sub.SendEmergencyAlerts ? "   🚨 Emergency alerts\n" : "";
            message += "\n";
        }

        return message;
    }
}