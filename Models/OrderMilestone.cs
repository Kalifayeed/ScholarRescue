using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ScholarRescue.Models.Enums;

namespace ScholarRescue.Models
{
    /// <summary>
    /// A progressive delivery milestone for an order. For orders with 40+ pages,
    /// milestones are mandatory. For 20-39 pages, they're optional.
    /// </summary>
    public class OrderMilestone
    {
        public int Id { get; set; }

        [Required]
        public int OrderId { get; set; }

        [ForeignKey(nameof(OrderId))]
        public virtual TutoringRequest? Order { get; set; }

        /// <summary>
        /// Display title, e.g. "Chapter 1-2 Draft" or "Research Section".
        /// </summary>
        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Number of pages this milestone covers.
        /// </summary>
        [Range(1, 1000)]
        public int Pages { get; set; }

        /// <summary>
        /// Milestone deadline.
        /// </summary>
        public DateTime Deadline { get; set; }

        public MilestoneStatus Status { get; set; } = MilestoneStatus.Pending;

        /// <summary>
        /// When the writer submitted the milestone files.
        /// </summary>
        public DateTime? SubmittedAt { get; set; }

        /// <summary>
        /// When the client approved the milestone.
        /// </summary>
        public DateTime? ApprovedAt { get; set; }

        /// <summary>
        /// Client who approved the milestone (for the audit trail).
        /// </summary>
        [MaxLength(450)]
        public string? ApprovedById { get; set; }

        [ForeignKey(nameof(ApprovedById))]
        public virtual ApplicationUser? ApprovedBy { get; set; }

        /// <summary>
        /// Optional client comment at approval.
        /// </summary>
        [MaxLength(1000)]
        public string? ApprovalNotes { get; set; }

        /// <summary>
        /// Writer's submission comment / file descriptions.
        /// </summary>
        [MaxLength(1000)]
        public string? SubmissionNotes { get; set; }

        /// <summary>
        /// Earnings credited when this milestone was approved. 90% of (Pages/TotalPages * Budget).
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal ApprovedEarnings { get; set; }

        /// <summary>
        /// Reference to the ledger transaction created on approval.
        /// </summary>
        [MaxLength(50)]
        public string? LedgerTransactionNumber { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Display order index (0, 1, 2...) for the milestone list.
        /// </summary>
        public int SortOrder { get; set; }

        public virtual ICollection<OrderMilestoneFile> Files { get; set; } = new List<OrderMilestoneFile>();
    }
}
