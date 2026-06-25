using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ScholarRescue.Models.Enums;

namespace ScholarRescue.Models
{
    /// <summary>
    /// Records a risk assessment event detected by the AI Risk Engine.
    /// </summary>
    public class RiskAssessment
    {
        [Key]
        public int Id { get; set; }

        /// <summary>Type of entity this risk relates to (User, Order, Message, etc.).</summary>
        [Required]
        [MaxLength(50)]
        public string EntityType { get; set; } = string.Empty;

        /// <summary>ID of the related entity.</summary>
        [Required]
        [MaxLength(100)]
        public string EntityId { get; set; } = string.Empty;

        /// <summary>Category of risk detected.</summary>
        [Required]
        public RiskCategory RiskCategory { get; set; }

        /// <summary>Numeric risk score (0-100).</summary>
        [Required]
        public int RiskScore { get; set; }

        /// <summary>Computed risk level based on score.</summary>
        [Required]
        public RiskLevel RiskLevel { get; set; }

        /// <summary>Human-readable reason for the risk assessment.</summary>
        [Required]
        [MaxLength(1000)]
        public string Reason { get; set; } = string.Empty;

        /// <summary>Details about what was detected (e.g., the phone number, email, pattern).</summary>
        [MaxLength(500)]
        public string? DetectedContent { get; set; }

        /// <summary>When the risk was detected.</summary>
        [Required]
        public DateTime DetectedAt { get; set; } = DateTime.UtcNow;

        /// <summary>When the risk was resolved (null if open).</summary>
        public DateTime? ResolvedAt { get; set; }

        /// <summary>Admin who resolved the risk.</summary>
        [MaxLength(100)]
        public string? ResolvedById { get; set; }

        /// <summary>Status of the risk assessment.</summary>
        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = "Open";

        /// <summary>Whether the message was blocked (for communication violations).</summary>
        public bool IsBlocked { get; set; }

        /// <summary>ID of the flagged message (if applicable).</summary>
        public int? MessageId { get; set; }

        /// <summary>ID of the flagged order (if applicable).</summary>
        public int? OrderId { get; set; }

        [ForeignKey(nameof(MessageId))]
        public virtual Message? Message { get; set; }

        [ForeignKey(nameof(OrderId))]
        public virtual Order? Order { get; set; }
    }
}