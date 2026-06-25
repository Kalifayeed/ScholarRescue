using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ScholarRescue.Models
{
    /// <summary>
    /// Tracks devices used by users for security monitoring and session management.
    /// </summary>
    public class UserDevice
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey(nameof(UserId))]
        public virtual ApplicationUser User { get; set; } = null!;

        [MaxLength(200)]
        public string? DeviceName { get; set; }

        [MaxLength(100)]
        public string? Browser { get; set; }

        [MaxLength(100)]
        public string? OperatingSystem { get; set; }

        [MaxLength(50)]
        public string? IPAddress { get; set; }

        [MaxLength(100)]
        public string? Country { get; set; }

        public DateTime FirstSeen { get; set; } = DateTime.UtcNow;

        public DateTime LastSeen { get; set; } = DateTime.UtcNow;

        public bool IsTrusted { get; set; }

        public bool IsActive { get; set; } = true;
    }
}