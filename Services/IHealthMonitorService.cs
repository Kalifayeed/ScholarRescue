using ScholarRescue.Models;

namespace ScholarRescue.Services
{
    public interface IHealthMonitorService
    {
        Task<SystemHealthRecord> CheckDatabaseAsync();
        Task<SystemHealthRecord> CheckPaystackAsync();
        Task<SystemHealthRecord> CheckSmtpAsync();
        Task<SystemHealthRecord> CheckDiskSpaceAsync();
        Task<SystemHealthRecord> CheckCpuUsageAsync();
        Task<SystemHealthRecord> CheckMemoryUsageAsync();
        Task<List<SystemHealthRecord>> RunAllChecksAsync();
        Task<List<SystemHealthRecord>> GetLatestHealthReportAsync();
        Task<SystemHealthRecord?> GetLatestByComponentAsync(string component);
    }
}