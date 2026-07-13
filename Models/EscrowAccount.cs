using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ScholarRescue.Models.Enums;

namespace ScholarRescue.Models
{
    /// <summary>
    /// Holds funds in escrow for an order. Funds only move through escrow - never directly.
    /// </summary>
    public class EscrowAccount
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int OrderId { get; set; }
        [ForeignKey(nameof(OrderId))]
        public virtual TutoringRequest Order { get; set; } = null!;

        [Required]
        public string ClientId { get; set; } = string.Empty;
        [ForeignKey(nameof(ClientId))]
        public virtual ApplicationUser Client { get; set; } = null!;

        public string? AssignedWriterId { get; set; }
        [ForeignKey(nameof(AssignedWriterId))]
        public virtual ApplicationUser? AssignedWriter { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal CommissionAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal WriterAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal FundedAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal ReleasedAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal RefundedAmount { get; set; }

        [Required]
        public EscrowStatus Status { get; set; } = EscrowStatus.PendingFunding;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}