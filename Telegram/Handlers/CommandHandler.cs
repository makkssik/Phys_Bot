using WeatherBot.Services;
using Telegram.Bot;
using WeatherBot.Interfaces.Repositories;
using Telegram.Bot.Types.ReplyMarkups;

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
            "/togglealert <city> - on/off emergency alerts\n" +
            "/checkalerts - manual check (admin)\n" +
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

        var url = $"https://conglobately-unempty-rosio.ngrok-free.dev/?city={Uri.EscapeDataString(location)}";

        var keyboard = new InlineKeyboardMarkup(
            InlineKeyboardButton.WithUrl(
                "🌐 Открыть на сайте",
                url
            )
        );

        await SendMessage(userId, "Хочешь посмотреть подробный прогноз на сайте?", keyboard);
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

    public async Task HandleManualAlertCheck(long userId)
    {
        await SendMessage(userId, "🔄 Triggering alert check...");
        await _notificationService.CheckAndSendAlertsAsync();
        await SendMessage(userId, "✅ Alert check completed.");
    }

    public async Task HandleToggleAlerts(long userId, string locationName)
    {
        if (string.IsNullOrWhiteSpace(locationName))
        {
            await SendMessage(userId, "Usage: /togglealert <city>");
            return;
        }

        var user = await _userRepository.GetUserAsync(userId);
        var sub = user.Subscriptions.FirstOrDefault(s => s.LocationName.Equals(locationName, StringComparison.OrdinalIgnoreCase));

        if (sub == null)
        {
            await SendMessage(userId, $"❌ Subscription for {locationName} not found.");
            return;
        }

        bool newState = !sub.SendEmergencyAlerts;
        sub.UpdateSettings(sub.SendDailyWeather, newState);

        await _userRepository.UpdateUserAsync(user);

        var status = newState ? "ON 🔔" : "OFF 🔕";
        await SendMessage(userId, $"✅ Emergency alerts for {locationName} are now {status}");
    }

    private async Task SendMessage(long chatId, string message, ReplyMarkup? replyMarkup = null)
    {
        if (replyMarkup is null)
        {
            await _botClient.SendMessage(chatId, message);
        }
        else
        {
            await _botClient.SendMessage(chatId, message, replyMarkup: replyMarkup);
        }
    }
}