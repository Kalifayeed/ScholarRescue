using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ScholarRescue.Models
{
    /// <summary>
    /// Represents a message within a conversation thread.
    /// Messages are stored permanently and can be edited.
    /// </summary>
    public class Message
    {
        /// <summary>
        /// Primary key for the message.
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Foreign key referencing the conversation this message belongs to.
        /// </summary>
        [Required]
        [Display(Name = "Conversation")]
        public int ConversationId { get; set; }

        /// <summary>
        /// Navigation property for the parent conversation.
        /// </summary>
        [ForeignKey(nameof(ConversationId))]
        public virtual Conversation Conversation { get; set; } = null!;

        /// <summary>
        /// Foreign key referencing the user who sent this message.
        /// </summary>
        [Required]
        [Display(Name = "Sender")]
        public string SenderId { get; set; } = string.Empty;

        /// <summary>
        /// Navigation property for the message sender.
        /// </summary>
        [ForeignKey(nameof(SenderId))]
        public virtual ApplicationUser Sender { get; set; } = null!;

        /// <summary>
        /// The content of the message.
        /// </summary>
        [Required]
        [MaxLength(5000)]
        [Display(Name = "Message")]
        public string MessageText { get; set; } = string.Empty;

        /// <summary>
        /// Foreign key referencing the file attachment if any.
        /// Nullable since not all messages have attachments.
        /// </summary>
        [Display(Name = "Attachment")]
        public int? AttachmentId { get; set; }

        /// <summary>
        /// Navigation property for the file attachment.
        /// </summary>
        [ForeignKey(nameof(AttachmentId))]
        public virtual MessageAttachment? Attachment { get; set; }

        /// <summary>
        /// Timestamp when the message was created.
        /// </summary>
        [Required]
        [Display(Name = "Created At")]
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Whether this message has been read by the recipient.
        /// </summary>
        [Display(Name = "Is Read")]
        public bool IsRead { get; set; }

        /// <summary>
        /// Whether this message has been edited.
        /// </summary>
        [Display(Name = "Is Edited")]
        public bool IsEdited { get; set; }

        /// <summary>
        /// Timestamp when the message was last edited.
        /// </summary>
        [Display(Name = "Edited At")]
        public DateTime? EditedDate { get; set; }
    }
}