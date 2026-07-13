using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ScholarRescue.Models
{
    /// <summary>
    /// One record per completed order storing the financial breakdown.
    /// Revenue is recognized when order status = Completed.
    /// </summary>
    public class OrderFinancialRecord
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int OrderId { get; set; }

        [ForeignKey(nameof(OrderId))]
        public virtual TutoringRequest Order { get; set; } = null!;

        /// <summary>Total order amount (budget).</summary>
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal OrderAmount { get; set; }

        /// <summary>Platform commission (10%).</summary>
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal CommissionAmount { get; set; }

        /// <summary>Writer earnings after commission (90%).</summary>
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal WriterAmount { get; set; }

        /// <summary>When the earnings were released to the writer's pending balance.</summary>
        public DateTime? ReleasedDate { get; set; }

        /// <summary>When this record was created.</summary>
        [Required]
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }
}