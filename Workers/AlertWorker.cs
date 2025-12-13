using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WeatherBot.Services;

namespace WeatherBot.Workers;

public class AlertWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AlertWorker> _logger;

    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(10);

    public AlertWorker(IServiceProvider serviceProvider, ILogger<AlertWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("🚨 Alert Monitor Service is starting...");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation("🔍 Checking for emergency alerts...");

                using (var scope = _serviceProvider.CreateScope())
                {
                    var notificationService = scope.ServiceProvider.GetRequiredService<SimpleNotificationService>();
                    
                    await notificationService.CheckAndSendAlertsAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error in Alert Monitor loop");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }
    }
}