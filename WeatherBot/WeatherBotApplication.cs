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
                
                services.AddScoped<IUserRepository, SqliteUserRepository>();

                services.AddHostedService<AlertWorker>();

                services.AddLogging(builder => 
                    builder.AddConsole().SetMinimumLevel(LogLevel.Information));
            })
            .Build();
    }

    public async Task RunAsync()
    {
        var bot = _host.Services.GetRequiredService<Bot>();
        var botClient = _host.Services.GetRequiredService<ITelegramBotClient>();

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

    private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(response => (int)response.StatusCode == 429)
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: attempt =>
                    TimeSpan.FromSeconds(Math.Pow(2, attempt)) +
                    TimeSpan.FromMilliseconds(Random.Shared.Next(0, 200)));
    }

    private static IAsyncPolicy<HttpResponseMessage> GetTimeoutPolicy()
    {
        return Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(10));
    }
}

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
        var tokenPath = "token.txt";
        string botToken;

        try
        {
            if (!File.Exists(tokenPath) && File.Exists("token.txt"))
            {
                tokenPath = "token.txt";
            }
            
            botToken = await File.ReadAllTextAsync(tokenPath);
            botToken = botToken.Trim();
        }
        catch
        {
            Console.WriteLine($"❌ Could not read bot token from {tokenPath}!");
            return;
        }

        if (string.IsNullOrWhiteSpace(botToken))
        {
            Console.WriteLine("❌ Bot token is missing or empty.");
            return;
        }

        Console.WriteLine("🤖 Starting Weather Bot...");
        await BotRunner.Run(botToken);
    }
}