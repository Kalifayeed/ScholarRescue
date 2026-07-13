using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ScholarRescue.Models
{
    /// <summary>
    /// Represents a conversation thread associated with an order.
    /// Each order has exactly one conversation for communication between participants.
    /// </summary>
    public class Conversation
    {
        /// <summary>
        /// Primary key for the conversation.
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Foreign key referencing the order this conversation is about.
        /// </summary>
        [Required]
        [Display(Name = "Order")]
        public int OrderId { get; set; }

        /// <summary>
        /// Navigation property for the associated order.
        /// </summary>
        [ForeignKey(nameof(OrderId))]
        public virtual TutoringRequest Order { get; set; } = null!;

        /// <summary>
        /// Timestamp when the conversation was created.
        /// </summary>
        [Required]
        [Display(Name = "Created At")]
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Timestamp when the last message was sent in this conversation.
        /// </summary>
        [Display(Name = "Last Message")]
        public DateTime LastMessageDate { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Whether this conversation has been archived.
        /// </summary>
        [Display(Name = "Is Archived")]
        public bool IsArchived { get; set; }

        /// <summary>
        /// Navigation property for messages in this conversation.
        /// </summary>
        public virtual ICollection<Message> Messages { get; set; } = new List<Message>();

        /// <summary>
        /// Navigation property for participants in this conversation.
        /// </summary>
        public virtual ICollection<ConversationParticipant> Participants { get; set; } = new List<ConversationParticipant>();
    }
}