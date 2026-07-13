using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ScholarRescue.Models.Enums;

namespace ScholarRescue.Models
{
    /// <summary>
    /// Represents an academic support tutoring request (formerly "Order") placed by a client 
    /// and fulfilled by a tutor/writer.
    /// </summary>
    public class TutoringRequest
    {
        /// <summary>
        /// Primary key for the tutoring request.
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Unique human-readable identifier for the request (e.g., "SR-2026-000001").
        /// </summary>
        [Required]
        [MaxLength(20)]
        [Display(Name = "Request Number")]
        [Column("OrderNumber")]
        public string OrderNumber { get; set; } = string.Empty;

        /// <summary>
        /// The type of academic support requested. Determines whether a client draft is required.
        /// </summary>
        [Required]
        [Display(Name = "Request Type")]
        public RequestType RequestType { get; set; }

        /// <summary>
        /// The title of the academic paper/assignment.
        /// </summary>
        [Required]
        [MaxLength(500)]
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Detailed description of the request requirements.
        /// </summary>
        [Required]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Academic subject or topic of the request.
        /// </summary>
        [Required]
        [MaxLength(200)]
        public string Subject { get; set; } = string.Empty;

        /// <summary>
        /// The academic level required (HighSchool, College, Undergraduate, Masters, PhD).
        /// </summary>
        [Required]
        [Display(Name = "Academic Level")]
        public AcademicLevel AcademicLevel { get; set; }

        /// <summary>
        /// Required citation/reference format for the request.
        /// </summary>
        [Required]
        [Display(Name = "Citation Format")]
        public CitationFormat CitationFormat { get; set; } = CitationFormat.APA_7th;

        /// <summary>
        /// The deadline by which the request must be completed.
        /// </summary>
        [Required]
        [Display(Name = "Deadline")]
        public DateTime Deadline { get; set; }

        /// <summary>
        /// Number of pages in the client's existing draft (informational, not a spec).
        /// Optional — only relevant when the client has an existing document.
        /// </summary>
        [Range(1, 1000)]
        public int? Pages { get; set; }

        /// <summary>
        /// Word count of the client's existing draft (informational, not a spec).
        /// Optional — only relevant when the client has an existing document.
        /// </summary>
        [Range(1, 100000)]
        [Display(Name = "Word Count")]
        public int? WordCount { get; set; }

        /// <summary>
        /// The total budget/price for the request.
        /// </summary>
        [Required]
        [Range(0.01, double.MaxValue)]
        [DataType(DataType.Currency)]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Budget")]
        public decimal Budget { get; set; }

        /// <summary>
        /// Number of sources/references requested (informational, does not affect pricing).
        /// </summary>
        [Range(0, 100)]
        [Display(Name = "Number of Sources")]
        public int NumberOfSources { get; set; }

        /// <summary>
        /// Commission amount (10% of budget) retained by the platform.
        /// </summary>
        [DataType(DataType.Currency)]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Commission")]
        public decimal CommissionAmount { get; set; }

        /// <summary>
        /// Writer's earnings after commission (90% of budget).
        /// </summary>
        [DataType(DataType.Currency)]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Writer Earnings")]
        public decimal WriterEarnings { get; set; }

        /// <summary>
        /// Priority level of the request.
        /// </summary>
        [Required]
        public PriorityLevel Priority { get; set; } = PriorityLevel.Normal;

        /// <summary>
        /// Current status of the request in its lifecycle.
        /// </summary>
        [Required]
        public OrderStatus Status { get; set; } = OrderStatus.Draft;

        /// <summary>
        /// Foreign key referencing the client (ApplicationUser) who created the request.
        /// </summary>
        [Required]
        [Display(Name = "Client")]
        public string ClientId { get; set; } = string.Empty;

        /// <summary>
        /// Navigation property for the client who owns the request.
        /// </summary>
        [ForeignKey(nameof(ClientId))]
        public virtual ApplicationUser Client { get; set; } = null!;

        /// <summary>
        /// Foreign key referencing the writer (ApplicationUser) assigned to the request.
        /// Nullable since a request may not yet be assigned to a writer.
        /// </summary>
        [Display(Name = "Assigned Writer")]
        public string? AssignedWriterId { get; set; }

        /// <summary>
        /// Navigation property for the writer assigned to fulfill the request.
        /// </summary>
        [ForeignKey(nameof(AssignedWriterId))]
        public virtual ApplicationUser? AssignedWriter { get; set; }

        /// <summary>
        /// Timestamp when the request was assigned to a writer.
        /// </summary>
        [Display(Name = "Assigned At")]
        public DateTime? AssignedAt { get; set; }

        /// <summary>
        /// Administrator who assigned the writer (if assigned through admin).
        /// </summary>
        [Display(Name = "Assigned By")]
        public string? AssignedByAdminId { get; set; }

        [ForeignKey(nameof(AssignedByAdminId))]
        public virtual ApplicationUser? AssignedByAdmin { get; set; }

        /// <summary>
        /// Whether the request is currently visible on the Available Orders marketplace.
        /// </summary>
        [Display(Name = "Is Open In Marketplace")]
        public bool IsMarketplaceOpen { get; set; }

        /// <summary>
        /// Timestamp when the request was created.
        /// </summary>
        [Required]
        [Display(Name = "Created At")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Timestamp when the request was last updated.
        /// </summary>
        [Required]
        [Display(Name = "Updated At")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Timestamp when the request was completed.
        /// </summary>
        [Display(Name = "Completed At")]
        public DateTime? CompletedAt { get; set; }

        /// <summary>
        /// Client rating (1-5) submitted after request completion. Used for writer ranking.
        /// </summary>
        [Range(1, 5)]
        public int? Rating { get; set; }

        public DateTime? RatedAt { get; set; }

        /// <summary>
        /// Whether this request was flagged as a dispute. Used for writer ranking.
        /// </summary>
        public bool IsDisputed { get; set; }

        /// <summary>
        /// Navigation property for documents attached to this request.
        /// </summary>
        public virtual ICollection<OrderDocument> Documents { get; set; } = new List<OrderDocument>();

        /// <summary>
        /// Navigation property for notes attached to this request.
        /// </summary>
        public virtual ICollection<OrderNote> Notes { get; set; } = new List<OrderNote>();

        /// <summary>
        /// Navigation property for status history of this request.
        /// </summary>
        public virtual ICollection<OrderHistory> History { get; set; } = new List<OrderHistory>();

        /// <summary>
        /// Navigation property for writer applications on this request.
        /// </summary>
        public virtual ICollection<OrderApplication> Applications { get; set; } = new List<OrderApplication>();

        /// <summary>Navigation property for file attachments uploaded by client.</summary>
        public virtual ICollection<OrderAttachment> Attachments { get; set; } = new List<OrderAttachment>();

        /// <summary>Payment status tracking for Paystack integration.</summary>
        [Display(Name = "Payment Status")]
        public OrderPaymentStatus PaymentStatus { get; set; } = OrderPaymentStatus.PendingPayment;

        /// <summary>Paystack transaction reference.</summary>
        [MaxLength(200)]
        [Display(Name = "Paystack Reference")]
        public string? PaystackReference { get; set; }

        /// <summary>Paystack access code for checkout.</summary>
        [MaxLength(200)]
        [Display(Name = "Paystack Access Code")]
        public string? PaystackAccessCode { get; set; }

        /// <summary>When the payment was completed.</summary>
        [Display(Name = "Payment Date")]
        public DateTime? PaymentDate { get; set; }

        /// <summary>When the escrow was funded.</summary>
        [Display(Name = "Escrow Funded Date")]
        public DateTime? EscrowFundedDate { get; set; }

        /// <summary>
        /// Whether the client chose "Pay Later" at creation.
        /// When true, the request is posted to the marketplace immediately without payment,
        /// with an unfunded EscrowAccount in PendingFunding status.
        /// Payment must be completed before work can be accepted.
        /// </summary>
        public bool PaymentDeferred { get; set; }

        // ---- Draft & Registration Fields ----
        /// <summary>Whether this request is a draft (not yet submitted for payment).</summary>
        public bool IsDraft { get; set; }

        /// <summary>When the draft was last saved.</summary>
        [Display(Name = "Draft Saved At")]
        public DateTime? DraftSavedAt { get; set; }

        // ═══════════════════════════════════════════
        // Domain Logic
        // ═══════════════════════════════════════════

        /// <summary>
        /// Returns true if the client is allowed to download the full submission file.
        /// Only paid requests grant full file access to the client.
        /// Admins and assigned writers bypass this check (enforced separately in controller logic).
        /// </summary>
        public bool CanClientAccessFullSubmission => PaymentStatus == OrderPaymentStatus.Paid;

        /// <summary>
        /// Determines whether this request has at least one attachment tagged as StudentDraft,
        /// which is required for DraftFeedback and ProofreadingOwnWork request types.
        /// Returns true if the request type does not require a draft, or if a qualifying attachment exists.
        /// </summary>
        public bool HasRequiredDraftAttachment()
        {
            if (RequestType != RequestType.DraftFeedback &&
                RequestType != RequestType.ProofreadingOwnWork)
            {
                return true; // No draft required for these types
            }

            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            if (Attachments == null)
                return false;

            return Attachments.Any(a => a.AttachmentPurpose == AttachmentPurpose.StudentDraft);
        }
    }
}