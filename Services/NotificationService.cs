using Telegram.Bot;
using WeatherBot.Interfaces.Repositories;
using WeatherBot.Interfaces.Services;

namespace WeatherBot.Services;

public sealed class SimpleNotificationService
{
    private readonly IUserRepository _userRepository;
    private readonly IWeatherService _weatherService;
    private readonly ITelegramBotClient _botClient;

    public SimpleNotificationService(
        IUserRepository userRepository,
        IWeatherService weatherService,
        ITelegramBotClient botClient)
    {
        _userRepository = userRepository;
        _weatherService = weatherService;
        _botClient = botClient;
    }

    public async Task SendDailyNotificationsAsync()
    {
        var allUsers = await _userRepository.GetAllUsersAsync();

        foreach (var user in allUsers)
        {
            foreach (var subscription in user.GetSubscriptionsForDailyWeather())
            {
                var weather = await _weatherService.GetCurrentWeatherAsync(subscription.Coordinate);
                if (weather != null)
                {
                    var message = $"📅 Daily Forecast for {subscription.LocationName}:\n" +
                                 $"{weather.Description}, {weather.Temperature}";
                    await SendMessageAsync(user.Id, message);
                }
                await Task.Delay(200); 
            }
        }
    }

    public async Task CheckAndSendAlertsAsync()
    {
        var allUsers = await _userRepository.GetAllUsersAsync();

        foreach (var user in allUsers)
        {
            var alertSubscriptions = user.GetSubscriptionsForEmergencyAlerts();

            foreach (var sub in alertSubscriptions)
            {
                var alerts = await _weatherService.GetAlertsAsync(sub.LocationName);

                if (alerts != null && alerts.Any())
                {
                    foreach (var alert in alerts)
                    {
                        var message = $"🚨 EMERGENCY ALERT: {sub.LocationName} 🚨\n\n" +
                                      $"⚠️ {alert.Headline}\n" +
                                      $"ℹ️ {alert.Event}\n" +
                                      $"📝 {alert.Description}";
                        
                        if (!string.IsNullOrWhiteSpace(alert.Instruction))
                        {
                            message += $"\n\n👮 Instruction: {alert.Instruction}";
                        }

                        await SendMessageAsync(user.Id, message);
                    }
                }
                await Task.Delay(200); 
            }
        }
    }

    public async Task SendWeatherAsync(long chatId, string locationName)
    {
        var weather = await _weatherService.GetCurrentWeatherAsync(locationName);
        if (weather == null)
        {
            await SendMessageAsync(chatId, "❌ Weather data not found.");
            return;
        }

        var message = $"Now in {locationName}:\n{weather.Description}, {weather.Temperature}";
        await SendMessageAsync(chatId, message);
    }

    private async Task SendMessageAsync(long chatId, string message)
    {
        try
        {
            await _botClient.SendMessage(chatId, message);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to send message to {chatId}: {ex.Message}");
        }
    }
}