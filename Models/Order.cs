using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ScholarRescue.Models.Enums;

namespace ScholarRescue.Models
{
    /// <summary>
    /// Represents an academic support order placed by a client and fulfilled by a tutor/writer.
    /// </summary>
    public class Order
    {
        /// <summary>
        /// Primary key for the order.
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Unique human-readable identifier for the order (e.g., "SR-2026-000001").
        /// </summary>
        [Required]
        [MaxLength(20)]
        [Display(Name = "Order Number")]
        public string OrderNumber { get; set; } = string.Empty;

        /// <summary>
        /// The title of the academic paper/assignment.
        /// </summary>
        [Required]
        [MaxLength(500)]
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Detailed description of the order requirements.
        /// </summary>
        [Required]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Academic subject or topic of the order.
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
        /// Required citation/reference format for the order.
        /// </summary>
        [Required]
        [Display(Name = "Citation Format")]
        public CitationFormat CitationFormat { get; set; } = CitationFormat.APA_7th;

        /// <summary>
        /// The deadline by which the order must be completed.
        /// </summary>
        [Required]
        [Display(Name = "Deadline")]
        public DateTime Deadline { get; set; }

        /// <summary>
        /// Number of pages required for the order.
        /// </summary>
        [Required]
        [Range(1, 1000)]
        public int Pages { get; set; }

        /// <summary>
        /// Word count for the order.
        /// </summary>
        [Required]
        [Range(1, 100000)]
        [Display(Name = "Word Count")]
        public int WordCount { get; set; }

        /// <summary>
        /// The total budget/price for the order.
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
        /// Priority level of the order.
        /// </summary>
        [Required]
        public PriorityLevel Priority { get; set; } = PriorityLevel.Normal;

        /// <summary>
        /// Current status of the order in its lifecycle.
        /// </summary>
        [Required]
        public OrderStatus Status { get; set; } = OrderStatus.Draft;

        /// <summary>
        /// Foreign key referencing the client (ApplicationUser) who created the order.
        /// </summary>
        [Required]
        [Display(Name = "Client")]
        public string ClientId { get; set; } = string.Empty;

        /// <summary>
        /// Navigation property for the client who owns the order.
        /// </summary>
        [ForeignKey(nameof(ClientId))]
        public virtual ApplicationUser Client { get; set; } = null!;

        /// <summary>
        /// Foreign key referencing the writer (ApplicationUser) assigned to the order.
        /// Nullable since an order may not yet be assigned to a writer.
        /// </summary>
        [Display(Name = "Assigned Writer")]
        public string? AssignedWriterId { get; set; }

        /// <summary>
        /// Navigation property for the writer assigned to fulfill the order.
        /// </summary>
        [ForeignKey(nameof(AssignedWriterId))]
        public virtual ApplicationUser? AssignedWriter { get; set; }

        /// <summary>
        /// Timestamp when the order was assigned to a writer.
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
        /// Whether the order is currently visible on the Available Orders marketplace.
        /// </summary>
        [Display(Name = "Is Open In Marketplace")]
        public bool IsMarketplaceOpen { get; set; }

        /// <summary>
        /// Timestamp when the order was created.
        /// </summary>
        [Required]
        [Display(Name = "Created At")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Timestamp when the order was last updated.
        /// </summary>
        [Required]
        [Display(Name = "Updated At")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Timestamp when the order was completed.
        /// </summary>
        [Display(Name = "Completed At")]
        public DateTime? CompletedAt { get; set; }

        /// <summary>
        /// Client rating (1-5) submitted after order completion. Used for writer ranking.
        /// </summary>
        [Range(1, 5)]
        public int? Rating { get; set; }

        public DateTime? RatedAt { get; set; }

        /// <summary>
        /// Whether this order was flagged as a dispute. Used for writer ranking.
        /// </summary>
        public bool IsDisputed { get; set; }

        /// <summary>
        /// Navigation property for documents attached to this order.
        /// </summary>
        public virtual ICollection<OrderDocument> Documents { get; set; } = new List<OrderDocument>();

        /// <summary>
        /// Navigation property for notes attached to this order.
        /// </summary>
        public virtual ICollection<OrderNote> Notes { get; set; } = new List<OrderNote>();

        /// <summary>
        /// Navigation property for status history of this order.
        /// </summary>
        public virtual ICollection<OrderHistory> History { get; set; } = new List<OrderHistory>();

        /// <summary>
        /// Navigation property for writer applications on this order.
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

        // ---- Draft & Registration Fields ----
        /// <summary>Whether this order is a draft (not yet submitted for payment).</summary>
        public bool IsDraft { get; set; }

        /// <summary>When the draft was last saved.</summary>
        [Display(Name = "Draft Saved At")]
        public DateTime? DraftSavedAt { get; set; }
    }
}
