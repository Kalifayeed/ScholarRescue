using System.ComponentModel.DataAnnotations;
using ScholarRescue.Models;

namespace ScholarRescue.ViewModels.Messaging
{
    /// <summary>
    /// Represents a single message item rendered in the chat interface.
    /// </summary>
    public class MessageViewModel
    {
        public int Id { get; set; }
        public int ConversationId { get; set; }
        public string SenderId { get; set; } = string.Empty;
        public string SenderName { get; set; } = string.Empty;
        public string? SenderAvatarInitials { get; set; }
        public string? SenderRole { get; set; }
        public string MessageText { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public bool IsRead { get; set; }
        public bool IsEdited { get; set; }
        public DateTime? EditedDate { get; set; }
        public bool IsMine { get; set; }
        public bool HasAttachment { get; set; }
        public string? AttachmentFileName { get; set; }
        public string? AttachmentUrl { get; set; }
    }

    /// <summary>
    /// View model for the conversation details / chat page.
    /// </summary>
    public class ConversationDetailsViewModel
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public string OrderTitle { get; set; } = string.Empty;

        public string CurrentUserId { get; set; } = string.Empty;
        public string CurrentUserName { get; set; } = string.Empty;
        public string CurrentUserRole { get; set; } = string.Empty;

        public List<MessageViewModel> Messages { get; set; } = new();
        public List<ConversationParticipantViewModel> Participants { get; set; } = new();

        public bool CanAccess { get; set; }
        public DateTime? LastReadDate { get; set; }
        public int TotalMessages { get; set; }
    }

    /// <summary>
    /// View model used for sending a new message via HTTP POST.
    /// </summary>
    public class SendMessageViewModel
    {
        [Required]
        public int ConversationId { get; set; }

        [Required(AllowEmptyStrings = false, ErrorMessage = "Message text is required.")]
        [MaxLength(5000, ErrorMessage = "Message cannot exceed 5000 characters.")]
        [Display(Name = "Message")]
        public string MessageText { get; set; } = string.Empty;

        public int? AttachmentId { get; set; }
    }

    /// <summary>
    /// Lightweight DTO used by the message search endpoint and for AJAX paged history.
    /// </summary>
    public class MessageSearchResultViewModel
    {
        public int ConversationId { get; set; }
        public string ConversationName { get; set; } = string.Empty;
        public List<MessageViewModel> Messages { get; set; } = new();
    }

    /// <summary>
    /// Lightweight view model for the dashboard "Recent Conversations" / "Recent Messages" widget.
    /// </summary>
    public class RecentConversationViewModel
    {
        public int ConversationId { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public string OrderTitle { get; set; } = string.Empty;
        public string OtherPartyName { get; set; } = string.Empty;
        public string? LastMessagePreview { get; set; }
        public DateTime LastMessageDate { get; set; }
        public int UnreadCount { get; set; }
        public bool IsOtherPartyOnline { get; set; }
    }
}
