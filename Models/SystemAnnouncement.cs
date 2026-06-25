using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ScholarRescue.Models.Enums;

namespace ScholarRescue.Models
{
    /// <summary>
    /// Represents a system-wide announcement or broadcast from admins to users.
    /// </summary>
    public class SystemAnnouncement
    {
        [Key]
        public int Id { get; set; }

        /// <summary>Announcement title.</summary>
        [Required]
        [MaxLength(200)]
        [Display(Name = "Title")]
        public string Title { get; set; } = string.Empty;

        /// <summary>Announcement body content.</summary>
        [Required]
        [MaxLength(5000)]
        [Display(Name = "Content")]
        public string Content { get; set; } = string.Empty;

        /// <summary>User ID of the admin who created the announcement.</summary>
        [Required]
        [Display(Name = "Created By")]
        public string CreatedById { get; set; } = string.Empty;

        [ForeignKey(nameof(CreatedById))]
        public virtual ApplicationUser CreatedBy { get; set; } = null!;

        /// <summary>Priority level for the announcement.</summary>
        [Required]
        [Display(Name = "Priority")]
        public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;

        /// <summary>Target audience for the announcement.</summary>
        [Required]
        [Display(Name = "Target Audience")]
        public TargetAudience TargetAudience { get; set; } = TargetAudience.AllUsers;

        /// <summary>When the announcement was created.</summary>
        [Required]
        [Display(Name = "Created At")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>When the announcement expires (null = never).</summary>
        [Display(Name = "Expires At")]
        public DateTime? ExpiresAt { get; set; }

        /// <summary>Whether this announcement is currently active.</summary>
        [Display(Name = "Is Active")]
        public bool IsActive { get; set; } = true;

        /// <summary>The broadcast category (Platform Update, Maintenance, Policy Change, etc.).</summary>
        [MaxLength(100)]
        [Display(Name = "Broadcast Type")]
        public string? BroadcastType { get; set; }

        [NotMapped]
        public bool IsExpired => ExpiresAt.HasValue && ExpiresAt.Value < DateTime.UtcNow;
    }
}