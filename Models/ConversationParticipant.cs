using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ScholarRescue.Models
{
    /// <summary>
    /// Represents a participant in a conversation.
    /// Links users to conversations they have access to.
    /// </summary>
    public class ConversationParticipant
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "Conversation")]
        public int ConversationId { get; set; }

        [ForeignKey(nameof(ConversationId))]
        public virtual Conversation Conversation { get; set; } = null!;

        [Required]
        [Display(Name = "User")]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey(nameof(UserId))]
        public virtual ApplicationUser User { get; set; } = null!;

        [Required]
        [Display(Name = "Joined At")]
        public DateTime JoinedDate { get; set; } = DateTime.UtcNow;

        [Display(Name = "Is Admin")]
        public bool IsAdmin { get; set; }

        [Display(Name = "Last Read Date")]
        public DateTime? LastReadDate { get; set; }
    }
}