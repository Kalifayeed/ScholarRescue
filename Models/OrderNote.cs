using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ScholarRescue.Models
{
    /// <summary>
    /// Represents an internal note attached to an order, typically for
    /// communication between writers and clients or internal staff.
    /// </summary>
    public class OrderNote
    {
        /// <summary>
        /// Primary key for the note.
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Foreign key referencing the order this note belongs to.
        /// </summary>
        [Required]
        public int OrderId { get; set; }

        /// <summary>
        /// Navigation property for the parent order.
        /// </summary>
        [ForeignKey(nameof(OrderId))]
        public virtual TutoringRequest Order { get; set; } = null!;

        /// <summary>
        /// The content of the note.
        /// </summary>
        [Required]
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// Whether this note is visible to the client or internal only.
        /// </summary>
        [Display(Name = "Visible to Client")]
        public bool IsClientVisible { get; set; } = true;

        /// <summary>
        /// Foreign key referencing the user who created the note.
        /// </summary>
        [Required]
        public string CreatedById { get; set; } = string.Empty;

        /// <summary>
        /// Navigation property for the user who created the note.
        /// </summary>
        [ForeignKey(nameof(CreatedById))]
        public virtual ApplicationUser CreatedBy { get; set; } = null!;

        /// <summary>
        /// Timestamp when the note was created.
        /// </summary>
        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}