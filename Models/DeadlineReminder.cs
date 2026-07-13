using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ScholarRescue.Models
{
    /// <summary>
    /// Tracks deadline reminder history to prevent duplicate reminders.
    /// </summary>
    public class DeadlineReminder
    {
        [Key]
        public int Id { get; set; }

        /// <summary>The order this reminder is for.</summary>
        [Required]
        public int OrderId { get; set; }

        [ForeignKey(nameof(OrderId))]
        public virtual TutoringRequest Order { get; set; } = null!;

        /// <summary>The user who received the reminder.</summary>
        [Required]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey(nameof(UserId))]
        public virtual ApplicationUser User { get; set; } = null!;

        /// <summary>Hours remaining before deadline when this reminder was sent.</summary>
        [Required]
        public int HoursRemaining { get; set; }

        /// <summary>When this reminder was sent.</summary>
        [Required]
        public DateTime SentAt { get; set; } = DateTime.UtcNow;
    }
}