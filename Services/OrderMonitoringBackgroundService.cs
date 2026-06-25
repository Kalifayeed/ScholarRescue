namespace ScholarRescue.Services
{
    /// <summary>
    /// Background service that periodically runs the Order Monitoring Engine checks.
    /// Runs every 5 minutes.
    /// </summary>
    public class OrderMonitoringBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<OrderMonitoringBackgroundService> _logger;

        public OrderMonitoringBackgroundService(
            IServiceProvider serviceProvider,
            ILogger<OrderMonitoringBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Order Monitoring Background Service started.");

            // Initial delay to let the app start up
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var monitoringService = scope.ServiceProvider
                        .GetRequiredService<IOrderMonitoringService>();

                    await monitoringService.RunMonitoringCheckAsync();
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error running order monitoring check.");
                }

                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }

            _logger.LogInformation("Order Monitoring Background Service stopped.");
        }
    }
}