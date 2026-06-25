using ScholarRescue.Models;
using ScholarRescue.Models.Enums;

namespace ScholarRescue.Services
{
    /// <summary>
    /// Content Moderation & File Scanning Engine - second line of defense
    /// against commission avoidance, fraud, and policy violations via file uploads.
    /// Scans all uploaded files across all entry points before delivery.
    /// </summary>
    public interface IContentModerationService
    {
        /// <summary>Scans a file and creates/updates a FileModerationRecord. Returns the record.</summary>
        Task<FileModerationRecord> ScanFileAsync(string filePath, string fileName, long fileSizeBytes,
            string uploadedById, string sourceType, string sourceId, int? orderId = null);

        /// <summary>Queue a file for background scanning (non-blocking).</summary>
        Task QueueFileForScanAsync(int fileModerationRecordId);

        /// <summary>Processes pending scans in the background.</summary>
        Task ProcessScanAsync(int fileModerationRecordId);

        /// <summary>Gets moderation status for a file.</summary>
        Task<FileModerationRecord?> GetModerationRecordAsync(int recordId);

        /// <summary>Gets all moderation records for a source.</summary>
        Task<List<FileModerationRecord>> GetSourceRecordsAsync(string sourceType, string sourceId);

        // --- Admin Review ---
        /// <summary>Admin approves a flagged/quarantined file.</summary>
        Task<FileModerationRecord> ApproveFileAsync(int recordId, string adminId, string? notes = null);

        /// <summary>Admin rejects/blocks a file.</summary>
        Task<FileModerationRecord> RejectFileAsync(int recordId, string adminId, string reason);

        /// <summary>Admin deletes a file and its record.</summary>
        Task DeleteFileAsync(int recordId, string adminId);

        // --- Dashboard ---
        /// <summary>Gets moderation dashboard stats.</summary>
        Task<ModerationDashboardStats> GetDashboardStatsAsync();

        /// <summary>Gets paginated moderation records for admin.</summary>
        Task<(List<FileModerationRecord> Records, int TotalCount)> GetRecordsAsync(
            int page = 1, int pageSize = 25, ModerationStatus? status = null,
            string? search = null, string? sourceType = null);

        /// <summary>Gets violation history for a user.</summary>
        Task<List<ModerationViolation>> GetUserViolationsAsync(string userId);

        /// <summary>Gets violation reports/stats.</summary>
        Task<ModerationReport> GetReportAsync();
    }

    public class ModerationDashboardStats
    {
        public int PendingReviewCount { get; set; }
        public int FlaggedCount { get; set; }
        public int BlockedCount { get; set; }
        public int QuarantinedCount { get; set; }
        public int TotalScanned { get; set; }
        public int HighRiskUserCount { get; set; }
        public List<(string SourceType, int Count)> TopSourceTypes { get; set; } = new();
    }

    public class ModerationReport
    {
        public Dictionary<string, int> ViolationsByType { get; set; } = new();
        public List<(string UserName, int Count)> RepeatOffenders { get; set; } = new();
        public int BlockedUploads { get; set; }
        public int CommissionAvoidanceAttempts { get; set; }
        public Dictionary<string, int> DailyTrends { get; set; } = new();
    }
}