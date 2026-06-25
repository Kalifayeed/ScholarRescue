using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ScholarRescue.Models.Enums;

namespace ScholarRescue.Models
{
    /// <summary>
    /// Immutable ledger entry recording every financial event in the system.
    /// Balances are never manually edited; they are always derived from transactions.
    /// </summary>
    public class FinancialTransaction
    {
        [Key]
        public int Id { get; set; }

        /// <summary>Unique transaction number: TXN-YYYY-000001</summary>
        [Required]
        [MaxLength(30)]
        public string TransactionNumber { get; set; } = string.Empty;

        /// <summary>The type of financial event.</summary>
        [Required]
        public TransactionType TransactionType { get; set; }

        /// <summary>ID of the related entity (order, payout, etc.).</summary>
        public int? ReferenceId { get; set; }

        /// <summary>Type of the related entity ("Order", "PayoutRequest", etc.).</summary>
        [MaxLength(50)]
        public string? ReferenceType { get; set; }

        /// <summary>User this transaction is for (writer, client, or system).</summary>
        [MaxLength(450)]
        public string? UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public virtual ApplicationUser? User { get; set; }

        /// <summary>Human-readable description of the transaction.</summary>
        [Required]
        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;

        /// <summary>Amount debited (money leaving the subject's balance).</summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal DebitAmount { get; set; }

        /// <summary>Amount credited (money entering the subject's balance).</summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal CreditAmount { get; set; }

        /// <summary>Balance of the subject after this transaction.</summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal BalanceAfter { get; set; }

        /// <summary>When the transaction occurred.</summary>
        [Required]
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        /// <summary>Who performed this transaction.</summary>
        [MaxLength(450)]
        public string? CreatedBy { get; set; }

        [ForeignKey(nameof(CreatedBy))]
        public virtual ApplicationUser? CreatedByUser { get; set; }
    }
}