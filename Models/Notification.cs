using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ScholarRescue.Models.Enums;

namespace ScholarRescue.Models
{
    /// <summary>
    /// Represents a notification sent to a user across any channel (in-app, email, etc.).
    /// Designed for extensibility to support SMS, WhatsApp, and push notifications.
    /// </summary>
    public class Notification
    {
        [Key]
        public int Id { get; set; }

        /// <summary>The recipient user ID.</summary>
        [Required]
        [Display(Name = "User")]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey(nameof(UserId))]
        public virtual ApplicationUser User { get; set; } = null!;

        /// <summary>Notification title/heading.</summary>
        [Required]
        [MaxLength(200)]
        [Display(Name = "Title")]
        public string Title { get; set; } = string.Empty;

        /// <summary>Notification body text.</summary>
        [Required]
        [MaxLength(2000)]
        [Display(Name = "Message")]
        public string Message { get; set; } = string.Empty;

        /// <summary>The type/category of notification event.</summary>
        [Required]
        [Display(Name = "Notification Type")]
        public NotificationType NotificationType { get; set; }

        /// <summary>Optional ID of the related entity (e.g., OrderId).</summary>
        [MaxLength(500)]
        [Display(Name = "Related Entity ID")]
        public string? RelatedEntityId { get; set; }

        /// <summary>Optional type name of the related entity (e.g., "Order", "Message").</summary>
        [MaxLength(100)]
        [Display(Name = "Related Entity Type")]
        public string? RelatedEntityType { get; set; }

        /// <summary>Whether the user has read this notification.</summary>
        [Display(Name = "Is Read")]
        public bool IsRead { get; set; }

        /// <summary>When the notification was created.</summary>
        [Required]
        [Display(Name = "Created At")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>When the user read this notification (null if unread).</summary>
        [Display(Name = "Read At")]
        public DateTime? ReadAt { get; set; }

        /// <summary>Whether the email notification was sent.</summary>
        [Display(Name = "Email Sent")]
        public bool EmailSent { get; set; }

        /// <summary>When the email was sent (null if not sent).</summary>
        [Display(Name = "Email Sent At")]
        public DateTime? EmailSentAt { get; set; }

        /// <summary>Priority level for the notification.</summary>
        [Display(Name = "Priority")]
        public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;

        /// <summary>Whether the notification has been archived.</summary>
        [Display(Name = "Is Archived")]
        public bool IsArchived { get; set; }
    }
}
