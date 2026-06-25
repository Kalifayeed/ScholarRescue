using Microsoft.EntityFrameworkCore;
using ScholarRescue.Data;
using ScholarRescue.Models;
using ScholarRescue.Models.Enums;

namespace ScholarRescue.Services
{
    public class SupportTicketService : ISupportTicketService
    {
        private readonly ScholarRescueDbContext _context;
        private readonly INotificationService _notificationService;
        private readonly ILogger<SupportTicketService> _logger;

        private static readonly Dictionary<SupportDepartment, string> DepartmentQueues = new()
        {
            { SupportDepartment.GeneralSupport, "General Queue" },
            { SupportDepartment.Orders, "Orders Queue" },
            { SupportDepartment.WriterApplications, "Writer Review Queue" },
            { SupportDepartment.BillingPayments, "Billing Queue" },
            { SupportDepartment.DisputesCompliance, "Disputes Queue" },
            { SupportDepartment.TechnicalSupport, "Technical Queue" },
            { SupportDepartment.Administration, "Admin Queue" },
        };

        private static readonly Dictionary<SupportDepartment, string> DepartmentDisplayNames = new()
        {
            { SupportDepartment.GeneralSupport, "General Support" },
            { SupportDepartment.Orders, "Orders Department" },
            { SupportDepartment.WriterApplications, "Writer Applications" },
            { SupportDepartment.BillingPayments, "Billing & Payments" },
            { SupportDepartment.DisputesCompliance, "Disputes & Compliance" },
            { SupportDepartment.TechnicalSupport, "Technical Support" },
            { SupportDepartment.Administration, "Administration" },
        };

        public SupportTicketService(
            ScholarRescueDbContext context,
            INotificationService notificationService,
            ILogger<SupportTicketService> logger)
        {
            _context = context;
            _notificationService = notificationService;
            _logger = logger;
        }

        public static string GetDepartmentDisplayName(SupportDepartment dept) =>
            DepartmentDisplayNames.GetValueOrDefault(dept, dept.ToString());

        public static string GetDepartmentQueue(SupportDepartment dept) =>
            DepartmentQueues.GetValueOrDefault(dept, "General Queue");

        public async Task<List<SupportTicket>> GetTicketsAsync(string? userId = null, SupportDepartment? department = null, TicketStatus? status = null)
        {
            var query = _context.SupportTickets
                .Include(t => t.Creator)
                .Include(t => t.AssignedAdmin)
                .AsQueryable();
            if (!string.IsNullOrEmpty(userId)) query = query.Where(t => t.CreatorId == userId);
            if (department.HasValue) query = query.Where(t => t.Department == department.Value);
            if (status.HasValue) query = query.Where(t => t.Status == status.Value);
            return await query.OrderByDescending(t => t.CreatedAt).AsNoTracking().ToListAsync();
        }

        public async Task<SupportTicket?> GetByIdAsync(int id)
        {
            return await _context.SupportTickets
                .Include(t => t.Creator)
                .Include(t => t.AssignedAdmin)
                .Include(t => t.Order)
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == id);
        }

        public async Task<string> GetNextTicketNumberAsync()
        {
            var last = await _context.SupportTickets.OrderByDescending(t => t.Id).FirstOrDefaultAsync();
            int next = 100001;
            if (last != null && !string.IsNullOrEmpty(last.TicketNumber))
            {
                var parts = last.TicketNumber.Split('-');
                if (parts.Length == 2 && int.TryParse(parts[1], out int num)) next = num + 1;
            }
            return $"SUP-{next}";
        }

        public async Task<SupportTicket> CreateTicketAsync(string subject, string description, SupportDepartment department, string creatorId, int? orderId)
        {
            var ticket = new SupportTicket
            {
                TicketNumber = await GetNextTicketNumberAsync(),
                Subject = subject,
                Description = description,
                Department = department,
                Status = TicketStatus.Open,
                Priority = TicketPriority.Normal,
                QueueName = GetDepartmentQueue(department),
                CreatorId = creatorId,
                OrderId = orderId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                LastReplyDate = DateTime.UtcNow,
                UnreadCount = 0,
                IsReopened = false
            };
            _context.SupportTickets.Add(ticket);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Support ticket {Number} created by {User} in department {Dept}.", ticket.TicketNumber, creatorId, department);

            // Notify admins about new ticket
            var admins = await _context.Users.Where(u => u.UserType == "Administrator").ToListAsync();
            foreach (var admin in admins)
            {
                await _notificationService.CreateNotificationAsync(
                    admin.Id,
                    $"New Support Ticket: #{ticket.TicketNumber}",
                    $"A new ticket has been created in {GetDepartmentDisplayName(department)}: {subject}",
                    NotificationType.SystemAlert,
                    ticket.Id.ToString(),
                    "SupportTicket");
            }

            // Audit Log
            _context.AuditLogs.Add(new AuditLog
            {
                Action = "Ticket Created",
                PerformedById = creatorId,
                Description = $"Ticket #{ticket.TicketNumber} created in {GetDepartmentDisplayName(department)} - Subject: {subject}",
                CreatedDate = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();

            return ticket;
        }

        public async Task<SupportTicketNote> AddNoteAsync(int ticketId, string content, string authorId, bool isInternal = false)
        {
            var ticket = await _context.SupportTickets.FindAsync(ticketId)
                ?? throw new InvalidOperationException("Ticket not found.");

            var note = new SupportTicketNote
            {
                TicketId = ticketId,
                Content = content,
                AuthorId = authorId,
                IsInternal = isInternal,
                CreatedAt = DateTime.UtcNow
            };
            _context.SupportTicketNotes.Add(note);

            ticket.UpdatedAt = DateTime.UtcNow;
            ticket.LastReplyDate = DateTime.UtcNow;

            if (!isInternal)
            {
                ticket.UnreadCount++;
            }

            await _context.SaveChangesAsync();

            // Notify the ticket creator if reply is from someone else
            if (!isInternal && ticket.CreatorId != authorId)
            {
                var ticketWithCreator = await _context.SupportTickets
                    .Include(t => t.Creator)
                    .FirstOrDefaultAsync(t => t.Id == ticketId);

                if (ticketWithCreator?.Creator != null)
                {
                    await _notificationService.CreateNotificationAsync(
                        ticketWithCreator.CreatorId,
                        $"New Reply on Ticket #{ticket.TicketNumber}",
                        $"You have received a new reply on ticket {ticket.TicketNumber}",
                        NotificationType.SystemAlert,
                        ticketId.ToString(),
                        "SupportTicket");
                }
            }

            // Notify admins if reply is from creator
            if (!isInternal && ticket.CreatorId == authorId)
            {
                var admins = await _context.Users.Where(u => u.UserType == "Administrator").ToListAsync();
                foreach (var admin in admins)
                {
                    await _notificationService.CreateNotificationAsync(
                        admin.Id,
                        $"New Reply on Ticket #{ticket.TicketNumber}",
                        $"A new reply has been received on ticket {ticket.TicketNumber}",
                        NotificationType.SystemAlert,
                        ticketId.ToString(),
                        "SupportTicket");
                }
            }

            // Audit log
            _context.AuditLogs.Add(new AuditLog
            {
                Action = isInternal ? "Internal Note Added" : "Reply Sent",
                PerformedById = authorId,
                TargetUserId = ticket.CreatorId,
                Description = $"{(isInternal ? "Internal note" : "Reply")} added to ticket #{ticket.TicketNumber}",
                CreatedDate = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();

            return note;
        }

        public async Task<SupportTicket> ChangeStatusAsync(int ticketId, TicketStatus newStatus, string adminId)
        {
            var ticket = await _context.SupportTickets.FindAsync(ticketId)
                ?? throw new InvalidOperationException("Ticket not found.");

            var oldStatus = ticket.Status;
            ticket.Status = newStatus;
            ticket.UpdatedAt = DateTime.UtcNow;

            if (newStatus == TicketStatus.Resolved)
                ticket.ResolvedAt = DateTime.UtcNow;
            else if (newStatus == TicketStatus.Closed)
                ticket.ResolvedAt = DateTime.UtcNow;

            if (string.IsNullOrEmpty(ticket.AssignedAdminId))
                ticket.AssignedAdminId = adminId;

            await _context.SaveChangesAsync();

            // Notify the creator
            if (ticket.CreatorId != adminId)
            {
                await _notificationService.CreateNotificationAsync(
                    ticket.CreatorId,
                    $"Ticket #{ticket.TicketNumber} Status Updated",
                    $"Your ticket status has changed from {oldStatus} to {newStatus}.",
                    NotificationType.SystemAlert,
                    ticketId.ToString(),
                    "SupportTicket");
            }

            // Audit log
            _context.AuditLogs.Add(new AuditLog
            {
                Action = "Ticket Status Changed",
                PerformedById = adminId,
                TargetUserId = ticket.CreatorId,
                Description = $"Ticket #{ticket.TicketNumber} status changed from {oldStatus} to {newStatus}",
                CreatedDate = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();

            return ticket;
        }

        public async Task<SupportTicket> AssignAdminAsync(int ticketId, string adminId)
        {
            var ticket = await _context.SupportTickets.FindAsync(ticketId)
                ?? throw new InvalidOperationException("Ticket not found.");

            var previousAdminId = ticket.AssignedAdminId;
            ticket.AssignedAdminId = adminId;
            ticket.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Notify creator
            await _notificationService.CreateNotificationAsync(
                ticket.CreatorId,
                $"Ticket #{ticket.TicketNumber} Assigned",
                $"Your ticket has been assigned to an administrator.",
                NotificationType.SystemAlert,
                ticketId.ToString(),
                "SupportTicket");

            // Audit log
            var action = string.IsNullOrEmpty(previousAdminId) ? "Ticket Assigned" : "Ticket Reassigned";
            _context.AuditLogs.Add(new AuditLog
            {
                Action = action,
                PerformedById = adminId,
                TargetUserId = ticket.CreatorId,
                Description = $"Ticket #{ticket.TicketNumber} assigned to admin {adminId}",
                CreatedDate = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();

            return ticket;
        }

        public async Task<SupportTicketAssignmentResult> TransferDepartmentAsync(int ticketId, SupportDepartment newDepartment, string adminId)
        {
            var ticket = await _context.SupportTickets.FindAsync(ticketId)
                ?? throw new InvalidOperationException("Ticket not found.");

            var oldDepartment = ticket.Department;
            ticket.Department = newDepartment;
            ticket.QueueName = GetDepartmentQueue(newDepartment);
            ticket.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Audit log
            _context.AuditLogs.Add(new AuditLog
            {
                Action = "Ticket Reassigned",
                PerformedById = adminId,
                TargetUserId = ticket.CreatorId,
                Description = $"Ticket #{ticket.TicketNumber} transferred from {GetDepartmentDisplayName(oldDepartment)} to {GetDepartmentDisplayName(newDepartment)}",
                CreatedDate = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();

            return new SupportTicketAssignmentResult
            {
                Ticket = ticket,
                AssignedQueue = ticket.QueueName ?? GetDepartmentQueue(newDepartment)
            };
        }

        public async Task<SupportTicket> ReopenTicketAsync(int ticketId, string userId)
        {
            var ticket = await _context.SupportTickets.FindAsync(ticketId)
                ?? throw new InvalidOperationException("Ticket not found.");

            ticket.Status = TicketStatus.Open;
            ticket.IsReopened = true;
            ticket.ResolvedAt = null;
            ticket.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Notify admins
            var admins = await _context.Users.Where(u => u.UserType == "Administrator").ToListAsync();
            foreach (var admin in admins)
            {
                await _notificationService.CreateNotificationAsync(
                    admin.Id,
                    $"Ticket #{ticket.TicketNumber} Reopened",
                    $"Ticket {ticket.TicketNumber} has been reopened by user {userId}.",
                    NotificationType.SystemAlert,
                    ticketId.ToString(),
                    "SupportTicket");
            }

            // Audit log
            _context.AuditLogs.Add(new AuditLog
            {
                Action = "Ticket Reopened",
                PerformedById = userId,
                TargetUserId = ticket.CreatorId,
                Description = $"Ticket #{ticket.TicketNumber} reopened",
                CreatedDate = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();

            return ticket;
        }

        public async Task<SupportTicketAttachment> AddAttachmentAsync(int ticketId, string fileName, string filePath, long fileSize, string? uploadedById)
        {
            var attachment = new SupportTicketAttachment
            {
                TicketId = ticketId,
                FileName = fileName,
                FilePath = filePath,
                FileSizeBytes = fileSize,
                UploadedById = uploadedById,
                UploadedAt = DateTime.UtcNow
            };
            _context.SupportTicketAttachments.Add(attachment);

            var ticket = await _context.SupportTickets.FindAsync(ticketId);
            if (ticket != null)
            {
                ticket.UpdatedAt = DateTime.UtcNow;
                ticket.LastReplyDate = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            // Audit log
            _context.AuditLogs.Add(new AuditLog
            {
                Action = "Attachment Uploaded",
                PerformedById = uploadedById ?? "system",
                Description = $"File '{fileName}' uploaded to ticket #{ticket?.TicketNumber ?? ticketId.ToString()}",
                CreatedDate = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();

            return attachment;
        }

        public async Task<List<SupportTicketNote>> GetNotesAsync(int ticketId)
        {
            return await _context.SupportTicketNotes
                .Include(n => n.Author)
                .Where(n => n.TicketId == ticketId)
                .OrderBy(n => n.CreatedAt)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<List<SupportTicketAttachment>> GetAttachmentsAsync(int ticketId)
        {
            return await _context.SupportTicketAttachments
                .Where(a => a.TicketId == ticketId)
                .OrderBy(a => a.UploadedAt)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<int> GetOpenTicketCountAsync(string userId)
        {
            return await _context.SupportTickets
                .Where(t => t.CreatorId == userId &&
                    t.Status != TicketStatus.Resolved &&
                    t.Status != TicketStatus.Closed)
                .CountAsync();
        }

        public async Task<List<SupportTicket>> SearchTicketsAsync(string searchTerm, string? userId = null)
        {
            var term = searchTerm.Trim().ToLowerInvariant();
            var query = _context.SupportTickets
                .Include(t => t.Creator)
                .Include(t => t.AssignedAdmin)
                .AsQueryable();

            if (!string.IsNullOrEmpty(userId))
                query = query.Where(t => t.CreatorId == userId);

            return await query
                .Where(t =>
                    t.TicketNumber.ToLower().Contains(term) ||
                    t.Subject.ToLower().Contains(term) ||
                    t.Department.ToString().ToLower().Contains(term) ||
                    t.Status.ToString().ToLower().Contains(term) ||
                    (t.QueueName != null && t.QueueName.ToLower().Contains(term)))
                .OrderByDescending(t => t.CreatedAt)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<int> GetTicketCountByStatusAsync(TicketStatus status)
        {
            return await _context.SupportTickets
                .Where(t => t.Status == status)
                .CountAsync();
        }

        public async Task MarkTicketAsReadAsync(int ticketId, string userId)
        {
            var ticket = await _context.SupportTickets.FindAsync(ticketId);
            if (ticket != null && ticket.CreatorId == userId)
            {
                ticket.UnreadCount = 0;
                await _context.SaveChangesAsync();
            }
        }
    }
}