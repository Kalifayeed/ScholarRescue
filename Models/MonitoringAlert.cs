using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ScholarRescue.Models.Enums;

namespace ScholarRescue.Models
{
    /// <summary>
    /// Represents a system-generated alert from the Order Monitoring Engine,
    /// flagging orders that need administrative attention.
    /// </summary>
    public class MonitoringAlert
    {
        [Key]
        public int Id { get; set; }

        /// <summary>The type of alert generated.</summary>
        [Required]
        public MonitoringAlertType AlertType { get; set; }

        /// <summary>Order this alert relates to.</summary>
        [Required]
        public int OrderId { get; set; }

        [ForeignKey(nameof(OrderId))]
        public virtual TutoringRequest Order { get; set; } = null!;

        /// <summary>Optional: writer involved in the alert (inactive writer, etc.).</summary>
        [MaxLength(450)]
        public string? WriterId { get; set; }

        [ForeignKey(nameof(WriterId))]
        public virtual ApplicationUser? Writer { get; set; }

        /// <summary>Optional: milestone involved in the alert.</summary>
        public int? MilestoneId { get; set; }

        /// <summary>Human-readable description of the issue.</summary>
        [Required]
        [MaxLength(2000)]
        public string Description { get; set; } = string.Empty;

        /// <summary>Whether the alert has been acknowledged by an admin.</summary>
        public bool IsAcknowledged { get; set; }

        /// <summary>Admin who acknowledged this alert.</summary>
        [MaxLength(450)]
        public string? AcknowledgedById { get; set; }

        [ForeignKey(nameof(AcknowledgedById))]
        public virtual ApplicationUser? AcknowledgedBy { get; set; }

        /// <summary>When the alert was acknowledged.</summary>
        public DateTime? AcknowledgedAt { get; set; }

        /// <summary>When the alert was created.</summary>
        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>When the alert was resolved (the condition cleared).</summary>
        public DateTime? ResolvedAt { get; set; }
    }
}