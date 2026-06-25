using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ScholarRescue.Models.Enums;

namespace ScholarRescue.Models
{
    /// <summary>
    /// Represents a file submission made by a writer for an order.
    /// Tracks versioned uploads of drafts, revisions, and final submissions.
    /// </summary>
    public class OrderSubmission
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int OrderId { get; set; }

        [ForeignKey(nameof(OrderId))]
        public virtual Order Order { get; set; } = null!;

        [Required]
        [Display(Name = "Writer")]
        public string WriterId { get; set; } = string.Empty;

        [ForeignKey(nameof(WriterId))]
        public virtual ApplicationUser Writer { get; set; } = null!;

        /// <summary>
        /// Version number of this submission (1, 2, 3...).
        /// </summary>
        [Required]
        [Display(Name = "Version Number")]
        public int VersionNumber { get; set; } = 1;

        /// <summary>
        /// Type of submission: Draft, Revision, or Final.
        /// </summary>
        [Required]
        public SubmissionType SubmissionType { get; set; } = SubmissionType.Draft;

        /// <summary>
        /// Stored file path for the submitted document.
        /// </summary>
        [Required]
        [MaxLength(1000)]
        [Display(Name = "File Path")]
        public string FilePath { get; set; } = string.Empty;

        /// <summary>
        /// Original file name as uploaded by the writer.
        /// </summary>
        [Required]
        [MaxLength(500)]
        [Display(Name = "File Name")]
        public string FileName { get; set; } = string.Empty;

        /// <summary>
        /// Writer's comments accompanying this submission.
        /// </summary>
        [MaxLength(2000)]
        public string? Comments { get; set; }

        /// <summary>
        /// When the submission was uploaded.
        /// </summary>
        [Required]
        [Display(Name = "Submitted At")]
        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    }
}