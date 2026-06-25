using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ScholarRescue.Models
{
    /// <summary>
    /// Tracks which users have read a specific announcement.
    /// </summary>
    public class AnnouncementRead
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int AnnouncementId { get; set; }

        [ForeignKey(nameof(AnnouncementId))]
        public virtual SystemAnnouncement Announcement { get; set; } = null!;

        [Required]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey(nameof(UserId))]
        public virtual ApplicationUser User { get; set; } = null!;

        [Required]
        [Display(Name = "Read At")]
        public DateTime ReadAt { get; set; } = DateTime.UtcNow;
    }
}