using System.IO
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using WeatherBot.Interfaces.Repositories;
using WeatherBot.Interfaces.Services;
using WeatherBot.Repositories;
using WeatherBot.Services;
using WeatherBot.Telegram;
using WeatherBot.Telegram.Handlers;


namespace WeatherBot;

public class WeatherBotApplication
{
    private readonly IHost _host;

    public WeatherBotApplication(string botToken)
    {
        _host = Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                // Telegram
                services.AddSingleton<ITelegramBotClient>(new TelegramBotClient(botToken));
                services.AddSingleton<Bot>();
                services.AddScoped<CommandHandler>();
                
                // Services - ВАЖНО: HttpClient должен быть зарегистрирован
                services.AddHttpClient(); // Эта строка обязательна
                services.AddScoped<SimpleNotificationService>();
                services.AddScoped<SubscriptionService>();
                services.AddScoped<IWeatherService, WeatherService>();
                services.AddScoped<ILocationService, LocationService>();
                
                // Repositories
                services.AddScoped<IUserRepository, UserRepository>();

                // Logging
                services.AddLogging(builder => 
                    builder.AddConsole().SetMinimumLevel(LogLevel.Information));
            })
            .Build();
    }

    public async Task RunAsync()
    {
        var bot = _host.Services.GetRequiredService<Bot>();
        var botClient = _host.Services.GetRequiredService<ITelegramBotClient>();

        // Исправляем вызов StartReceiving - убираем pollingErrorHandler
        botClient.StartReceiving(
            updateHandler: async (client, update, token) => await bot.HandleUpdateAsync(update),
            errorHandler: (client, exception, token) => 
            {
                var logger = _host.Services.GetRequiredService<ILogger<WeatherBotApplication>>();
                logger.LogError(exception, "Telegram bot error");
                return Task.CompletedTask;
            }
        );

        Console.WriteLine("Bot started!");
        await _host.RunAsync();
    }
}

// Статический класс для простого запуска
public static class BotRunner
{
    public static async Task Run(string botToken)
    {
        var app = new WeatherBotApplication(botToken);
        await app.RunAsync();
    }
}

public static class Program
{
    public static async Task Main(string[] args)
    {
        // ⭐⭐⭐ ЗАМЕНИ ЭТУ СТРОКУ НА СВОЙ ТОКЕН БОТА ⭐⭐⭐
        var tokenPath = Path.Combine("WeatherBot", "token.txt");
    string botToken;

    try
    {
        botToken = File.ReadAllText(tokenPath).Trim();
    }
    catch
    {
        Console.WriteLine($"❌ Could not read bot token from {tokenPath}!");
        return;
    }
    if (string.IsNullOrWhiteSpace(botToken))
    {
        Console.WriteLine("❌ Bot token is missing or empty in token.txt.");
        return;
    }

    Console.WriteLine("🤖 Starting Weather Bot...");
    await BotRunner.Run(botToken);
    }
}
