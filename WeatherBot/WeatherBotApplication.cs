using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;
using Polly;
using Polly.Extensions.Http;
using WeatherBot.Interfaces.Repositories;
using WeatherBot.Interfaces.Services;
using WeatherBot.Repositories;
using WeatherBot.Services;
using WeatherBot.Telegram;
using WeatherBot.Telegram.Handlers;
using WeatherBot.Workers;

namespace WeatherBot;

public class WeatherBotApplication
{
    private readonly IHost _host;

    public WeatherBotApplication(string botToken)
    {
        _host = Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddMemoryCache();

                services.AddSingleton<ITelegramBotClient>(new TelegramBotClient(botToken));
                services.AddSingleton<Bot>();
                services.AddScoped<CommandHandler>();
                
                var httpClientBuilder = services.AddHttpClient(string.Empty);
                httpClientBuilder
                    .AddPolicyHandler(GetRetryPolicy())
                    .AddPolicyHandler(GetTimeoutPolicy());

                services.AddScoped<SimpleNotificationService>();
                services.AddScoped<SubscriptionService>();
                
                services.AddScoped<IWeatherService, WeatherService>();
                services.AddScoped<ILocationService, LocationService>();
                
                services.AddScoped<SqliteUserRepository>();
                services.AddScoped<IUserRepository>(provider => provider.GetRequiredService<SqliteUserRepository>());

                services.AddHostedService<UnifiedNotificationWorker>();

                services.AddLogging(builder => 
                {
                    builder.AddConsole();
                    builder.SetMinimumLevel(LogLevel.Information);
                });
            })
            .Build();
    }

    public async Task RunAsync()
    {
        using (var scope = _host.Services.CreateScope())
        {
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<WeatherBotApplication>>();
            try 
            {
                logger.LogInformation("Initializing Database...");
                var repo = scope.ServiceProvider.GetRequiredService<SqliteUserRepository>();
                await repo.InitializeDatabaseAsync();
            }
            catch (Exception ex)
            {
                logger.LogCritical(ex, "Critical Error during DB init. Shutting down.");
                return;
            }
        }
        
        var bot = _host.Services.GetRequiredService<Bot>();
        var botClient = _host.Services.GetRequiredService<ITelegramBotClient>();
        
        try 
        {
            await botClient.DeleteWebhook(dropPendingUpdates: true);
        }
        catch 
        {
            try { await botClient.SetWebhook(""); } catch { }
        }
        
        var commands = new[]
        {
            new BotCommand { Command = "start", Description = "Начать работу" },
            new BotCommand { Command = "weather", Description = "Погода (напр. /weather London)" },
            new BotCommand { Command = "subscribe", Description = "Подписаться" },
            new BotCommand { Command = "subscriptions", Description = "Мои подписки" },
            new BotCommand { Command = "togglealert", Description = "Вкл/Выкл тревоги" },
            new BotCommand { Command = "unsubscribe", Description = "Отписаться" },
            new BotCommand { Command = "checkalerts", Description = "Проверить тревоги (Админ)" }
        };

        try 
        {
            await botClient.SetMyCommands(commands);
            Console.WriteLine("✅ Telegram menu commands updated.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ Could not set commands: {ex.Message}");
        }
        
        botClient.StartReceiving(
            updateHandler: async (client, update, token) => await bot.HandleUpdateAsync(update),
            errorHandler: (client, exception, token) => 
            {
                Console.WriteLine($"Telegram Error: {exception.Message}");
                return Task.CompletedTask;
            }
        );
        
        await _host.StartAsync();

        Console.WriteLine("🤖 Weather Bot is RUNNING!");
        Console.WriteLine("------------------------------------------------");
        Console.WriteLine("Console Commands:");
        Console.WriteLine("  test  - Send TEST emergency alert to all subscribers");
        Console.WriteLine("  exit  - Stop the bot");
        Console.WriteLine("------------------------------------------------");
        
        while (true)
        {
            var command = await Task.Run(() => Console.ReadLine());

            if (string.IsNullOrWhiteSpace(command)) continue;

            if (command.Trim().ToLower() == "test")
            {
                Console.WriteLine("🔄 Executing test alert run...");
                using (var scope = _host.Services.CreateScope())
                {
                    var notificationService = scope.ServiceProvider.GetRequiredService<SimpleNotificationService>();
                    await notificationService.SendTestAlertAsync();
                }
            }
            else if (command.Trim().ToLower() == "exit")
            {
                Console.WriteLine("Stopping...");
                break;
            }
            else
            {
                Console.WriteLine($"Unknown command: {command}");
            }
        }
        
        await _host.StopAsync();
    }

    private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy() =>
        HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

    private static IAsyncPolicy<HttpResponseMessage> GetTimeoutPolicy() =>
        Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(10));
}