using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ScholarRescue.Models
{
    /// <summary>
    /// Records administrative actions for audit trail and security review.
    /// </summary>
    public class AuditLog
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Short action identifier (e.g., "User Suspended", "Writer Approved").
        /// </summary>
        [Required]
        [MaxLength(100)]
        [Display(Name = "Action")]
        public string Action { get; set; } = string.Empty;

        /// <summary>
        /// Foreign key referencing the administrator who performed the action.
        /// </summary>
        [Required]
        [Display(Name = "Performed By")]
        public string PerformedById { get; set; } = string.Empty;

        [ForeignKey(nameof(PerformedById))]
        public virtual ApplicationUser PerformedBy { get; set; } = null!;

        /// <summary>
        /// Foreign key referencing the user who was the target of the action (if any).
        /// </summary>
        [Display(Name = "Target User")]
        public string? TargetUserId { get; set; }

        [ForeignKey(nameof(TargetUserId))]
        public virtual ApplicationUser? TargetUser { get; set; }

        /// <summary>
        /// Detailed description of the action performed.
        /// </summary>
        [MaxLength(2000)]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        /// <summary>
        /// Timestamp when the action was performed.
        /// </summary>
        [Required]
        [Display(Name = "Created Date")]
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }
}