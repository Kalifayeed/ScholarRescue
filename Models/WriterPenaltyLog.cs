using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ScholarRescue.Models
{
    /// <summary>
    /// Immutable audit log entry for every penalty or bonus action on a writer's reliability score.
    /// </summary>
    public class WriterPenaltyLog
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string WriterId { get; set; } = string.Empty;
        [ForeignKey(nameof(WriterId))]
        public virtual ApplicationUser Writer { get; set; } = null!;

        /// <summary>Description of the action (e.g. "Late Delivery", "5-Star Review").</summary>
        [Required]
        [MaxLength(200)]
        public string Action { get; set; } = string.Empty;

        /// <summary>Points deducted (negative) or added (positive).</summary>
        public int PointsAdded { get; set; }
        public int PointsRemoved { get; set; }

        /// <summary>Detailed reason.</summary>
        [MaxLength(1000)]
        public string? Reason { get; set; }

        /// <summary>Admin who applied the action (null if automated).</summary>
        [MaxLength(450)]
        public string? CreatedBy { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}