using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ScholarRescue.Models
{
    /// <summary>
    /// A file uploaded for an OrderMilestone by the writer.
    /// </summary>
    public class OrderMilestoneFile
    {
        public int Id { get; set; }

        [Required]
        public int MilestoneId { get; set; }

        [ForeignKey(nameof(MilestoneId))]
        public virtual OrderMilestone? Milestone { get; set; }

        [Required]
        [MaxLength(255)]
        public string FileName { get; set; } = string.Empty;

        /// <summary>
        /// Web-relative path under /uploads/ (e.g. /uploads/milestones/12/abc.pdf).
        /// </summary>
        [Required]
        [MaxLength(500)]
        public string FilePath { get; set; } = string.Empty;

        public long FileSizeBytes { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    }
}
