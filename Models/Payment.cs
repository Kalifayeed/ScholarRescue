using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ScholarRescue.Models.Enums;

namespace ScholarRescue.Models
{
    /// <summary>
    /// Represents a payment transaction for an order.
    /// Covers PayPal, Visa/Mastercard, Payoneer, Bank Transfer, and Internal Wallet.
    /// </summary>
    public class Payment
    {
        [Key]
        public int Id { get; set; }

        /// <summary>The order being paid for.</summary>
        [Required]
        public int OrderId { get; set; }

        [ForeignKey(nameof(OrderId))]
        public virtual Order Order { get; set; } = null!;

        /// <summary>Payment amount.</summary>
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        /// <summary>Payment method used (PayPal, CreditCard, Payoneer, BankTransfer, Wallet).</summary>
        [Required]
        [MaxLength(50)]
        public string PaymentMethod { get; set; } = string.Empty;

        /// <summary>Current status of this payment.</summary>
        [Required]
        public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

        /// <summary>External transaction reference ID (from PayPal, Stripe, etc.).</summary>
        [MaxLength(255)]
        public string? TransactionReference { get; set; }

        /// <summary>When the payment was created.</summary>
        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>When the payment was completed (null if not yet completed).</summary>
        public DateTime? CompletedAt { get; set; }
    }
}