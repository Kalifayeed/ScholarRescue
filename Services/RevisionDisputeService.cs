using Microsoft.EntityFrameworkCore;
using ScholarRescue.Data;
using ScholarRescue.Models;
using ScholarRescue.Models.Enums;

namespace ScholarRescue.Services
{
    /// <summary>
    /// Manages revisions and disputes with escrow locking, notifications, and audit logging.
    /// </summary>
    public class RevisionDisputeService : IRevisionDisputeService
    {
        private readonly ScholarRescueDbContext _context;
        private readonly INotificationService _notificationService;
        private readonly IEscrowService _escrowService;
        private readonly IOrderTimelineService _timelineService;
        private readonly ILogger<RevisionDisputeService> _logger;

        public RevisionDisputeService(
            ScholarRescueDbContext context,
            INotificationService notificationService,
            IEscrowService escrowService,
            IOrderTimelineService timelineService,
            ILogger<RevisionDisputeService> logger)
        {
            _context = context;
            _notificationService = notificationService;
            _escrowService = escrowService;
            _timelineService = timelineService;
            _logger = logger;
        }

        // ============================================================
        // REVISIONS
        // ============================================================

        public async Task<Models.RevisionRequest> RequestRevisionAsync(int orderId, string clientId, string title, string description)
        {
            var order = await _context.Orders.Include(o => o.AssignedWriter).FirstOrDefaultAsync(o => o.Id == orderId);
            if (order == null) throw new InvalidOperationException("Order not found.");
            if (order.AssignedWriterId == null) throw new InvalidOperationException("No writer assigned.");

            var revision = new Models.RevisionRequest
            {
                OrderId = orderId,
                ClientId = clientId,
                WriterId = order.AssignedWriterId,
                Comments = $"{title}\n\n{description}",
                Status = RevisionRequestStatus.Pending,
                RequestedAt = DateTime.UtcNow
            };
            _context.RevisionRequests.Add(revision);
            await _context.SaveChangesAsync();

            await _notificationService.CreateNotificationAsync(order.AssignedWriterId,
                "Revision Requested", $"Client requested revisions for order {order.OrderNumber}: {title}",
                NotificationType.RevisionRequested, orderId.ToString(), "Order");

            await _timelineService.AddEventAsync(orderId, clientId, "Client", TimelineEventType.RevisionRequested,
                "Revision Requested", title);

            _context.AuditLogs.Add(new AuditLog { Action = "Revision Created", PerformedById = clientId, TargetUserId = order.AssignedWriterId, Description = $"Revision for order {order.OrderNumber}", CreatedDate = DateTime.UtcNow });
            await _context.SaveChangesAsync();

            return revision;
        }

        public async Task<Models.RevisionRequest> SubmitRevisionAsync(int revisionId, string writerId)
        {
            var revision = await _context.RevisionRequests.Include(r => r.Order).FirstOrDefaultAsync(r => r.Id == revisionId);
            if (revision == null) throw new InvalidOperationException("Revision not found.");

            revision.Status = RevisionRequestStatus.Completed;
            await _context.SaveChangesAsync();

            await _notificationService.CreateNotificationAsync(revision.ClientId,
                "Revision Submitted", $"Writer has submitted revisions for order {revision.Order?.OrderNumber}.",
                NotificationType.RevisionRequested, revision.OrderId.ToString(), "Order");

            await _timelineService.AddEventAsync(revision.OrderId, writerId, "Writer", TimelineEventType.RevisionSubmitted,
                "Revision Submitted", "");

            return revision;
        }

        public async Task<Models.RevisionRequest> ApproveRevisionAsync(int revisionId, string clientId)
        {
            var revision = await _context.RevisionRequests.FindAsync(revisionId);
            if (revision == null) throw new InvalidOperationException("Revision not found.");
            revision.Status = RevisionRequestStatus.Completed;
            await _context.SaveChangesAsync();
            return revision;
        }

        public async Task<List<Models.RevisionRequest>> GetOrderRevisionsAsync(int orderId) =>
            await _context.RevisionRequests.Where(r => r.OrderId == orderId).OrderByDescending(r => r.RequestedAt).ToListAsync();

        // ============================================================
        // DISPUTES
        // ============================================================

        public async Task<OrderDispute> OpenDisputeAsync(int orderId, string clientId, string title, string description, string disputeType)
        {
            var order = await _context.Orders.Include(o => o.AssignedWriter).FirstOrDefaultAsync(o => o.Id == orderId);
            if (order == null) throw new InvalidOperationException("Order not found.");
            if (order.AssignedWriterId == null) throw new InvalidOperationException("No writer assigned.");

            var dispute = new OrderDispute
            {
                OrderId = orderId, ClientId = clientId, WriterId = order.AssignedWriterId,
                Title = title, Description = description, DisputeType = disputeType,
                Status = "Open", OpenedAt = DateTime.UtcNow
            };
            _context.Set<OrderDispute>().Add(dispute);

            // Lock escrow
            try { await _escrowService.LockEscrowForDisputeAsync(orderId); } catch { }

            // Audit
            _context.AuditLogs.Add(new AuditLog { Action = "Dispute Opened", PerformedById = clientId, TargetUserId = order.AssignedWriterId, Description = $"Dispute on order {order.OrderNumber}: {title}", CreatedDate = DateTime.UtcNow });
            await _context.SaveChangesAsync();

            // Notifications
            await _notificationService.CreateNotificationAsync(order.AssignedWriterId, "Dispute Opened", $"A dispute has been opened on order {order.OrderNumber}.", NotificationType.DisputeOpened, orderId.ToString(), "Order");
            await _notificationService.CreateNotificationAsync(clientId, "Dispute Opened", $"Your dispute on order {order.OrderNumber} has been opened.", NotificationType.DisputeOpened, orderId.ToString(), "Order");

            var admins = await _context.Users.Where(u => u.UserType == "Administrator" && u.IsActive).ToListAsync();
            foreach (var a in admins)
                await _notificationService.CreateNotificationAsync(a.Id, "Dispute Requires Review", $"Dispute opened on order {order.OrderNumber}.", NotificationType.DisputeOpened, orderId.ToString(), "Order");

            await _timelineService.AddEventAsync(orderId, clientId, "Client", TimelineEventType.SystemAction, "Dispute Opened", $"{disputeType}: {title}");
            return dispute;
        }

        public async Task<OrderDispute> ResolveDisputeAsync(int disputeId, string adminId, string resolution, string decision)
        {
            var dispute = await _context.Set<OrderDispute>().Include(d => d.Order).FirstOrDefaultAsync(d => d.Id == disputeId);
            if (dispute == null) throw new InvalidOperationException("Dispute not found.");

            dispute.Status = "Resolved";
            dispute.Resolution = resolution;
            dispute.ResolvedAt = DateTime.UtcNow;
            dispute.ResolvedByAdminId = adminId;

            // If full refund to client
            if (decision == "FullRefund" || decision == "ClientApproved")
            {
                try { await _escrowService.RefundEscrowAsync(dispute.OrderId, adminId); } catch { }
            }

            await _context.SaveChangesAsync();

            await _notificationService.CreateNotificationAsync(dispute.ClientId, "Dispute Resolved", $"Your dispute on order {dispute.Order?.OrderNumber} has been resolved.", NotificationType.DisputeResolved, dispute.OrderId.ToString(), "Order");
            await _notificationService.CreateNotificationAsync(dispute.WriterId, "Dispute Resolved", $"The dispute on order {dispute.Order?.OrderNumber} has been resolved.", NotificationType.DisputeResolved, dispute.OrderId.ToString(), "Order");

            return dispute;
        }

        public async Task<OrderDispute?> GetDisputeAsync(int orderId) =>
            await _context.Set<OrderDispute>().Include(d => d.Client).Include(d => d.Writer).FirstOrDefaultAsync(d => d.OrderId == orderId);

        public async Task<List<OrderDispute>> GetAllDisputesAsync(string? status = null)
        {
            IQueryable<OrderDispute> query = _context.Set<OrderDispute>().Include(d => d.Order).Include(d => d.Client).Include(d => d.Writer);
            if (!string.IsNullOrWhiteSpace(status)) query = query.Where(d => d.Status == status);
            return await query.OrderByDescending(d => d.OpenedAt).ToListAsync();
        }

        public async Task<DisputeEvidence> AddEvidenceAsync(int disputeId, string uploadedBy, string fileName, string filePath, string? description)
        {
            var evidence = new DisputeEvidence { DisputeId = disputeId, UploadedBy = uploadedBy, FileName = fileName, FilePath = filePath, Description = description };
            _context.Set<DisputeEvidence>().Add(evidence);
            await _context.SaveChangesAsync();
            return evidence;
        }

        public async Task<List<DisputeEvidence>> GetEvidenceAsync(int disputeId) =>
            await _context.Set<DisputeEvidence>().Where(e => e.DisputeId == disputeId).ToListAsync();
    }
}