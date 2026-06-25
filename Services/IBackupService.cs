using ScholarRescue.Models;

namespace ScholarRescue.Services
{
    public interface IBackupService
    {
        Task<BackupRecord> CreateBackupAsync(bool isManual = false);
        Task<bool> VerifyBackupAsync(int backupId);
        Task<bool> RestoreFromBackupAsync(int backupId);
        Task<List<BackupRecord>> GetRecentBackupsAsync(int count = 20);
        Task<BackupRecord?> GetLatestBackupAsync();
        Task<int> GetTotalBackupCountAsync();
        Task<DateTime?> GetLastSuccessfulBackupAsync();
        Task<DateTime?> GetLastRestoreTestAsync();
        Task DeleteOldBackupsAsync(int keepCount = 30);
    }
}