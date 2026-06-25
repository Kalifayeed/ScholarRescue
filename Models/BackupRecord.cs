using System.ComponentModel.DataAnnotations;

namespace ScholarRescue.Models
{
    public class BackupRecord
    {
        public int Id { get; set; }

        [Required, MaxLength(500)]
        public string FileName { get; set; } = string.Empty;

        [MaxLength(2048)]
        public string FilePath { get; set; } = string.Empty;

        public long FileSizeBytes { get; set; }

        [MaxLength(50)]
        public string Status { get; set; } = "Completed"; // Completed, Failed, Verifying, Restoring

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? VerifiedAt { get; set; }

        public bool IsVerified { get; set; }

        public DateTime? LastRestoreTestAt { get; set; }

        public bool RestoreTestPassed { get; set; }

        [MaxLength(1000)]
        public string? Notes { get; set; }

        public bool IsManual { get; set; }
    }
}