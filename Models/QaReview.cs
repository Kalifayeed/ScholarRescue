using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ScholarRescue.Models.Enums;

namespace ScholarRescue.Models
{
    /// <summary>
    /// Quality Assurance review record for a final submission.
    /// Admin checks tutor feedback quality before delivery to client.
    /// </summary>
    public class QaReview
    {
        public int Id { get; set; }

        [Required]
        public int OrderId { get; set; }

        [ForeignKey(nameof(OrderId))]
        public virtual TutoringRequest? Order { get; set; }

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

        // ════════════════════════════════════════════════════════════
        // Legacy essay-mill checklist fields — kept for backward
        // compatibility with any existing QaReview rows. New reviews
        // will use the feedback-quality fields below.
        // ════════════════════════════════════════════════════════════

        /// <summary>
        /// Whether formatting meets the required standards. (Legacy field)
        /// </summary>
        public bool FormattingPassed { get; set; }

        /// <summary>
        /// Whether the word count matches the requirements. (Legacy field)
        /// </summary>
        public bool WordCountPassed { get; set; }

        /// <summary>
        /// Whether all required files are present and properly uploaded. (Legacy field)
        /// </summary>
        public bool FilesPassed { get; set; }

        /// <summary>
        /// Whether all client instructions were followed correctly. (Legacy field)
        /// </summary>
        public bool InstructionsPassed { get; set; }

        /// <summary>
        /// Path to the uploaded plagiarism report (PDF or screenshot). (Legacy field)
        /// </summary>
        [MaxLength(500)]
        public string? PlagiarismReportPath { get; set; }

        // ════════════════════════════════════════════════════════════
        // Feedback-quality checklist fields (Phase 4)
        // ════════════════════════════════════════════════════════════

        /// <summary>
        /// Tutor engaged substantively with the content of the student's draft —
        /// feedback goes beyond surface corrections to address argument, structure, or clarity.
        /// </summary>
        public bool? FeedbackIsSubstantive { get; set; }

        /// <summary>
        /// Feedback is specific and actionable — student knows exactly what to change and why,
        /// not just that something is wrong.
        /// </summary>
        public bool? FeedbackIsActionable { get; set; }

        /// <summary>
        /// Feedback addresses the student's own work, not a rewrite — tutor improved the draft
        /// rather than replacing it.
        /// </summary>
        public bool? PreservesStudentVoice { get; set; }

        /// <summary>
        /// Feedback directly addresses the request type (e.g. for ProofreadingOwnWork,
        /// grammar/flow corrections are present; for DraftFeedback, argument/structure
        /// guidance is present).
        /// </summary>
        public bool? MatchesRequestType { get; set; }

        /// <summary>
        /// Written notes accompanying the feedback are clear, professional, and
        /// appropriately detailed (at least a few sentences addressing the key issues).
        /// </summary>
        public bool? FeedbackNotesQualityPassed { get; set; }

        /// <summary>
        /// Optional admin notes on this QA decision — required when IsApproved is false,
        /// to give the tutor specific guidance on what needs to improve.
        /// </summary>
        [MaxLength(2000)]
        public string? AdminNotes { get; set; }

        [MaxLength(1000)]
        public string? Notes { get; set; }

        /// <summary>
        /// Whether QA passed. If true, order moves to Delivered status.
        /// </summary>
        public bool IsApproved { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}