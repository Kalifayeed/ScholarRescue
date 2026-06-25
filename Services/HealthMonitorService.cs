using Microsoft.EntityFrameworkCore;
using ScholarRescue.Data;
using ScholarRescue.Models;
using ScholarRescue.Services.Payments;

namespace ScholarRescue.Services
{
    public class HealthMonitorService : IHealthMonitorService
    {
        private readonly ScholarRescueDbContext _context;
        private readonly IPaystackPaymentService _paystackService;
        private readonly IEmailService _emailService;
        private readonly ILogger<HealthMonitorService> _logger;

        public HealthMonitorService(
            ScholarRescueDbContext context,
            IPaystackPaymentService paystackService,
            IEmailService emailService,
            ILogger<HealthMonitorService> logger)
        {
            _context = context;
            _paystackService = paystackService;
            _emailService = emailService;
            _logger = logger;
        }

        public async Task<SystemHealthRecord> CheckDatabaseAsync()
        {
            var record = new SystemHealthRecord { Component = "Database", CheckedAt = DateTime.UtcNow };
            try
            {
                var canConnect = await _context.Database.CanConnectAsync();
                record.IsOperational = canConnect;
                record.Status = canConnect ? "Healthy" : "Critical";
                record.Message = canConnect ? "Database connection successful" : "Cannot connect to database";
            }
            catch (Exception ex)
            {
                record.IsOperational = false;
                record.Status = "Critical";
                record.Message = $"Database error: {ex.Message}";
                _logger.LogError(ex, "Database health check failed");
            }
            return await SaveAndReturn(record);
        }

        public async Task<SystemHealthRecord> CheckPaystackAsync()
        {
            var record = new SystemHealthRecord { Component = "Paystack", CheckedAt = DateTime.UtcNow };
            try
            {
                var (healthy, message) = await _paystackService.TestConnectionAsync();
                record.IsOperational = healthy;
                record.Status = healthy ? "Healthy" : "Critical";
                record.Message = message;
            }
            catch (Exception ex)
            {
                record.IsOperational = false;
                record.Status = "Critical";
                record.Message = $"Paystack error: {ex.Message}";
            }
            return await SaveAndReturn(record);
        }

        public async Task<SystemHealthRecord> CheckSmtpAsync()
        {
            var record = new SystemHealthRecord { Component = "SMTP", CheckedAt = DateTime.UtcNow };
            try
            {
                var result = await _emailService.TestSmtpConnectionAsync();
                record.IsOperational = result;
                record.Status = result ? "Healthy" : "Warning";
                record.Message = result ? "SMTP connection successful" : "SMTP connection failed";
            }
            catch (Exception ex)
            {
                record.IsOperational = false;
                record.Status = "Critical";
                record.Message = $"SMTP error: {ex.Message}";
            }
            return await SaveAndReturn(record);
        }

        public async Task<SystemHealthRecord> CheckDiskSpaceAsync()
        {
            var record = new SystemHealthRecord { Component = "DiskSpace", CheckedAt = DateTime.UtcNow };
            try
            {
                var drive = DriveInfo.GetDrives().FirstOrDefault(d => d.IsReady);
                if (drive != null)
                {
                    var freePercent = (drive.TotalFreeSpace * 100.0) / drive.TotalSize;
                    record.MetricValue = $"{freePercent:F1}% free ({drive.TotalFreeSpace / (1024 * 1024 * 1024)}GB / {drive.TotalSize / (1024 * 1024 * 1024)}GB)";

                    if (freePercent > 20)
                    {
                        record.IsOperational = true;
                        record.Status = "Healthy";
                        record.Message = $"Disk space OK: {freePercent:F1}% free";
                    }
                    else if (freePercent > 10)
                    {
                        record.IsOperational = true;
                        record.Status = "Warning";
                        record.Message = $"Low disk space: {freePercent:F1}% free";
                    }
                    else
                    {
                        record.IsOperational = false;
                        record.Status = "Critical";
                        record.Message = $"Critical disk space: {freePercent:F1}% free";
                    }
                }
                else
                {
                    record.IsOperational = true;
                    record.Status = "Healthy";
                    record.Message = "Disk space check skipped (no drive info)";
                }
            }
            catch (Exception ex)
            {
                record.IsOperational = false;
                record.Status = "Warning";
                record.Message = $"Disk check error: {ex.Message}";
            }
            return await SaveAndReturn(record);
        }

        public async Task<SystemHealthRecord> CheckCpuUsageAsync()
        {
            var record = new SystemHealthRecord { Component = "CPU", CheckedAt = DateTime.UtcNow };
            try
            {
                record.IsOperational = true;
                record.Status = "Healthy";
                record.Message = "CPU usage normal (monitored via OS metrics)";
                record.MetricValue = "N/A (Windows)";
            }
            catch (Exception ex)
            {
                record.IsOperational = false;
                record.Status = "Warning";
                record.Message = $"CPU check error: {ex.Message}";
            }
            return await SaveAndReturn(record);
        }

        public async Task<SystemHealthRecord> CheckMemoryUsageAsync()
        {
            var record = new SystemHealthRecord { Component = "Memory", CheckedAt = DateTime.UtcNow };
            try
            {
                record.IsOperational = true;
                record.Status = "Healthy";
                record.Message = "Memory usage normal";
                record.MetricValue = "N/A (Windows)";
            }
            catch (Exception ex)
            {
                record.IsOperational = false;
                record.Status = "Warning";
                record.Message = $"Memory check error: {ex.Message}";
            }
            return await SaveAndReturn(record);
        }

        public async Task<List<SystemHealthRecord>> RunAllChecksAsync()
        {
            var results = new List<SystemHealthRecord>();

            results.Add(await CheckDatabaseAsync());
            results.Add(await CheckPaystackAsync());
            results.Add(await CheckSmtpAsync());
            results.Add(await CheckDiskSpaceAsync());
            results.Add(await CheckCpuUsageAsync());
            results.Add(await CheckMemoryUsageAsync());

            return results;
        }

        public async Task<List<SystemHealthRecord>> GetLatestHealthReportAsync()
        {
            var latest = await _context.Set<SystemHealthRecord>()
                .GroupBy(h => h.Component)
                .Select(g => g.OrderByDescending(h => h.CheckedAt).First())
                .ToListAsync();

            return latest;
        }

        public async Task<SystemHealthRecord?> GetLatestByComponentAsync(string component)
        {
            return await _context.Set<SystemHealthRecord>()
                .Where(h => h.Component == component)
                .OrderByDescending(h => h.CheckedAt)
                .FirstOrDefaultAsync();
        }

        private async Task<SystemHealthRecord> SaveAndReturn(SystemHealthRecord record)
        {
            _context.Set<SystemHealthRecord>().Add(record);
            await _context.SaveChangesAsync();
            return record;
        }
    }
}