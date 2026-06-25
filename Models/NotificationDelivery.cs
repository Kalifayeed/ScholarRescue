using System.ComponentModel.DataAnnotations;

namespace ScholarRescue.Models
{
    public class NotificationDelivery
    {
        public int Id { get; set; }

        public int NotificationId { get; set; }

        [Required, MaxLength(450)]
        public string UserId { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? DeliveredAt { get; set; }

        public DateTime? ReadAt { get; set; }

        public bool IsDelivered { get; set; }

        public bool IsRead { get; set; }

        [MaxLength(50)]
        public string DeliveryMethod { get; set; } = "InApp"; // InApp, Email, Both

        public int RetryCount { get; set; }

        [MaxLength(500)]
        public string? ErrorMessage { get; set; }
    }
}