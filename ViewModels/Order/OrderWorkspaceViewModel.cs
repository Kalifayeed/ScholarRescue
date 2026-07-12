using ScholarRescue.Models;
using ScholarRescue.Models.Enums;

namespace ScholarRescue.ViewModels.Order
{
    /// <summary>
    /// ViewModel for the Assigned Order Workspace page.
    /// Shows privacy-safe order details, submissions, and revision workflow.
    /// </summary>
    public class OrderWorkspaceViewModel
    {
        public int Id { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public RequestType RequestType { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Subject { get; set; } = string.Empty;
        public AcademicLevel AcademicLevel { get; set; }
        public CitationFormat CitationFormat { get; set; }
        public string AcademicLevelName { get; set; } = string.Empty;
        public string CitationFormatName { get; set; } = string.Empty;
        public DateTime Deadline { get; set; }
        /// <summary>Number of pages in the client's existing draft (informational).</summary>
        public int? Pages { get; set; }
        /// <summary>Word count of the client's existing draft (informational).</summary>
        public int? WordCount { get; set; }
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

        /// <summary>Writer submissions (drafts/revisions/final) for this order.</summary>
        public List<OrderSubmission> Submissions { get; set; } = new();
        public bool HasSubmissions => Submissions.Count > 0;

        /// <summary>Revision requests for this order.</summary>
        public List<RevisionRequest> RevisionRequests { get; set; } = new();
        public bool HasRevisions => RevisionRequests.Count > 0;

        /// <summary>Whether the current user can submit work (assigned writer with appropriate status).</summary>
        public bool CanSubmitWork { get; set; }

        /// <summary>Whether the current user can request revision (client with submitted work).</summary>
        public bool CanRequestRevision { get; set; }

        /// <summary>Whether the current user can accept work (client with submitted work).</summary>
        public bool CanAcceptWork { get; set; }

        /// <summary>Payment status of the order.</summary>
        public OrderPaymentStatus PaymentStatus { get; set; }

        /// <summary>Whether the client chose Pay Later.</summary>
        public bool PaymentDeferred { get; set; }
    }
}
