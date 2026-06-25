using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ScholarRescue.Models
{
    /// <summary>
    /// Stores per-user notification preference flags.
    /// </summary>
    public class NotificationSettings
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "User")]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey(nameof(UserId))]
        public virtual ApplicationUser User { get; set; } = null!;

        /// <summary>Receive notifications for new messages.</summary>
        public bool NotifyMessages { get; set; } = true;

        /// <summary>Receive notifications for order status changes.</summary>
        public bool NotifyOrders { get; set; } = true;

        /// <summary>Receive notifications when orders are assigned.</summary>
        public bool NotifyAssignments { get; set; } = true;

        /// <summary>Receive notifications for revision requests.</summary>
        public bool NotifyRevisions { get; set; } = true;

        /// <summary>Receive system alert notifications.</summary>
        public bool NotifySystemAlerts { get; set; } = true;

        /// <summary>Receive email notifications.</summary>
        [Display(Name = "Email Notifications")]
        public bool EmailNotifications { get; set; } = true;

        /// <summary>Receive in-app notifications.</summary>
        [Display(Name = "In-App Notifications")]
        public bool InAppNotifications { get; set; } = true;

        /// <summary>Receive SMS notifications (future).</summary>
        [Display(Name = "SMS Notifications")]
        public bool SmsNotifications { get; set; }

        /// <summary>Receive WhatsApp notifications (future).</summary>
        [Display(Name = "WhatsApp Notifications")]
        public bool WhatsAppNotifications { get; set; }
    }
}