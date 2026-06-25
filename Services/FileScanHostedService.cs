namespace ScholarRescue.Services
{
    /// <summary>
    /// Background service that processes pending file scans from the queue.
    /// Runs every 30 seconds to avoid blocking the UI during file uploads.
    /// </summary>
    public class FileScanHostedService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<FileScanHostedService> _logger;

        public FileScanHostedService(IServiceScopeFactory scopeFactory, ILogger<FileScanHostedService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("FileScanHostedService started.");

            // Initial startup delay
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("FileScanHostedService cancelled during startup.");
                return;
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var moderationService = scope.ServiceProvider.GetRequiredService<IContentModerationService>();
                    var context = scope.ServiceProvider.GetRequiredService<Data.ScholarRescueDbContext>();

                    // Get pending scans
                    var pendingRecords = context.FileModerationRecords
                        .Where(r => r.ModerationStatus == Models.Enums.ModerationStatus.Pending
                                 || r.ModerationStatus == Models.Enums.ModerationStatus.Scanning)
                        .OrderBy(r => r.CreatedAt)
                        .Take(10)
                        .ToList();

                    foreach (var record in pendingRecords)
                    {
                        if (stoppingToken.IsCancellationRequested) break;

                        try
                        {
                            await moderationService.ProcessScanAsync(record.Id);
                        }
                        catch (OperationCanceledException)
                        {
                            break;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error processing file scan record {RecordId}", record.Id);
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in FileScanHostedService main loop");
                }

                if (stoppingToken.IsCancellationRequested)
                    break;

                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }

            _logger.LogInformation("FileScanHostedService stopped.");
        }
    }
}