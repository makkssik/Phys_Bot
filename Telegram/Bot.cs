using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using WeatherBot.Telegram.Handlers;

namespace WeatherBot.Telegram;

public class Bot
{
    private readonly ITelegramBotClient _botClient;
    private readonly CommandHandler _commandHandler;

    public Bot(ITelegramBotClient botClient, CommandHandler commandHandler)
    {
        _botClient = botClient;
        _commandHandler = commandHandler;
    }

    public async Task HandleUpdateAsync(Update update)
    {
        if (update.Message is not { } message) return;
        if (message.Text is not { } text) return;

        var chatId = message.Chat.Id;
        var username = message.From?.Username ?? "User";

        try
        {
            var parts = text.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            
            var command = parts[0].ToLower();
            var arguments = parts.Length > 1 ? parts[1].Trim() : string.Empty;

            switch (command)
            {
                case "/start":
                    await _commandHandler.HandleStartCommand(chatId, username);
                    break;

                case "/weather":
                    await _commandHandler.HandleWeatherCommand(chatId, arguments);
                    break;

                case "/subscribe":
                    await _commandHandler.HandleSubscribeCommand(chatId, arguments);
                    break;

                case "/unsubscribe":
                    await _commandHandler.HandleUnsubscribeCommand(chatId, arguments);
                    break;

                case "/subscriptions":
                    await _commandHandler.HandleSubscriptionsCommand(chatId);
                    break;

                case "/checkalerts":
                    await _commandHandler.HandleManualAlertCheck(chatId);
                    break;

                case "/togglealert":
                    await _commandHandler.HandleToggleAlerts(chatId, arguments);
                    break;

                default:
                    if (message.Chat.Type == ChatType.Private && !command.StartsWith("/"))
                    {
                         await _commandHandler.HandleWeatherCommand(chatId, text);
                    }
                    break;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error handling update: {ex}");
            await _botClient.SendMessage(chatId, "❌ Произошла ошибка при обработке команды.");
        }
    }
}