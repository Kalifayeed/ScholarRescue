using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ScholarRescue.Models
{
    /// <summary>
    /// Stores writer payment method details for payout processing.
    /// Admins can view but not edit these directly.
    /// </summary>
    public class WriterPaymentDetail
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "Writer")]
        public string WriterId { get; set; } = string.Empty;

        [ForeignKey(nameof(WriterId))]
        public virtual ApplicationUser Writer { get; set; } = null!;

        /// <summary>Payment method type.</summary>
        [Required]
        [MaxLength(50)]
        [Display(Name = "Payment Method")]
        public string PaymentMethod { get; set; } = string.Empty; // M-Pesa, PayPal, Bank Transfer

        /// <summary>Account holder name.</summary>
        [MaxLength(200)]
        [Display(Name = "Account Name")]
        public string? AccountName { get; set; }

        /// <summary>Account/ID number.</summary>
        [MaxLength(200)]
        [Display(Name = "Account Number")]
        public string? AccountNumber { get; set; }

        /// <summary>Phone number (for M-Pesa).</summary>
        [MaxLength(50)]
        [Display(Name = "Phone Number")]
        public string? PhoneNumber { get; set; }

        /// <summary>Bank name (for bank transfers).</summary>
        [MaxLength(200)]
        [Display(Name = "Bank Name")]
        public string? BankName { get; set; }

        /// <summary>PayPal email address.</summary>
        [MaxLength(200)]
        [Display(Name = "PayPal Email")]
        public string? PayPalEmail { get; set; }

        /// <summary>When this record was last updated.</summary>
        [Display(Name = "Last Updated")]
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }
}