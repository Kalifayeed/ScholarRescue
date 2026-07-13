using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ScholarRescue.Models.Enums;

namespace ScholarRescue.Models
{
    /// <summary>
    /// Immutable audit-quality timeline entry for order lifecycle events.
    /// Events can never be edited or deleted - only created and viewed.
    /// </summary>
    public class OrderTimelineEvent
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int OrderId { get; set; }

        [ForeignKey(nameof(OrderId))]
        public virtual TutoringRequest Order { get; set; } = null!;

        /// <summary>User who performed/triggered the action.</summary>
        [Required]
        [MaxLength(450)]
        public string CreatedByUserId { get; set; } = string.Empty;

        /// <summary>Display name of the user at time of event.</summary>
        [Required]
        [MaxLength(200)]
        public string CreatedByName { get; set; } = string.Empty;

        /// <summary>Type of timeline event.</summary>
        [Required]
        public TimelineEventType EventType { get; set; }

        /// <summary>Short headline (e.g. "Order Created", "Writer Assigned").</summary>
        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        /// <summary>Detailed description of what happened.</summary>
        [MaxLength(2000)]
        public string? Description { get; set; }

        /// <summary>When the event occurred.</summary>
        [Required]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>Optional JSON metadata (file names, amounts, etc.).</summary>
        [MaxLength(4000)]
        public string? MetadataJson { get; set; }

        public bool IsVisibleToClient { get; set; } = true;
        public bool IsVisibleToWriter { get; set; } = true;
        public bool IsVisibleToAdmin { get; set; } = true;
    }
}