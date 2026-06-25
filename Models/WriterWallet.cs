using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ScholarRescue.Models
{
    /// <summary>
    /// Tracks writer earnings, balances, and lifetime financial data.
    /// </summary>
    public class WriterWallet
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "Writer")]
        public string WriterId { get; set; } = string.Empty;

        [ForeignKey(nameof(WriterId))]
        public virtual ApplicationUser Writer { get; set; } = null!;

        /// <summary>Balance available for payout.</summary>
        [Display(Name = "Available Balance")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal AvailableBalance { get; set; }

        /// <summary>Balance from orders not yet eligible for payout.</summary>
        [Display(Name = "Pending Balance")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal PendingBalance { get; set; }

        /// <summary>Total earnings over lifetime.</summary>
        [Display(Name = "Lifetime Earnings")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal LifetimeEarnings { get; set; }

        /// <summary>Total commission paid over lifetime.</summary>
        [Display(Name = "Lifetime Commission")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal LifetimeCommissionPaid { get; set; }

        /// <summary>Total amount paid out to this writer over lifetime.</summary>
        [Display(Name = "Lifetime Payouts")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal LifetimePayouts { get; set; }

        /// <summary>When the wallet was last updated.</summary>
        [Display(Name = "Last Updated")]
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }
}