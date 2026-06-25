using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ScholarRescue.Data;
using ScholarRescue.Models;
using ScholarRescue.Models.Enums;

namespace ScholarRescue.Services
{
    /// <summary>
    /// Content Moderation & File Scanning Engine implementation.
    /// Manages FileModerationRecords, background scanning, quarantine, admin review, violations, and reporting.
    /// </summary>
    public class ContentModerationService : IContentModerationService
    {
        private readonly IRiskDetectionService _riskDetection;
        private readonly IFileScanningService _fileScanning;
        private readonly ScholarRescueDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<ContentModerationService> _logger;

        public ContentModerationService(
            IRiskDetectionService riskDetection,
            IFileScanningService fileScanning,
            ScholarRescueDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<ContentModerationService> logger)
        {
            _riskDetection = riskDetection;
            _fileScanning = fileScanning;
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<FileModerationRecord> ScanFileAsync(string filePath, string fileName, long fileSizeBytes,
            string uploadedById, string sourceType, string sourceId, int? orderId = null)
        {
            var ext = Path.GetExtension(fileName);

            var record = new FileModerationRecord
            {
                FileId = Guid.NewGuid().ToString(),
                FileName = fileName,
                FilePath = filePath,
                FileExtension = ext,
                FileSizeBytes = fileSizeBytes,
                UploadedById = uploadedById,
                ModerationStatus = ModerationStatus.Pending,
                SourceType = sourceType,
                SourceId = sourceId,
                CreatedAt = DateTime.UtcNow
            };

            _context.FileModerationRecords.Add(record);
            await _context.SaveChangesAsync();

            // Queue for background scan
            await QueueFileForScanAsync(record.Id);

            return record;
        }

        public async Task QueueFileForScanAsync(int fileModerationRecordId)
        {
            var record = await _context.FileModerationRecords.FindAsync(fileModerationRecordId);
            if (record == null) return;

            record.ModerationStatus = ModerationStatus.Scanning;
            await _context.SaveChangesAsync();
        }

        public async Task ProcessScanAsync(int fileModerationRecordId)
        {
            var record = await _context.FileModerationRecords.FindAsync(fileModerationRecordId);
            if (record == null || string.IsNullOrEmpty(record.FilePath)) return;

            record.ModerationStatus = ModerationStatus.Scanning;
            record.ScanDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            var scanResult = await _fileScanning.ScanFileAsync(record.FilePath, record.FileName,
                record.UploadedById, null);

            record.RiskScore = scanResult.RiskScore;
            record.ViolationCount = scanResult.Reasons.Length;
            record.ScanSummary = string.Join("; ", scanResult.Reasons);
            record.RequiresReview = scanResult.IsFlagged;

            if (scanResult.RiskScore >= 75)
            {
                record.ModerationStatus = ModerationStatus.Blocked;
                record.IsQuarantined = true;
            }
            else if (scanResult.RiskScore >= 50)
            {
                record.ModerationStatus = ModerationStatus.Quarantined;
                record.IsQuarantined = true;
                record.RequiresReview = true;
            }
            else if (scanResult.RiskScore >= 25)
            {
                record.ModerationStatus = ModerationStatus.Flagged;
                record.RequiresReview = true;
            }
            else if (scanResult.RiskScore > 0)
            {
                record.ModerationStatus = ModerationStatus.Flagged;
            }
            else
            {
                record.ModerationStatus = ModerationStatus.Approved;
            }

            // Create violation record if flagged
            if (scanResult.IsFlagged)
            {
                foreach (var reason in scanResult.Reasons)
                {
                    _context.ModerationViolations.Add(new ModerationViolation
                    {
                        UserId = record.UploadedById,
                        FileModerationRecordId = record.Id,
                        ViolationType = reason.Length > 100 ? reason[..100] : reason,
                        RiskScore = scanResult.RiskScore,
                        Description = reason,
                        CreatedAt = DateTime.UtcNow
                    });
                }

                // Update risk profile
                await _riskDetection.UpdateRiskProfileAsync(record.UploadedById, scanResult.RiskScore,
                    (await _userManager.GetRolesAsync(await _userManager.FindByIdAsync(record.UploadedById))).FirstOrDefault() ?? "Client");
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("File scan completed: {FileName} - {Status} (Score: {Score})",
                record.FileName, record.ModerationStatus, record.RiskScore);
        }

        public async Task<FileModerationRecord?> GetModerationRecordAsync(int recordId)
        {
            return await _context.FileModerationRecords
                .Include(r => r.UploadedBy)
                .FirstOrDefaultAsync(r => r.Id == recordId);
        }

        public async Task<List<FileModerationRecord>> GetSourceRecordsAsync(string sourceType, string sourceId)
        {
            return await _context.FileModerationRecords
                .Where(r => r.SourceType == sourceType && r.SourceId == sourceId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task<FileModerationRecord> ApproveFileAsync(int recordId, string adminId, string? notes = null)
        {
            var record = await _context.FileModerationRecords.FindAsync(recordId)
                ?? throw new InvalidOperationException("Record not found");

            record.ModerationStatus = ModerationStatus.Approved;
            record.ReviewedById = adminId;
            record.ReviewedAt = DateTime.UtcNow;
            record.ReviewNotes = notes;
            record.IsQuarantined = false;

            _context.AuditLogs.Add(new AuditLog
            {
                Action = "File Approved",
                PerformedById = adminId,
                Description = $"File '{record.FileName}' approved after review",
                CreatedDate = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
            return record;
        }

        public async Task<FileModerationRecord> RejectFileAsync(int recordId, string adminId, string reason)
        {
            var record = await _context.FileModerationRecords.FindAsync(recordId)
                ?? throw new InvalidOperationException("Record not found");

            record.ModerationStatus = ModerationStatus.Blocked;
            record.ReviewedById = adminId;
            record.ReviewedAt = DateTime.UtcNow;
            record.ReviewNotes = reason;
            record.IsQuarantined = true;

            _context.AuditLogs.Add(new AuditLog
            {
                Action = "File Rejected",
                PerformedById = adminId,
                Description = $"File '{record.FileName}' rejected: {reason}",
                CreatedDate = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
            return record;
        }

        public async Task DeleteFileAsync(int recordId, string adminId)
        {
            var record = await _context.FileModerationRecords.FindAsync(recordId);
            if (record == null) return;

            // Delete physical file
            if (!string.IsNullOrEmpty(record.FilePath) && File.Exists(record.FilePath))
            {
                File.Delete(record.FilePath);
            }

            _context.FileModerationRecords.Remove(record);

            _context.AuditLogs.Add(new AuditLog
            {
                Action = "File Deleted",
                PerformedById = adminId,
                Description = $"File '{record.FileName}' deleted by admin review",
                CreatedDate = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
        }

        public async Task<ModerationDashboardStats> GetDashboardStatsAsync()
        {
            var stats = new ModerationDashboardStats();
            stats.PendingReviewCount = await _context.FileModerationRecords
                .CountAsync(r => r.ModerationStatus == ModerationStatus.Flagged || r.ModerationStatus == ModerationStatus.Quarantined);
            stats.FlaggedCount = await _context.FileModerationRecords.CountAsync(r => r.ModerationStatus == ModerationStatus.Flagged);
            stats.BlockedCount = await _context.FileModerationRecords.CountAsync(r => r.ModerationStatus == ModerationStatus.Blocked);
            stats.QuarantinedCount = await _context.FileModerationRecords.CountAsync(r => r.ModerationStatus == ModerationStatus.Quarantined);
            stats.TotalScanned = await _context.FileModerationRecords.CountAsync();
            stats.HighRiskUserCount = await _context.WriterRiskProfiles.CountAsync(p => p.RiskLevel == RiskLevel.High || p.RiskLevel == RiskLevel.Critical);

            var topSources = await _context.FileModerationRecords
                .GroupBy(r => r.SourceType ?? "Unknown")
                .Select(g => new { SourceType = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(5)
                .ToListAsync();
            stats.TopSourceTypes = topSources.Select(x => (x.SourceType, x.Count)).ToList();

            return stats;
        }

        public async Task<(List<FileModerationRecord> Records, int TotalCount)> GetRecordsAsync(
            int page = 1, int pageSize = 25, ModerationStatus? status = null,
            string? search = null, string? sourceType = null)
        {
            IQueryable<FileModerationRecord> query = _context.FileModerationRecords
                .Include(r => r.UploadedBy);

            if (status.HasValue)
                query = query.Where(r => r.ModerationStatus == status.Value);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLowerInvariant();
                query = query.Where(r => r.FileName.ToLower().Contains(term) || r.FileId.Contains(term));
            }

            if (!string.IsNullOrWhiteSpace(sourceType))
                query = query.Where(r => r.SourceType == sourceType);

            var totalCount = await query.CountAsync();

            var records = await query
                .OrderByDescending(r => r.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (records, totalCount);
        }

        public async Task<List<ModerationViolation>> GetUserViolationsAsync(string userId)
        {
            return await _context.ModerationViolations
                .Include(v => v.FileModerationRecord)
                .Where(v => v.UserId == userId)
                .OrderByDescending(v => v.CreatedAt)
                .ToListAsync();
        }

        public async Task<ModerationReport> GetReportAsync()
        {
            var report = new ModerationReport();

            report.ViolationsByType = await _context.ModerationViolations
                .GroupBy(v => v.ViolationType)
                .Select(g => new { Type = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .ToDictionaryAsync(x => x.Type, x => x.Count);

            report.BlockedUploads = await _context.FileModerationRecords
                .CountAsync(r => r.ModerationStatus == ModerationStatus.Blocked);

            report.CommissionAvoidanceAttempts = await _context.RiskAssessments
                .CountAsync(r => r.RiskCategory == RiskCategory.CommissionAvoidance);

            var offenders = await _context.ModerationViolations
                .GroupBy(v => v.UserId)
                .Select(g => new { UserId = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(10)
                .ToListAsync();
            report.RepeatOffenders = offenders.Select(x => ((string)x.UserId, x.Count)).ToList();

            report.DailyTrends = await _context.ModerationViolations
                .GroupBy(v => v.CreatedAt.Date.ToString("yyyy-MM-dd"))
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .OrderBy(x => x.Date)
                .Take(30)
                .ToDictionaryAsync(x => x.Date, x => x.Count);

            return report;
        }
    }
}