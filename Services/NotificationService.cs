using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Exceptions;
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
        var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = 5 };

        await Parallel.ForEachAsync(allUsers, parallelOptions, async (user, token) =>
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
                        
                        await SendMessageAsync(user.Id, message);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing daily weather for user {UserId}", user.Id);
                }
            }
        });
    }

    public async Task CheckAndSendAlertsAsync()
    {
        var allUsers = await _userRepository.GetAllUsersAsync();
        var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = 5 };

        await Parallel.ForEachAsync(allUsers, parallelOptions, async (user, token) =>
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
                            {
                                message += $"\n\n👮 Instruction: {alert.Instruction}";
                            }

                            await SendMessageAsync(user.Id, message);
                            
                            await Task.Delay(100, token); 
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing alert for user {UserId}", user.Id);
                }
            }
        });
    }
    
    public async Task SendTestAlertAsync()
    {
        var allUsers = await _userRepository.GetAllUsersAsync();
        int sentCount = 0;

        foreach (var user in allUsers)
        {
            if (user.GetSubscriptionsForEmergencyAlerts().Any())
            {
                var message = "🧪 <b>TEST EMERGENCY ALERT</b> 🧪\n\n" +
                              "This is a test of the notification system.\n" +
                              "If you see this, your emergency alerts are configured correctly.";

                await SendMessageAsync(user.Id, message, ParseMode.Html);
                sentCount++;
            }
        }
        Console.WriteLine($"✅ Test alerts sent to {sentCount} users.");
    }

    public async Task SendWeatherAsync(long chatId, string locationName)
    {
        try 
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
        catch (Exception ex)
        {
             _logger.LogError(ex, "Error sending current weather");
             await SendMessageAsync(chatId, "❌ Error retrieving weather data.");
        }
    }

    private async Task SendMessageAsync(long chatId, string message, ParseMode? parseMode = null)
    {
        try
        {
            await _botClient.SendMessage(chatId, message, parseMode: parseMode ?? ParseMode.None);
        }
        catch (ApiRequestException ex) when (ex.ErrorCode == 403)
        {
            _logger.LogWarning($"User {chatId} has blocked the bot.");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to send message to {chatId}: {ex.Message}");
        }
    }
}