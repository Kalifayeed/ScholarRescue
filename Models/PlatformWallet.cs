using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ScholarRescue.Models
{
    /// <summary>
    /// Represents the platform's financial holdings.
    /// Only one platform wallet should exist.
    /// Balances are derived from transactions, never manually edited.
    /// </summary>
    public class PlatformWallet
    {
        [Key]
        public int Id { get; set; }

        /// <summary>Balance available for payouts and operations.</summary>
        [Display(Name = "Available Balance")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal AvailableBalance { get; set; }

        /// <summary>Balance pending from orders not yet recognized as revenue.</summary>
        [Display(Name = "Pending Balance")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal PendingBalance { get; set; }

        /// <summary>Total revenue earned over the platform's lifetime.</summary>
        [Display(Name = "Lifetime Revenue")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal LifetimeRevenue { get; set; }

        /// <summary>Total commission collected over the platform's lifetime.</summary>
        [Display(Name = "Lifetime Commission")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal LifetimeCommission { get; set; }

        /// <summary>Total amount paid out to writers over the platform's lifetime.</summary>
        [Display(Name = "Total Payouts")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalPayouts { get; set; }

        /// <summary>When the wallet was last updated.</summary>
        [Display(Name = "Last Updated")]
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }
}