using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ScholarRescue.Models
{
    /// <summary>
    /// Represents a writer's request to withdraw funds from their wallet.
    /// Payouts are only allowed on the 1st and 15th of each month.
    /// </summary>
    public class PayoutRequest
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "Writer")]
        public string WriterId { get; set; } = string.Empty;

        [ForeignKey(nameof(WriterId))]
        public virtual ApplicationUser Writer { get; set; } = null!;

        /// <summary>The amount requested for payout.</summary>
        [Required]
        [Range(1, double.MaxValue)]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Amount")]
        public decimal Amount { get; set; }

        /// <summary>When the payout was requested.</summary>
        [Required]
        [Display(Name = "Requested Date")]
        public DateTime RequestedDate { get; set; } = DateTime.UtcNow;

        /// <summary>When the payout was approved/rejected.</summary>
        [Display(Name = "Approved Date")]
        public DateTime? ApprovedDate { get; set; }

        /// <summary>Current status of the payout request.</summary>
        [Required]
        [Display(Name = "Status")]
        public PayoutStatus Status { get; set; } = PayoutStatus.Pending;

        /// <summary>Admin who processed the payout.</summary>
        [Display(Name = "Processed By")]
        public string? ProcessedById { get; set; }

        [ForeignKey(nameof(ProcessedById))]
        public virtual ApplicationUser? ProcessedBy { get; set; }

        /// <summary>Optional admin notes.</summary>
        [MaxLength(1000)]
        [Display(Name = "Admin Notes")]
        public string? AdminNotes { get; set; }

        /// <summary>When the payout was actually paid.</summary>
        [Display(Name = "Payout Date")]
        public DateTime? PayoutDate { get; set; }

        /// <summary>Transaction number reference for this payout.</summary>
        [MaxLength(30)]
        [Display(Name = "Transaction Number")]
        public string? TransactionNumber { get; set; }
    }

    /// <summary>
    /// Status of a payout request.
    /// </summary>
    public enum PayoutStatus
    {
        Pending = 0,
        Approved = 1,
        Rejected = 2,
        Paid = 3
    }
}