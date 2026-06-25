using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ScholarRescue.Models.Enums;

namespace ScholarRescue.Models
{
    /// <summary>
    /// Tracks moderation status and scan results for uploaded files.
    /// </summary>
    public class FileModerationRecord
    {
        [Key]
        public int Id { get; set; }

        /// <summary>Reference to the original file entity ID.</summary>
        [Required]
        [MaxLength(100)]
        public string FileId { get; set; } = string.Empty;

        /// <summary>Original filename.</summary>
        [Required]
        [MaxLength(500)]
        public string FileName { get; set; } = string.Empty;

        /// <summary>File path on disk.</summary>
        [MaxLength(1000)]
        public string? FilePath { get; set; }

        /// <summary>File extension.</summary>
        [MaxLength(20)]
        public string? FileExtension { get; set; }

        /// <summary>File size in bytes.</summary>
        public long FileSizeBytes { get; set; }

        /// <summary>Uploader user ID.</summary>
        [Required]
        public string UploadedById { get; set; } = string.Empty;

        [ForeignKey(nameof(UploadedById))]
        public virtual ApplicationUser? UploadedBy { get; set; }

        /// <summary>When the file was scanned.</summary>
        public DateTime? ScanDate { get; set; }

        /// <summary>Current moderation status.</summary>
        [Required]
        public ModerationStatus ModerationStatus { get; set; } = ModerationStatus.Pending;

        /// <summary>Risk score from content scan (0-100+).</summary>
        public int RiskScore { get; set; }

        /// <summary>Number of violations detected.</summary>
        public int ViolationCount { get; set; }

        /// <summary>Summary of scan findings.</summary>
        [MaxLength(2000)]
        public string? ScanSummary { get; set; }

        /// <summary>Whether admin review is required.</summary>
        public bool RequiresReview { get; set; }

        /// <summary>Admin who reviewed the file.</summary>
        [MaxLength(100)]
        public string? ReviewedById { get; set; }

        /// <summary>When the file was reviewed.</summary>
        public DateTime? ReviewedAt { get; set; }

        /// <summary>Admin review notes.</summary>
        [MaxLength(1000)]
        public string? ReviewNotes { get; set; }

        /// <summary>Source of upload (OrderCreation, ChatAttachment, DraftDelivery, RevisionAttachment, DisputeEvidence, SupportTicket).</summary>
        [MaxLength(50)]
        public string? SourceType { get; set; }

        /// <summary>Source entity ID.</summary>
        [MaxLength(100)]
        public string? SourceId { get; set; }

        /// <summary>Timestamp.</summary>
        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>Whether the file is quarantined (cannot be delivered/downloaded).</summary>
        public bool IsQuarantined { get; set; }
    }
}