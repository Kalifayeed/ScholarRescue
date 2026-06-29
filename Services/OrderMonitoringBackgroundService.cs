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

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Initial delay before first check, then periodic delay
                    await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

                    using var scope = _serviceProvider.CreateScope();
                    var monitoringService = scope.ServiceProvider
                        .GetRequiredService<IOrderMonitoringService>();

                    await monitoringService.RunMonitoringCheckAsync();

                    // Wait 5 minutes between checks
                    await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    // Graceful shutdown - expected during service stop
                    break;
                }
                catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
                {
                    _logger.LogError(ex, "Error running order monitoring check.");
                    // Wait before retrying after error
                    try { await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken); }
                    catch (OperationCanceledException) { break; }
                }
            }

            _logger.LogInformation("Order Monitoring Background Service stopped.");
        }
    }
}