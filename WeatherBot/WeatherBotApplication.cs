using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
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

        botClient.StartReceiving(
            updateHandler: async (client, update, token) => await bot.HandleUpdateAsync(update),
            errorHandler: (client, exception, token) => 
            {
                var logger = _host.Services.GetRequiredService<ILogger<WeatherBotApplication>>();
                logger.LogError(exception, "Telegram bot error");
                return Task.CompletedTask;
            }
        );

        Console.WriteLine("Bot started! Press Ctrl+C to stop.");
        
        await _host.RunAsync();
    }

    private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy() =>
        HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

    private static IAsyncPolicy<HttpResponseMessage> GetTimeoutPolicy() =>
        Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(10));
}

public static class Program
{
    public static async Task Main(string[] args)
    {
        var tokenPath = "token.txt";
        string botToken;

        try
        {
            if (!File.Exists(tokenPath))
            {
                if (File.Exists("../token.txt")) tokenPath = "../token.txt";
            }
            
            if (!File.Exists(tokenPath))
            {
                Console.WriteLine($"❌ Could not find {tokenPath}");
                return;
            }

            botToken = await File.ReadAllTextAsync(tokenPath);
            botToken = botToken.Trim();
        }
        catch
        {
            Console.WriteLine($"❌ Error reading bot token from {tokenPath}!");
            return;
        }

        if (string.IsNullOrWhiteSpace(botToken))
        {
            Console.WriteLine("❌ Bot token is missing or empty.");
            return;
        }

        Console.WriteLine("🤖 Starting Weather Bot...");
        var app = new WeatherBotApplication(botToken);
        await app.RunAsync();
    }
}