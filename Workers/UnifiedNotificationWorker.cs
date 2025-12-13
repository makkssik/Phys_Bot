using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WeatherBot.Services;

namespace WeatherBot.Workers;

public class UnifiedNotificationWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<UnifiedNotificationWorker> _logger;
    
    private readonly TimeSpan _loopInterval = TimeSpan.FromMinutes(10);
    private readonly TimeSpan _dailyTargetTime = new(08, 00, 00); 
    private DateTime _lastDailyRunDate = DateTime.MinValue;

    public UnifiedNotificationWorker(IServiceProvider serviceProvider, ILogger<UnifiedNotificationWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Unified Notification Service starting...");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var notificationService = scope.ServiceProvider.GetRequiredService<SimpleNotificationService>();

                _logger.LogDebug("Running alert check...");
                await notificationService.CheckAndSendAlertsAsync();

                var now = DateTime.UtcNow;
                bool isTimeForDaily = now.TimeOfDay >= _dailyTargetTime 
                                      && now.Date > _lastDailyRunDate.Date;

                if (isTimeForDaily)
                {
                    _logger.LogInformation("Running daily weather distribution...");
                    await notificationService.SendDailyNotificationsAsync();
                    _lastDailyRunDate = now.Date;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in notification loop");
            }

            await Task.Delay(_loopInterval, stoppingToken);
        }
    }
}