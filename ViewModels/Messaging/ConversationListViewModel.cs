using System.ComponentModel.DataAnnotations;
using ScholarRescue.Models;

namespace ScholarRescue.ViewModels.Messaging
{
    /// <summary>
    /// View model for a single conversation item in the conversation list.
    /// Contains latest message preview, unread count, and participant information.
    /// </summary>
    public class ConversationListViewModel
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public string OrderTitle { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;
        public string? LastMessagePreview { get; set; }
        public DateTime LastMessageDate { get; set; }
        public DateTime CreatedDate { get; set; }

        public int UnreadCount { get; set; }
        public List<ConversationParticipantViewModel> Participants { get; set; } = new();

        public string OtherPartyName { get; set; } = string.Empty;
        public string? OtherPartyId { get; set; }
        public bool IsOtherPartyOnline { get; set; }
        public DateTime? OtherPartyLastSeen { get; set; }
    }

    /// <summary>
    /// View model for the overall conversation index page.
    /// </summary>
    public class ConversationIndexViewModel
    {
        public List<ConversationListViewModel> Conversations { get; set; } = new();
        public string? SearchTerm { get; set; }
        public int TotalUnreadCount { get; set; }
    }

    /// <summary>
    /// Lightweight representation of a conversation participant for UI rendering.
    /// </summary>
    public class ConversationParticipantViewModel
    {
        public string UserId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string UserType { get; set; } = string.Empty;
        public bool IsOnline { get; set; }
        public DateTime? LastSeen { get; set; }
        public DateTime JoinedDate { get; set; }
        public DateTime? LastReadDate { get; set; }
    }
}
