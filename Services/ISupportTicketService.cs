using ScholarRescue.Models;
using ScholarRescue.Models.Enums;

namespace ScholarRescue.Services
{
    public interface ISupportTicketService
    {
        Task<List<SupportTicket>> GetTicketsAsync(string? userId = null, SupportDepartment? department = null, TicketStatus? status = null);
        Task<SupportTicket?> GetByIdAsync(int id);
        Task<SupportTicket> CreateTicketAsync(string subject, string description, SupportDepartment department, string creatorId, int? orderId = null);
        Task<string> GetNextTicketNumberAsync();
        Task<SupportTicketNote> AddNoteAsync(int ticketId, string content, string authorId, bool isInternal = false);
        Task<SupportTicket> ChangeStatusAsync(int ticketId, TicketStatus newStatus, string adminId);
        Task<SupportTicket> AssignAdminAsync(int ticketId, string adminId);
        Task<SupportTicketAssignmentResult> TransferDepartmentAsync(int ticketId, SupportDepartment newDepartment, string adminId);
        Task<SupportTicket> ReopenTicketAsync(int ticketId, string userId);
        Task<SupportTicketAttachment> AddAttachmentAsync(int ticketId, string fileName, string filePath, long fileSize, string? uploadedById);
        Task<List<SupportTicketNote>> GetNotesAsync(int ticketId);
        Task<List<SupportTicketAttachment>> GetAttachmentsAsync(int ticketId);
        Task<int> GetOpenTicketCountAsync(string userId);
        Task<List<SupportTicket>> SearchTicketsAsync(string searchTerm, string? userId = null);
        Task<int> GetTicketCountByStatusAsync(TicketStatus status);
        Task MarkTicketAsReadAsync(int ticketId, string userId);
    }

    /// <summary>
    /// Result of a department transfer operation.
    /// </summary>
    public class SupportTicketAssignmentResult
    {
        public SupportTicket Ticket { get; set; } = null!;
        public string AssignedQueue { get; set; } = string.Empty;
    }
}