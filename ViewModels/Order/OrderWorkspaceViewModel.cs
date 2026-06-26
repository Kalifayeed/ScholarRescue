using ScholarRescue.Models;
using ScholarRescue.Models.Enums;

namespace ScholarRescue.ViewModels.Order
{
    /// <summary>
    /// ViewModel for the Assigned Order Workspace page.
    /// Shows privacy-safe order details without exposing client/writer contact info.
    /// </summary>
    public class OrderWorkspaceViewModel
    {
        public int Id { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Subject { get; set; } = string.Empty;
        public AcademicLevel AcademicLevel { get; set; }
        public CitationFormat CitationFormat { get; set; }
        public string AcademicLevelName { get; set; } = string.Empty;
        public string CitationFormatName { get; set; } = string.Empty;
        public DateTime Deadline { get; set; }
        public int Pages { get; set; }
        public int WordCount { get; set; }
        public decimal Budget { get; set; }
        public OrderStatus Status { get; set; }
        public bool IsAssigned { get; set; }

        /// <summary>Label for the other party: "Assigned Writer" for client, "Client" for writer, "Client / Assigned Writer" for admin.</summary>
        public string OtherPartyLabel { get; set; } = string.Empty;
        public string OtherPartyName { get; set; } = string.Empty;
        /// <summary>Role placeholder: "Writer", "Client", "Administrator"</summary>
        public string MyRole { get; set; } = string.Empty;

        /// <summary>Existing conversation ID for this order, if any.</summary>
        public int? ConversationId { get; set; }

        /// <summary>Order attachments uploaded for this order.</summary>
        public List<OrderAttachment> Attachments { get; set; } = new();
        public bool HasAttachments => Attachments.Count > 0;
    }
}