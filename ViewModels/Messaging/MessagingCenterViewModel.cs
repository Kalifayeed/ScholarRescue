using Microsoft.AspNetCore.Http;
using ScholarRescue.Models;
using ScholarRescue.Models.Enums;

namespace ScholarRescue.ViewModels.Messaging
{
    /// <summary>
    /// View model for the combined messaging center (order chats + support tickets).
    /// </summary>
    public class MessagingCenterViewModel
    {
        public List<ConversationListViewModel> Conversations { get; set; } = new();
        public List<TicketListViewModel> SupportTickets { get; set; } = new();
        public string? SearchTerm { get; set; }
        public int TotalUnreadCount { get; set; }
        public int OpenTicketCount { get; set; }
    }

    /// <summary>
    /// View model for a support ticket displayed in the ticket list.
    /// </summary>
    public class TicketListViewModel
    {
        public int Id { get; set; }
        public string TicketNumber { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string DepartmentDisplay { get; set; } = string.Empty;
        public SupportDepartment Department { get; set; }
        public string StatusDisplay { get; set; } = string.Empty;
        public TicketStatus Status { get; set; }
        public TicketPriority Priority { get; set; } = TicketPriority.Normal;
        public DateTime CreatedAt { get; set; }
        public DateTime? LastReplyDate { get; set; }
        public int UnreadCount { get; set; }
        public string? QueueName { get; set; }
        public string? AssignedAdminName { get; set; }
        public bool HasAttachments { get; set; }
    }

    /// <summary>
    /// View model for creating a new support message/ticket.
    /// </summary>
    public class CreateSupportMessageViewModel
    {
        public SupportDepartment Department { get; set; }
        public string Subject { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public List<IFormFile>? Attachments { get; set; }
    }

    /// <summary>
    /// View model for an individual ticket conversation thread.
    /// </summary>
    public class TicketThreadViewModel
    {
        public int Id { get; set; }
        public string TicketNumber { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string DepartmentDisplay { get; set; } = string.Empty;
        public SupportDepartment Department { get; set; }
        public string StatusDisplay { get; set; } = string.Empty;
        public TicketStatus Status { get; set; }
        public TicketPriority Priority { get; set; } = TicketPriority.Normal;
        public DateTime CreatedAt { get; set; }
        public string CreatorName { get; set; } = string.Empty;
        public string? AssignedAdminName { get; set; }
        public string? QueueName { get; set; }
        public bool IsAdmin { get; set; }
        public List<TicketNoteViewModel> Notes { get; set; } = new();
        public List<TicketAttachmentViewModel> Attachments { get; set; } = new();
        public string? NewReply { get; set; }
        public List<IFormFile>? NewAttachments { get; set; }
    }

    /// <summary>
    /// View model for a note/reply in a ticket thread.
    /// </summary>
    public class TicketNoteViewModel
    {
        public int Id { get; set; }
        public string Content { get; set; } = string.Empty;
        public string AuthorName { get; set; } = string.Empty;
        public string AuthorId { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool IsAdminNote { get; set; }
        public bool IsInternal { get; set; }
    }

    /// <summary>
    /// View model for a ticket attachment.
    /// </summary>
    public class TicketAttachmentViewModel
    {
        public int Id { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public long FileSizeBytes { get; set; }
        public DateTime UploadedAt { get; set; }
    }

    /// <summary>
    /// View model for admin ticket management.
    /// </summary>
    public class AdminTicketManagementViewModel
    {
        public List<TicketListViewModel> Tickets { get; set; } = new();
        public SupportDepartment? DepartmentFilter { get; set; }
        public TicketStatus? StatusFilter { get; set; }
        public string? SearchTerm { get; set; }
        public int OpenCount { get; set; }
        public int PendingCount { get; set; }
        public int ResolvedCount { get; set; }
    }
}