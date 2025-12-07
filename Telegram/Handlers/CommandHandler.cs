using WeatherBot.Services;
using Telegram.Bot;
using WeatherBot.Interfaces.Repositories;

namespace WeatherBot.Telegram.Handlers;

public partial class CommandHandler
{
    private readonly ITelegramBotClient _botClient;
    private readonly SimpleNotificationService _notificationService;
    private readonly SubscriptionService _subscriptionService;
    private readonly IUserRepository _userRepository;

    public CommandHandler(
        ITelegramBotClient botClient,
        SimpleNotificationService notificationService,
        SubscriptionService subscriptionService,
        IUserRepository userRepository)
    {
        _botClient = botClient;
        _notificationService = notificationService;
        _subscriptionService = subscriptionService;
        _userRepository = userRepository;
    }

    public async Task HandleStartCommand(long userId, string username)
    {
        var user = await _userRepository.GetUserAsync(userId);
        await _userRepository.UpdateUserAsync(user);

        await SendMessage(userId, $"👋 Welcome, {username}!\n\n" +
            "Use:\n" +
            "/weather <city> - get current weather\n" +
            "/subscribe <city> - subscribe to weather updates\n" +
            "/subscriptions - view your subscriptions\n" +
            "/unsubscribe <city> - remove subscription");
    }

    public async Task HandleWeatherCommand(long userId, string location)
    {
        if (string.IsNullOrWhiteSpace(location))
        {
            await SendMessage(userId, "Please specify location: /weather <city>");
            return;
        }

        await _notificationService.SendWeatherAsync(userId, location);
    }

    public async Task HandleSubscribeCommand(long userId, string[] args)
    {
        if (args.Length < 1)
        {
            await SendMessage(userId, "Usage: /subscribe <city> [daily] [emergency]");
            return;
        }

        var locationName = args[0];
        var dailyWeather = args.Contains("daily");
        var emergencyAlerts = args.Contains("emergency");

        if (!dailyWeather && !emergencyAlerts)
        {
            dailyWeather = true;
            emergencyAlerts = true;
        }

        var result = await _subscriptionService.SubscribeAsync(userId, locationName, dailyWeather, emergencyAlerts);
        await SendMessage(userId, result);
    }

    public async Task HandleUnsubscribeCommand(long userId, string locationName)
    {
        if (string.IsNullOrWhiteSpace(locationName))
        {
            await SendMessage(userId, "Usage: /unsubscribe <city>");
            return;
        }

        var result = await _subscriptionService.UnsubscribeAsync(userId, locationName);
        await SendMessage(userId, result);
    }

    public async Task HandleSubscriptionsCommand(long userId)
    {
        var result = await _subscriptionService.ListSubscriptionsAsync(userId);
        await SendMessage(userId, result);
    }

    private async Task SendMessage(long chatId, string message)
    {
        await _botClient.SendMessage(chatId, message);
    }
}
