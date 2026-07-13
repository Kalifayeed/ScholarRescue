using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ScholarRescue.Models.Enums;

namespace ScholarRescue.Models
{
    /// <summary>
    /// Represents a client's request for revision on submitted work.
    /// </summary>
    public class RevisionRequest
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int OrderId { get; set; }

        [ForeignKey(nameof(OrderId))]
        public virtual TutoringRequest Order { get; set; } = null!;

        [Required]
        [Display(Name = "Client")]
        public string ClientId { get; set; } = string.Empty;

        [ForeignKey(nameof(ClientId))]
        public virtual ApplicationUser Client { get; set; } = null!;

        [Required]
        [Display(Name = "Tutor")]
        public string WriterId { get; set; } = string.Empty;

        [ForeignKey(nameof(WriterId))]
        public virtual ApplicationUser Writer { get; set; } = null!;

        /// <summary>
        /// The client's revision instructions/comments.
        /// </summary>
        [Required]
        [MaxLength(5000)]
        [Display(Name = "Revision Comments")]
        public string Comments { get; set; } = string.Empty;

        /// <summary>
        /// When the revision was requested.
        /// </summary>
        [Required]
        [Display(Name = "Requested At")]
        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Status of this revision request.
        /// </summary>
        [Required]
        public RevisionRequestStatus Status { get; set; } = RevisionRequestStatus.Pending;
    }
}