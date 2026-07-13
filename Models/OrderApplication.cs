using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ScholarRescue.Models.Enums;

namespace ScholarRescue.Models
{
    /// <summary>
    /// Represents a writer's application/expression of interest for a specific order.
    /// </summary>
    public class OrderApplication
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "Order")]
        public int OrderId { get; set; }

        [ForeignKey(nameof(OrderId))]
        public virtual TutoringRequest Order { get; set; } = null!;

        [Required]
        [Display(Name = "Writer")]
        public string WriterId { get; set; } = string.Empty;

        [ForeignKey(nameof(WriterId))]
        public virtual ApplicationUser Writer { get; set; } = null!;

        [Required]
        [Display(Name = "Applied At")]
        public DateTime AppliedAt { get; set; } = DateTime.UtcNow;

        [Required]
        [Display(Name = "Status")]
        public OrderApplicationStatus Status { get; set; } = OrderApplicationStatus.Pending;

        /// <summary>
        /// Optional message from the writer to the admin.
        /// </summary>
        [MaxLength(1000)]
        [Display(Name = "Message")]
        public string? Message { get; set; }

        /// <summary>
        /// When the application status was last updated.
        /// </summary>
        [Display(Name = "Updated At")]
        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// Administrator who handled the application (if any).
        /// </summary>
        [Display(Name = "Processed By")]
        public string? ProcessedByAdminId { get; set; }

        [ForeignKey(nameof(ProcessedByAdminId))]
        public virtual ApplicationUser? ProcessedByAdmin { get; set; }
    }
}
