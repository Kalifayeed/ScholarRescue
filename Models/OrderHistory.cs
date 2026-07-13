using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ScholarRescue.Models.Enums;

namespace ScholarRescue.Models
{
    /// <summary>
    /// Tracks status changes for an order throughout its lifecycle.
    /// </summary>
    public class OrderHistory
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "Order")]
        public int OrderId { get; set; }

        [ForeignKey(nameof(OrderId))]
        public virtual TutoringRequest Order { get; set; } = null!;

        [Required]
        [Display(Name = "Changed By")]
        public string ChangedById { get; set; } = string.Empty;

        [ForeignKey(nameof(ChangedById))]
        public virtual ApplicationUser ChangedBy { get; set; } = null!;

        [Required]
        [Display(Name = "Previous Status")]
        public OrderStatus OldStatus { get; set; }

        [Required]
        [Display(Name = "New Status")]
        public OrderStatus NewStatus { get; set; }

        [Display(Name = "Notes")]
        [MaxLength(2000)]
        public string? Notes { get; set; }

        [Required]
        [Display(Name = "Changed At")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}