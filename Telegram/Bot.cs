using Telegram.Bot;
using Telegram.Bot.Types;
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
        if (update.Message?.Text == null) return;

        var message = update.Message;
        var text = message.Text;
        var chatId = message.Chat.Id;
        var username = message.From?.Username ?? "User";

        try
        {
            var args = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var command = args[0].ToLower();

            switch (command)
            {
                case "/start":
                    await _commandHandler.HandleStartCommand(chatId, username);
                    break;

                case "/weather":
                    var location = args.Length > 1 ? args[1] : "";
                    await _commandHandler.HandleWeatherCommand(chatId, location);
                    break;

                case "/subscribe":
                    await _commandHandler.HandleSubscribeCommand(chatId, args.Skip(1).ToArray());
                    break;

                case "/unsubscribe":
                    var unsubscribeLocation = args.Length > 1 ? args[1] : "";
                    await _commandHandler.HandleUnsubscribeCommand(chatId, unsubscribeLocation);
                    break;

                case "/subscriptions":
                    await _commandHandler.HandleSubscriptionsCommand(chatId);
                    break;

                default:
                    await _commandHandler.HandleStartCommand(chatId, username);
                    break;
            }
        }
        catch (Exception ex)
        {
            await _botClient.SendMessage(chatId, $"❌ Error: {ex.Message}");
        }
    }
}