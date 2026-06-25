using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ScholarRescue.Models.Enums;

namespace ScholarRescue.Models
{
    /// <summary>
    /// Quality Assurance review record for a final submission.
    /// Admin checks formatting, word count, files, and instructions before delivery.
    /// </summary>
    public class QaReview
    {
        public int Id { get; set; }

        [Required]
        public int OrderId { get; set; }

        [ForeignKey(nameof(OrderId))]
        public virtual Order? Order { get; set; }

        /// <summary>
        /// The order submission being reviewed.
        /// </summary>
        public int SubmissionId { get; set; }

        /// <summary>
        /// Admin who performed the review.
        /// </summary>
        [Required]
        [MaxLength(450)]
        public string ReviewerId { get; set; } = string.Empty;

        [ForeignKey(nameof(ReviewerId))]
        public virtual ApplicationUser? Reviewer { get; set; }

        /// <summary>
        /// Whether formatting meets the required standards.
        /// </summary>
        public bool FormattingPassed { get; set; }

        /// <summary>
        /// Whether the word count matches the requirements.
        /// </summary>
        public bool WordCountPassed { get; set; }

        /// <summary>
        /// Whether all required files are present and properly uploaded.
        /// </summary>
        public bool FilesPassed { get; set; }

        /// <summary>
        /// Whether all client instructions were followed correctly.
        /// </summary>
        public bool InstructionsPassed { get; set; }

        /// <summary>
        /// Path to the uploaded plagiarism report (PDF or screenshot).
        /// </summary>
        [MaxLength(500)]
        public string? PlagiarismReportPath { get; set; }

        [MaxLength(1000)]
        public string? Notes { get; set; }

        /// <summary>
        /// Whether QA passed. If true, order moves to Delivered status.
        /// </summary>
        public bool IsApproved { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}