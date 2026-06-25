using System.ComponentModel.DataAnnotations;
using ScholarRescue.Models.Enums;

namespace ScholarRescue.Models
{
    /// <summary>
    /// Represents an email in the processing queue.
    /// Background service dequeues and sends pending emails.
    /// </summary>
    public class EmailQueueItem
    {
        [Key]
        public int Id { get; set; }

        /// <summary>Recipient email address.</summary>
        [Required]
        [MaxLength(256)]
        public string Recipient { get; set; } = string.Empty;

        /// <summary>Email subject.</summary>
        [Required]
        [MaxLength(500)]
        public string Subject { get; set; } = string.Empty;

        /// <summary>HTML email body.</summary>
        [Required]
        public string Body { get; set; } = string.Empty;

        /// <summary>Processing status.</summary>
        [Required]
        public EmailStatus Status { get; set; } = EmailStatus.Pending;

        /// <summary>Number of send attempts.</summary>
        public int Attempts { get; set; }

        /// <summary>Maximum send attempts before failing permanently.</summary>
        public int MaxAttempts { get; set; } = 3;

        /// <summary>Error message from last failed attempt.</summary>
        public string? ErrorMessage { get; set; }

        /// <summary>When the email was created.</summary>
        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>When the email was sent successfully.</summary>
        public DateTime? SentAt { get; set; }
    }
}