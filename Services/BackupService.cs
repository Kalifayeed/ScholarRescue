using Microsoft.EntityFrameworkCore;
using ScholarRescue.Data;
using ScholarRescue.Models;

namespace ScholarRescue.Services
{
    public class BackupService : IBackupService
    {
        private readonly ScholarRescueDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<BackupService> _logger;
        private readonly string _backupDirectory;

        public BackupService(
            ScholarRescueDbContext context,
            IConfiguration configuration,
            ILogger<BackupService> logger)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
            _backupDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Backups");
            if (!Directory.Exists(_backupDirectory))
                Directory.CreateDirectory(_backupDirectory);
        }

        public async Task<BackupRecord> CreateBackupAsync(bool isManual = false)
        {
            var backup = new BackupRecord
            {
                FileName = $"scholarrescue_backup_{DateTime.UtcNow:yyyyMMdd_HHmmss}.sql",
                Status = "InProgress",
                CreatedAt = DateTime.UtcNow,
                IsManual = isManual
            };

            _context.Set<BackupRecord>().Add(backup);
            await _context.SaveChangesAsync();

            try
            {
                var connectionString = _configuration.GetConnectionString("DefaultConnection");
                var backupPath = Path.Combine(_backupDirectory, backup.FileName);
                backup.FilePath = backupPath;

                var process = new System.Diagnostics.Process();
                process.StartInfo.FileName = "pg_dump";
                process.StartInfo.Arguments = $"--dbname=\"{connectionString}\" --format=custom --file=\"{backupPath}\"";
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.UseShellExecute = false;
                process.Start();

                var output = await process.StandardOutput.ReadToEndAsync();
                var error = await process.StandardError.ReadToEndAsync();
                process.WaitForExit();

                if (process.ExitCode == 0)
                {
                    var fileInfo = new FileInfo(backupPath);
                    backup.FileSizeBytes = fileInfo.Length;
                    backup.Status = "Completed";
                    backup.Notes = "Backup created successfully";
                }
                else
                {
                    backup.Status = "Failed";
                    backup.Notes = $"pg_dump error: {error}";
                    _logger.LogError("Backup failed: {Error}", error);
                }
            }
            catch (Exception ex)
            {
                backup.Status = "Failed";
                backup.Notes = $"Backup failed: {ex.Message}";
                _logger.LogError(ex, "Backup creation failed");
            }

            await _context.SaveChangesAsync();
            return backup;
        }

        public async Task<bool> VerifyBackupAsync(int backupId)
        {
            var backup = await _context.Set<BackupRecord>().FindAsync(backupId);
            if (backup == null || string.IsNullOrEmpty(backup.FilePath))
                return false;

            try
            {
                var fileInfo = new FileInfo(backup.FilePath);
                if (!fileInfo.Exists || fileInfo.Length == 0)
                {
                    backup.IsVerified = false;
                    backup.Notes = "Backup file missing or empty";
                    await _context.SaveChangesAsync();
                    return false;
                }

                // Verify file integrity using pg_restore --list
                var process = new System.Diagnostics.Process();
                process.StartInfo.FileName = "pg_restore";
                process.StartInfo.Arguments = $"--list \"{backup.FilePath}\"";
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.UseShellExecute = false;
                process.Start();

                var output = await process.StandardOutput.ReadToEndAsync();
                var error = await process.StandardError.ReadToEndAsync();
                process.WaitForExit();

                backup.IsVerified = process.ExitCode == 0 && !string.IsNullOrEmpty(output);
                backup.VerifiedAt = DateTime.UtcNow;
                backup.Notes = backup.IsVerified ? "Backup verified successfully" : $"Verification failed: {error}";
                await _context.SaveChangesAsync();

                return backup.IsVerified;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Backup verification failed for ID {BackupId}", backupId);
                backup.IsVerified = false;
                backup.Notes = $"Verification error: {ex.Message}";
                await _context.SaveChangesAsync();
                return false;
            }
        }

        public async Task<bool> RestoreFromBackupAsync(int backupId)
        {
            var backup = await _context.Set<BackupRecord>().FindAsync(backupId);
            if (backup == null || string.IsNullOrEmpty(backup.FilePath))
                return false;

            try
            {
                var connectionString = _configuration.GetConnectionString("DefaultConnection");
                backup.Status = "Restoring";
                await _context.SaveChangesAsync();

                var process = new System.Diagnostics.Process();
                process.StartInfo.FileName = "pg_restore";
                process.StartInfo.Arguments = $"--dbname=\"{connectionString}\" --clean --if-exists \"{backup.FilePath}\"";
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.UseShellExecute = false;
                process.Start();

                var error = await process.StandardError.ReadToEndAsync();
                process.WaitForExit();

                if (process.ExitCode == 0)
                {
                    backup.Status = "Completed";
                    backup.LastRestoreTestAt = DateTime.UtcNow;
                    backup.RestoreTestPassed = true;
                    backup.Notes = "Restore completed successfully";
                }
                else
                {
                    backup.Status = "Failed";
                    backup.LastRestoreTestAt = DateTime.UtcNow;
                    backup.RestoreTestPassed = false;
                    backup.Notes = $"Restore failed: {error}";
                }

                await _context.SaveChangesAsync();
                return process.ExitCode == 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Restore failed for backup ID {BackupId}", backupId);
                backup.Status = "Failed";
                backup.Notes = $"Restore error: {ex.Message}";
                await _context.SaveChangesAsync();
                return false;
            }
        }

        public async Task<List<BackupRecord>> GetRecentBackupsAsync(int count = 20)
        {
            return await _context.Set<BackupRecord>()
                .OrderByDescending(b => b.CreatedAt)
                .Take(count)
                .ToListAsync();
        }

        public async Task<BackupRecord?> GetLatestBackupAsync()
        {
            return await _context.Set<BackupRecord>()
                .Where(b => b.Status == "Completed")
                .OrderByDescending(b => b.CreatedAt)
                .FirstOrDefaultAsync();
        }

        public async Task<int> GetTotalBackupCountAsync()
        {
            return await _context.Set<BackupRecord>().CountAsync();
        }

        public async Task<DateTime?> GetLastSuccessfulBackupAsync()
        {
            var last = await _context.Set<BackupRecord>()
                .Where(b => b.Status == "Completed")
                .OrderByDescending(b => b.CreatedAt)
                .Select(b => (DateTime?)b.CreatedAt)
                .FirstOrDefaultAsync();
            return last;
        }

        public async Task<DateTime?> GetLastRestoreTestAsync()
        {
            var last = await _context.Set<BackupRecord>()
                .Where(b => b.LastRestoreTestAt != null)
                .OrderByDescending(b => b.LastRestoreTestAt)
                .Select(b => b.LastRestoreTestAt)
                .FirstOrDefaultAsync();
            return last;
        }

        public async Task DeleteOldBackupsAsync(int keepCount = 30)
        {
            var oldBackups = await _context.Set<BackupRecord>()
                .OrderByDescending(b => b.CreatedAt)
                .Skip(keepCount)
                .ToListAsync();

            foreach (var backup in oldBackups)
            {
                if (!string.IsNullOrEmpty(backup.FilePath) && File.Exists(backup.FilePath))
                {
                    try { File.Delete(backup.FilePath); } catch { }
                }
                _context.Set<BackupRecord>().Remove(backup);
            }

            await _context.SaveChangesAsync();
        }
    }
}