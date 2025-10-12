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
        try
        {
            var allUsers = await _userRepository.GetAllUsersAsync();

            foreach (var user in allUsers)
            {
                var dailySubscriptions = user.GetSubscriptionsForDailyWeather();

                foreach (var subscription in dailySubscriptions)
                {
                    try
                    {
                        // Используем сохраненные координаты из подписки
                        var weather = await _weatherService.GetCurrentWeatherAsync(subscription.Coordinate);
                        if (weather != null)
                        {
                            var message = $"🌤️ Daily weather in {subscription.LocationName}:\n" +
                                         $"Temperature: {weather.Temperature}\n" +
                                         $"Condition: {weather.Description}\n" +
                                         $"Wind: {weather.WindSpeed} m/s";

                            await _botClient.SendMessage(user.Id, message);
                        }
                        else
                        {
                            await _botClient.SendMessage(user.Id, 
                                $"❌ Could not get weather data for {subscription.LocationName}");
                        }

                        await Task.Delay(200);
                    }
                    catch (Exception ex)
                    {
                        // Логируем ошибку, но продолжаем для других подписок
                        Console.WriteLine($"Error sending notification to user {user.Id}: {ex.Message}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in daily notifications: {ex.Message}");
        }
    }

    public async Task SendEmergencyAlertAsync(string locationName, string alertMessage)
    {
        try
        {
            var allUsers = await _userRepository.GetAllUsersAsync();

            var affectedUsers = allUsers
                .Where(u => u.Subscriptions.Any(s =>
                    s.LocationName.Equals(locationName, StringComparison.OrdinalIgnoreCase) &&
                    s.SendEmergencyAlerts))
                .ToList();

            foreach (var user in affectedUsers)
            {
                await _botClient.SendMessage(user.Id, $"🚨 EMERGENCY for {locationName}:\n{alertMessage}");
                await Task.Delay(200);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error sending emergency alerts: {ex.Message}");
        }
    }

    public async Task SendMessageAsync(long chatId, string message)
    {
        await _botClient.SendMessage(chatId, message);
    }

    public async Task SendWeatherAsync(long chatId, string locationName)
    {
        try
        {
            var weather = await _weatherService.GetCurrentWeatherAsync(locationName);

            if (weather == null)
            {
                await SendMessageAsync(chatId, $"❌ Could not get weather data for '{locationName}'. Please check the location name and try again.");
                return;
            }

            var message = $"🌤️ Weather in {locationName}:\n" +
                         $"Temperature: {weather.Temperature}\n" +
                         $"Condition: {weather.Description}\n" +
                         $"Wind: {weather.WindSpeed} m/s\n" +
                         $"Updated: {weather.Timestamp:HH:mm} UTC";

            await SendMessageAsync(chatId, message);
        }
        catch (Exception ex)
        {
            await SendMessageAsync(chatId, $"❌ Error getting weather information: {ex.Message}");
        }
    }
}