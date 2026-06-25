using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ScholarRescue.Models.Enums;

namespace ScholarRescue.Models
{
    /// <summary>
    /// Records a content moderation violation for a user.
    /// </summary>
    public class ModerationViolation
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey(nameof(UserId))]
        public virtual ApplicationUser User { get; set; } = null!;

        public int? FileModerationRecordId { get; set; }

        [ForeignKey(nameof(FileModerationRecordId))]
        public virtual FileModerationRecord? FileModerationRecord { get; set; }

        [Required]
        [MaxLength(100)]
        public string ViolationType { get; set; } = string.Empty;

        public int RiskScore { get; set; }

        [MaxLength(1000)]
        public string? Description { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}