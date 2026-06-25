using ScholarRescue.Models;

namespace ScholarRescue.ViewModels.Admin
{
    public class SystemCenterViewModel
    {
        public string PlatformVersion { get; set; } = "1.0.0";
        public List<SystemHealthRecord> HealthRecords { get; set; } = new();
        public BackupRecord? LatestBackup { get; set; }
        public int TotalBackups { get; set; }
        public DateTime? LastSuccessfulBackup { get; set; }
        public DateTime? LastRestoreTest { get; set; }
        public int UnresolvedErrors { get; set; }
        public int PendingNotifications { get; set; }
        public int FailedNotifications { get; set; }
        public bool DatabaseReady { get; set; }
        public bool StripeReady { get; set; }
        public bool SmtpReady { get; set; }
    }
}