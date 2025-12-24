using WeatherBot.Services;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Types.Enums;
using WeatherBot.Interfaces.Repositories;
using WeatherBot.Entities; 
using Microsoft.Extensions.Configuration;

namespace WeatherBot.Telegram.Handlers;

public partial class CommandHandler
{
    private readonly ITelegramBotClient _botClient;
    private readonly SimpleNotificationService _notificationService;
    private readonly SubscriptionService _subscriptionService;
    private readonly IUserRepository _userRepository;
    private readonly string _siteUrl;

    public CommandHandler(
        ITelegramBotClient botClient,
        SimpleNotificationService notificationService,
        SubscriptionService subscriptionService,
        IUserRepository userRepository,
        IConfiguration configuration)
    {
        _botClient = botClient;
        _notificationService = notificationService;
        _subscriptionService = subscriptionService;
        _userRepository = userRepository;
        _siteUrl = configuration["AppConfig:MlApiUrl"] ?? "";
    }
    
    public async Task HandleStartCommand(long userId, string username)
    {
        var user = await _userRepository.GetUserAsync(userId);
        if (user.Username != username)
        {
             user = new User(user.Id, username); 
             await _userRepository.UpdateUserAsync(user); 
        }

        var message = 
            $"👋 **Привет, {username}!**\n\n" +
            "Я — умный погодный бот с Искусственным Интеллектом 🧠.\n" +
            "Я не просто показываю температуру, я анализирую ветер и влажность, чтобы посоветовать, **что надеть**.\n\n" +
            "🚀 **С чего начать:**\n" +
            "1. Заполни профиль: `/profile 25 yes бег`\n" +
            "   *(возраст, водитель: yes/no, хобби)*\n" +
            "2. Узнай погоду: `/weather London`\n" +
            "3. Подпишись на уведомления: `/subscribe Moscow`\n\n" +
            "👇 Нажми кнопку **Меню** или введи /help для помощи.";

        await SendMessage(userId, message, ParseMode.Markdown);
    }
    
    public async Task HandleHelpCommand(long userId)
    {
        var message = 
            "📖 **Справка по командам:**\n\n" +
            "🌤 **Погода:**\n" +
            "`/weather <город>` — прогноз + совет от AI.\n" +
            "Пример: `/weather Saint Petersburg`\n\n" +
            "👤 **Профиль (Важно для AI):**\n" +
            "`/profile <возраст> <водитель: yes/no> <хобби>`\n" +
            "Пример: `/profile 20 no фото,спорт`\n" +
            "*(хобби перечислять через запятую)*\n\n" +
            "🔔 **Подписки:**\n" +
            "`/subscribe <город>` — подписаться на ежедневный прогноз (8:00).\n" +
            "`/subscribe <город> emergency` — только тревоги МЧС.\n" +
            "`/subscriptions` — список ваших подписок.\n" +
            "`/unsubscribe <город>` — удалить подписку.\n\n" +
            "⚙️ **Настройки:**\n" +
            "`/togglealert <город>` — включить/выключить тревоги для конкретного города.";
        
        await SendMessage(userId, message, ParseMode.Markdown);
    }

    public async Task HandleSetProfileCommand(long userId, string argsString)
    {
        var parts = argsString.Split(' ', 3, StringSplitOptions.RemoveEmptyEntries);
        
        if (parts.Length < 3)
        {
            await SendMessage(userId, "⚠️ Формат: /profile <возраст> <водитель:yes/no> <хобби через запятую>\nПример: /profile 30 yes рыбалка,фото");
            return;
        }

        if (!int.TryParse(parts[0], out int age))
        {
            await SendMessage(userId, "❌ Некорректный возраст.");
            return;
        }
        
        bool isDriver = parts[1].ToLower().StartsWith("y");
        string hobbies = parts[2];

        var user = await _userRepository.GetUserAsync(userId);
        
        user.UpdateProfile(age, "unknown", hobbies, isDriver);
        
        await _userRepository.UpdateUserAsync(user);
        await SendMessage(userId, "✅ Профиль обновлен! Я запомнил ваши интересы и буду давать персональные советы.");
    }

    public async Task HandleWeatherCommand(long userId, string location)
    {
        if (string.IsNullOrWhiteSpace(location))
        {
            await SendMessage(userId, "Please specify location: /weather <city>");
            return;
        }

        await _notificationService.SendWeatherAsync(userId, location);
        
        var url = $"{_siteUrl}/?city={Uri.EscapeDataString(location)}&ngrok-skip-browser-warning=true";
        
        var keyboard = new InlineKeyboardMarkup(
            InlineKeyboardButton.WithUrl("🌐 Открыть на сайте", url)
        );

        await SendMessage(userId, "Хочешь посмотреть подробный прогноз на сайте?", replyMarkup: keyboard);
    }

    public async Task HandleSubscribeCommand(long userId, string argsString)
    {
        if (string.IsNullOrWhiteSpace(argsString))
        {
            await SendMessage(userId, "Usage: /subscribe <city> [daily] [emergency]");
            return;
        }
        
        var parts = argsString.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();
        bool dailyWeather = false;
        bool emergencyAlerts = false;
        
        for (int i = parts.Count - 1; i >= 0; i--)
        {
            var part = parts[i].ToLower();
            if (part == "daily") { dailyWeather = true; parts.RemoveAt(i); }
            else if (part == "emergency") { emergencyAlerts = true; parts.RemoveAt(i); }
        }
        
        if (!dailyWeather && !emergencyAlerts) { dailyWeather = true; emergencyAlerts = true; }
        
        var locationName = string.Join(" ", parts);
        if (string.IsNullOrWhiteSpace(locationName)) { await SendMessage(userId, "❌ Could not parse city name."); return; }

        var result = await _subscriptionService.SubscribeAsync(userId, locationName, dailyWeather, emergencyAlerts);
        await SendMessage(userId, result);
    }

    public async Task HandleUnsubscribeCommand(long userId, string locationName)
    {
        if (string.IsNullOrWhiteSpace(locationName)) { await SendMessage(userId, "Usage: /unsubscribe <city>"); return; }
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
        if (string.IsNullOrWhiteSpace(locationName)) { await SendMessage(userId, "Usage: /togglealert <city>"); return; }
        var user = await _userRepository.GetUserAsync(userId);
        var sub = user.Subscriptions.FirstOrDefault(s => s.LocationName.Equals(locationName, StringComparison.OrdinalIgnoreCase));

        if (sub == null) { await SendMessage(userId, $"❌ Subscription for {locationName} not found."); return; }

        bool newState = !sub.SendEmergencyAlerts;
        sub.UpdateSettings(sub.SendDailyWeather, newState);
        await _userRepository.UpdateUserAsync(user);

        var status = newState ? "ON 🔔" : "OFF 🔕";
        await SendMessage(userId, $"✅ Emergency alerts for {locationName} are now {status}");
    }

    private async Task SendMessage(long chatId, string message, ParseMode? parseMode = null, ReplyMarkup? replyMarkup = null)
    {
        try 
        {
            await _botClient.SendMessage(chatId, message, parseMode: parseMode ?? ParseMode.None, replyMarkup: replyMarkup);
        }
        catch (Exception ex)
        {
             Console.WriteLine($"Error sending message to {chatId}: {ex.Message}");
        }
    }
}