using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using WeatherBot.Interfaces.Repositories;
using WeatherBot.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace WeatherBot.Services;

public sealed class SimpleNotificationService
{
    private readonly IUserRepository _userRepository;
    private readonly IWeatherService _weatherService;
    private readonly ITelegramBotClient _botClient;
    private readonly ILogger<SimpleNotificationService> _logger;

    public SimpleNotificationService(
        IUserRepository userRepository,
        IWeatherService weatherService,
        ITelegramBotClient botClient,
        ILogger<SimpleNotificationService> logger)
    {
        _userRepository = userRepository;
        _weatherService = weatherService;
        _botClient = botClient;
        _logger = logger;
    }

    public async Task SendDailyNotificationsAsync()
    {
        var allUsers = await _userRepository.GetAllUsersAsync();

        foreach (var user in allUsers)
        {
            var subs = user.GetSubscriptionsForDailyWeather();
            foreach (var subscription in subs)
            {
                try 
                {
                    var weather = await _weatherService.GetCurrentWeatherAsync(subscription.Coordinate);
                    if (weather != null)
                    {
                        var message = $"📅 Daily Forecast for {subscription.LocationName}:\n" +
                                     $"{weather.Description}, {weather.Temperature}\n" +
                                     $"Wind: {weather.WindSpeed} m/s";
                        
                        if (int.TryParse(weather.Condition.Code, out int code))
                        {
                            string hobbiesToSend = user.Hobbies;
                            if (user.IsMotorist) hobbiesToSend += ",auto";
                            
                            var mlAdvice = await _weatherService.GetClothingRecommendationAsync(
                                (double)weather.Temperature.Value, 
                                weather.WindSpeed, 
                                code,
                                hobbiesToSend 
                            );
                            
                            if (!string.IsNullOrWhiteSpace(mlAdvice))
                                message += $"\n\n💡 {mlAdvice}";
                        }

                        await SendMessageAsync(user.Id, message, ParseMode.Html);
                        await Task.Delay(200); 
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing daily weather for user {UserId}", user.Id);
                }
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
                try
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
                                message += $"\n\n👮 Instruction: {alert.Instruction}";

                            await SendMessageAsync(user.Id, message);
                            await Task.Delay(100); 
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing alert for user {UserId}", user.Id);
                }
            }
        }
    }
    
    public async Task SendWeatherAsync(long chatId, string locationName)
    {
        try 
        {
            var user = await _userRepository.GetUserAsync(chatId);
            var weather = await _weatherService.GetCurrentWeatherAsync(locationName);
            
            if (weather == null)
            {
                await SendMessageAsync(chatId, "❌ Weather data not found.");
                return;
            }

            var message = $"Now in {locationName}:\n{weather.Description}, {weather.Temperature}\nWind: {weather.WindSpeed} m/s";
            
            if (int.TryParse(weather.Condition.Code, out int code))
            {
                string hobbiesToSend = user.Hobbies;
                if (user.IsMotorist) 
                {
                    if (!string.IsNullOrEmpty(hobbiesToSend)) hobbiesToSend += ",";
                    hobbiesToSend += "auto";
                }
                
                var mlAdvice = await _weatherService.GetClothingRecommendationAsync(
                    (double)weather.Temperature.Value, 
                    weather.WindSpeed, 
                    code,
                    hobbiesToSend
                );
                
                if (!string.IsNullOrWhiteSpace(mlAdvice))
                    message += $"\n\n{mlAdvice}";
            }

            await SendMessageAsync(chatId, message, ParseMode.Html);
        }
        catch (Exception ex)
        {
             _logger.LogError(ex, "Error sending current weather");
             await SendMessageAsync(chatId, "❌ Error retrieving weather data.");
        }
    }

    public async Task SendTestAlertAsync()
    {
        var allUsers = await _userRepository.GetAllUsersAsync();
        foreach (var user in allUsers)
        {
            if (user.GetSubscriptionsForEmergencyAlerts().Any())
            {
                await SendMessageAsync(user.Id, "🧪 <b>TEST EMERGENCY ALERT</b>", ParseMode.Html);
                await Task.Delay(100);
            }
        }
    }

    private async Task SendMessageAsync(long chatId, string message, ParseMode? parseMode = null)
    {
        try
        {
            await _botClient.SendMessage(chatId, message, parseMode: parseMode ?? ParseMode.None);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to send message to {chatId}: {ex.Message}");
        }
    }
}