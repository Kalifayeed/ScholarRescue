using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ScholarRescue.Models.Enums;

namespace ScholarRescue.Models
{
    /// <summary>
    /// A support ticket created by any user, assigned to a department.
    /// </summary>
    public class SupportTicket
    {
        public int Id { get; set; }

        /// <summary>
        /// Auto-generated ticket number, e.g. "SUP-100001".
        /// </summary>
        [Required]
        [MaxLength(20)]
        public string TicketNumber { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string Subject { get; set; } = string.Empty;

        [Required]
        public string Description { get; set; } = string.Empty;

        public SupportDepartment Department { get; set; }
        public TicketStatus Status { get; set; } = TicketStatus.Open;

        /// <summary>
        /// The user who created the ticket (client, writer, or admin acting on behalf).
        /// </summary>
        [Required]
        [MaxLength(450)]
        public string CreatorId { get; set; } = string.Empty;

        [ForeignKey(nameof(CreatorId))]
        public virtual ApplicationUser? Creator { get; set; }

        /// <summary>
        /// Admin assigned to handle this ticket (if any).
        /// </summary>
        [MaxLength(450)]
        public string? AssignedAdminId { get; set; }

        [ForeignKey(nameof(AssignedAdminId))]
        public virtual ApplicationUser? AssignedAdmin { get; set; }

        /// <summary>
        /// Optional reference to an order if this ticket is about a specific order.
        /// </summary>
        public int? OrderId { get; set; }

        [ForeignKey(nameof(OrderId))]
        public virtual TutoringRequest? Order { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// When the ticket was resolved.
        /// </summary>
        public DateTime? ResolvedAt { get; set; }

        /// <summary>
        /// When someone last replied to this ticket thread.
        /// </summary>
        public DateTime? LastReplyDate { get; set; }

        /// <summary>
        /// The department queue this ticket is assigned to (e.g., "Billing Queue", "Technical Queue").
        /// </summary>
        [MaxLength(100)]
        public string? QueueName { get; set; }

        /// <summary>
        /// Priority level for the ticket.
        /// </summary>
        public TicketPriority Priority { get; set; } = TicketPriority.Normal;

        /// <summary>
        /// Number of unread replies for the creator.
        /// </summary>
        public int UnreadCount { get; set; }

        /// <summary>
        /// Whether the ticket has been reopened after being resolved/closed.
        /// </summary>
        public bool IsReopened { get; set; }

        public virtual ICollection<SupportTicketAttachment> Attachments { get; set; } = new List<SupportTicketAttachment>();
        public virtual ICollection<SupportTicketNote> Notes { get; set; } = new List<SupportTicketNote>();
    }
}