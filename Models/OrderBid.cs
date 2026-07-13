using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ScholarRescue.Models.Enums;

namespace ScholarRescue.Models
{
    /// <summary>
    /// Represents a monetary bid placed by a writer on an order.
    /// Writers bid on available orders; clients review bids.
    /// </summary>
    public class OrderBid
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// The order this bid belongs to.
        /// </summary>
        [Required]
        public int OrderId { get; set; }

        [ForeignKey(nameof(OrderId))]
        public virtual TutoringRequest Order { get; set; } = null!;

        /// <summary>
        /// The writer who placed the bid.
        /// </summary>
        [Required]
        [MaxLength(450)]
        public string WriterId { get; set; } = string.Empty;

        [ForeignKey(nameof(WriterId))]
        public virtual ApplicationUser Writer { get; set; } = null!;

        /// <summary>
        /// The amount the writer proposes for completing the order.
        /// </summary>
        [Required]
        [Range(0.01, double.MaxValue)]
        [DataType(DataType.Currency)]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        /// <summary>
        /// Optional message/proposal from the writer to the client.
        /// </summary>
        [MaxLength(1000)]
        public string? Message { get; set; }

        /// <summary>
        /// Writer's estimated delivery date for the order.
        /// </summary>
        [Display(Name = "Estimated Delivery Date")]
        public DateTime? EstimatedDeliveryDate { get; set; }

        /// <summary>
        /// Current status of the bid.
        /// </summary>
        [Required]
        public OrderBidStatus Status { get; set; } = OrderBidStatus.Pending;

        /// <summary>
        /// When the bid was created.
        /// </summary>
        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// When the bid was last updated.
        /// </summary>
        public DateTime? UpdatedAt { get; set; }
    }
}