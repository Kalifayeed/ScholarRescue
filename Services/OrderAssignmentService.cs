using Microsoft.EntityFrameworkCore;
using ScholarRescue.Data;
using ScholarRescue.Models;
using ScholarRescue.Models.Enums;

namespace ScholarRescue.Services
{
    /// <summary>
    /// Service interface for the order assignment workflow: writer applications,
    /// admin assignment / rejection / reassignment, and marketplace visibility.
    /// </summary>
    public interface IOrderAssignmentService
    {
        /// <summary>
        /// Lists all orders currently visible in the Available Orders marketplace.
        /// Excludes already-assigned, cancelled, and completed orders.
        /// </summary>
        Task<List<TutoringRequest>> GetAvailableOrdersAsync();

        /// <summary>
        /// Lists available orders with filtering and sorting for the marketplace.
        /// </summary>
        Task<List<TutoringRequest>> GetAvailableOrdersFilteredAsync(
            string? discipline = null,
            AcademicLevel? academicLevel = null,
            int? minPages = null,
            int? maxPages = null,
            DateTime? deadlineFrom = null,
            DateTime? deadlineTo = null,
            decimal? minPrice = null,
            decimal? maxPrice = null,
            CitationFormat? citationStyle = null,
            PriorityLevel? urgency = null,
            RequestType? requestType = null,
            string sortBy = "newest");

        /// <summary>
        /// Returns the writer applications for a specific order.
        /// </summary>
        Task<List<OrderApplication>> GetApplicationsForOrderAsync(int orderId);

        /// <summary>
        /// Returns the writer's own applications to orders.
        /// </summary>
        Task<List<OrderApplication>> GetApplicationsByWriterAsync(string writerId);

        /// <summary>
        /// Allows an approved writer to apply to an open order.
        /// </summary>
        Task<OrderApplication> ApplyForOrderAsync(int orderId, string writerId, string? message);

        /// <summary>
        /// Allows the writer to withdraw a pending application.
        /// </summary>
        Task WithdrawApplicationAsync(int applicationId, string writerId);

        /// <summary>
        /// Assigns the order to the given writer (admin action).
        /// </summary>
        Task AssignWriterAsync(int orderId, string writerId, string adminId);

        /// <summary>
        /// Rejects a writer's application to a specific order (admin action).
        /// </summary>
        Task RejectApplicationAsync(int applicationId, string adminId);

        /// <summary>
        /// Removes the assigned writer and returns the order to the marketplace.
        /// </summary>
        Task ReassignOrderAsync(int orderId, string adminId);
    }

    /// <summary>
    /// Default implementation of <see cref="IOrderAssignmentService"/>.
    /// </summary>
    public class OrderAssignmentService : IOrderAssignmentService
    {
        /// <summary>
        /// Cutoff UTC timestamp for enforcing the StudentDraft attachment requirement.
        /// Orders created before this timestamp are grandfathered — they can be assigned
        /// without a StudentDraft attachment.
        /// Set to 2026-07-05T07:00:00Z (deployment time of this enforcement fix).
        /// </summary>
        public static readonly DateTime AttachmentEnforcementCutoffUtc =
            new DateTime(2026, 7, 5, 7, 0, 0, DateTimeKind.Utc);

        private readonly ScholarRescueDbContext _context;
        private readonly INotificationService _notificationService;
        private readonly ILogger<OrderAssignmentService> _logger;

        public OrderAssignmentService(
            ScholarRescueDbContext context,
            INotificationService notificationService,
            ILogger<OrderAssignmentService> logger)
        {
            _context = context;
            _notificationService = notificationService;
            _logger = logger;
        }

        public async Task<List<TutoringRequest>> GetAvailableOrdersAsync()
        {
            return await _context.Orders
                .Include(o => o.Client)
                .Where(o => o.Status == OrderStatus.Open
                    && o.AssignedWriterId == null
                    && o.IsMarketplaceOpen
                    && o.Deadline > DateTime.UtcNow)
                .OrderByDescending(o => o.CreatedAt)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<List<TutoringRequest>> GetAvailableOrdersFilteredAsync(
            string? discipline = null,
            AcademicLevel? academicLevel = null,
            int? minPages = null,
            int? maxPages = null,
            DateTime? deadlineFrom = null,
            DateTime? deadlineTo = null,
            decimal? minPrice = null,
            decimal? maxPrice = null,
            CitationFormat? citationStyle = null,
            PriorityLevel? urgency = null,
            RequestType? requestType = null,
            string sortBy = "newest")
        {
            var query = _context.Orders
                .Include(o => o.Client)
                .Where(o => o.Status == OrderStatus.Open
                    && o.AssignedWriterId == null
                    && o.IsMarketplaceOpen
                    && o.Deadline > DateTime.UtcNow)
                .AsNoTracking();

            // Filtering
            if (!string.IsNullOrWhiteSpace(discipline))
                query = query.Where(o => o.Subject != null && o.Subject.Contains(discipline));

            if (academicLevel.HasValue)
                query = query.Where(o => o.AcademicLevel == academicLevel.Value);

            if (minPages.HasValue)
                query = query.Where(o => o.Pages >= minPages.Value);

            if (maxPages.HasValue)
                query = query.Where(o => o.Pages <= maxPages.Value);

            if (deadlineFrom.HasValue)
                query = query.Where(o => o.Deadline >= deadlineFrom.Value);

            if (deadlineTo.HasValue)
                query = query.Where(o => o.Deadline <= deadlineTo.Value);

            if (minPrice.HasValue)
                query = query.Where(o => o.Budget >= minPrice.Value);

            if (maxPrice.HasValue)
                query = query.Where(o => o.Budget <= maxPrice.Value);

            if (citationStyle.HasValue)
                query = query.Where(o => o.CitationFormat == citationStyle.Value);

            if (urgency.HasValue)
                query = query.Where(o => o.Priority == urgency.Value);

            if (requestType.HasValue)
                query = query.Where(o => o.RequestType == requestType.Value);

            // Sorting
            query = sortBy?.ToLower() switch
            {
                "highest_price" => query.OrderByDescending(o => o.Budget),
                "closest_deadline" => query.OrderBy(o => o.Deadline),
                "most_pages" => query.OrderByDescending(o => o.Pages),
                _ => query.OrderByDescending(o => o.CreatedAt) // "newest" (default)
            };

            return await query.ToListAsync();
        }

        public async Task<List<OrderApplication>> GetApplicationsForOrderAsync(int orderId)
        {
            return await _context.OrderApplications
                .Include(a => a.Writer)
                .Where(a => a.OrderId == orderId)
                .OrderByDescending(a => a.AppliedAt)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<List<OrderApplication>> GetApplicationsByWriterAsync(string writerId)
        {
            return await _context.OrderApplications
                .Include(a => a.Order)
                .Where(a => a.WriterId == writerId)
                .OrderByDescending(a => a.AppliedAt)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<OrderApplication> ApplyForOrderAsync(int orderId, string writerId, string? message)
        {
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == orderId);
            if (order == null)
                throw new InvalidOperationException("Order not found.");
            if (order.AssignedWriterId != null)
                throw new InvalidOperationException("Order has already been assigned to a writer.");
            if (order.Status != OrderStatus.Open || !order.IsMarketplaceOpen)
                throw new InvalidOperationException("This order is not accepting applications right now.");

            // Block duplicate applications
            var existing = await _context.OrderApplications
                .FirstOrDefaultAsync(a => a.OrderId == orderId && a.WriterId == writerId);
            if (existing != null)
                throw new InvalidOperationException("You have already applied to this order.");

            var application = new OrderApplication
            {
                OrderId = orderId,
                WriterId = writerId,
                AppliedAt = DateTime.UtcNow,
                Status = OrderApplicationStatus.Pending,
                Message = message
            };

            _context.OrderApplications.Add(application);

            _context.AuditLogs.Add(new AuditLog
            {
                Action = "Writer Applied to Order",
                PerformedById = writerId,
                TargetUserId = order.ClientId,
                Description = $"Writer applied to order {order.OrderNumber}.",
                CreatedDate = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            // Notify all administrators
            await _notificationService.CreateNotificationAsync(order.ClientId,
                "New Writer Application",
                $"A writer has applied for order {order.OrderNumber}.",
                NotificationType.WriterApplied,
                orderId.ToString());

            return application;
        }

        public async Task WithdrawApplicationAsync(int applicationId, string writerId)
        {
            var application = await _context.OrderApplications
                .FirstOrDefaultAsync(a => a.Id == applicationId && a.WriterId == writerId);
            if (application == null)
                throw new InvalidOperationException("Application not found.");
            if (application.Status != OrderApplicationStatus.Pending)
                throw new InvalidOperationException("Only pending applications can be withdrawn.");

            application.Status = OrderApplicationStatus.Withdrawn;
            application.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        public async Task AssignWriterAsync(int orderId, string writerId, string adminId)
        {
            var order = await _context.Orders
                .Include(o => o.Attachments)
                .FirstOrDefaultAsync(o => o.Id == orderId);
            if (order == null)
                throw new InvalidOperationException("Order not found.");

            if (order.AssignedWriterId != null && order.AssignedWriterId != writerId)
            {
                throw new InvalidOperationException(
                    "Order is already assigned. Remove the current writer before reassigning.");
            }

            // Guard: order must be paid/funded before assignment
            if (order.PaymentStatus != OrderPaymentStatus.Paid &&
                order.PaymentStatus != OrderPaymentStatus.EscrowFunded)
            {
                throw new InvalidOperationException(
                    "This order must be paid before assigning a writer.");
            }

            // Guard: enforce required draft attachment for applicable request types
            // Grandfathering: orders created before the cutoff are exempt.
            if (order.CreatedAt >= AttachmentEnforcementCutoffUtc && !order.HasRequiredDraftAttachment())
            {
                _logger.LogWarning("Attempted to assign order {OrderNumber} (created {CreatedAt}) of type {RequestType} without required StudentDraft attachment.",
                    order.OrderNumber, order.CreatedAt, order.RequestType);
                throw new InvalidOperationException(
                    "This order requires the client to upload their own work before a writer can be assigned. " +
                    "Please ensure the client has uploaded a draft.");
            }

            if (order.CreatedAt < AttachmentEnforcementCutoffUtc)
            {
                _logger.LogInformation("Order {OrderNumber} (created {CreatedAt}) is before attachment enforcement cutoff; grandfathering assignment.",
                    order.OrderNumber, order.CreatedAt);
            }

            // Make sure the writer is approved
            var latestApp = await _context.WriterApplications
                .Where(a => a.UserId == writerId)
                .OrderByDescending(a => a.SubmittedAt)
                .FirstOrDefaultAsync();
            if (latestApp == null || latestApp.Status != WriterApplicationStatus.Approved)
                throw new InvalidOperationException("Writer is not approved.");

            order.AssignedWriterId = writerId;
            order.AssignedAt = DateTime.UtcNow;
            order.AssignedByAdminId = adminId;
            order.Status = OrderStatus.Assigned;
            order.IsMarketplaceOpen = false;
            order.UpdatedAt = DateTime.UtcNow;

            // Mark this writer's application as Selected
            var writerApplication = await _context.OrderApplications
                .FirstOrDefaultAsync(a => a.OrderId == orderId && a.WriterId == writerId);
            if (writerApplication != null)
            {
                writerApplication.Status = OrderApplicationStatus.Selected;
                writerApplication.UpdatedAt = DateTime.UtcNow;
                writerApplication.ProcessedByAdminId = adminId;
            }

            // Mark all other applications as Declined
            var otherApplications = await _context.OrderApplications
                .Where(a => a.OrderId == orderId && a.WriterId != writerId &&
                    a.Status == OrderApplicationStatus.Pending)
                .ToListAsync();
            foreach (var a in otherApplications)
            {
                a.Status = OrderApplicationStatus.Declined;
                a.UpdatedAt = DateTime.UtcNow;
                a.ProcessedByAdminId = adminId;
            }

            _context.AuditLogs.Add(new AuditLog
            {
                Action = "Order Assigned",
                PerformedById = adminId,
                TargetUserId = writerId,
                Description =
                    $"Order {order.OrderNumber} assigned to writer {writerId} by admin {adminId}.",
                CreatedDate = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            await _notificationService.NotifyOrderAssignedAsync(order.Id, writerId, order.ClientId, order.OrderNumber);
        }

        public async Task RejectApplicationAsync(int applicationId, string adminId)
        {
            var application = await _context.OrderApplications
                .Include(a => a.Order)
                .FirstOrDefaultAsync(a => a.Id == applicationId);
            if (application == null)
                throw new InvalidOperationException("Application not found.");

            application.Status = OrderApplicationStatus.Declined;
            application.UpdatedAt = DateTime.UtcNow;
            application.ProcessedByAdminId = adminId;

            _context.AuditLogs.Add(new AuditLog
            {
                Action = "Writer Application Rejected (Order)",
                PerformedById = adminId,
                TargetUserId = application.WriterId,
                Description =
                    $"Writer's application for order {application.Order!.OrderNumber} was declined.",
                CreatedDate = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            await _notificationService.CreateNotificationAsync(
                application.WriterId,
                "Application Declined",
                $"Your application for order {application.Order.OrderNumber} was not selected.",
                NotificationType.WriterRejected,
                application.OrderId.ToString());
        }

        public async Task ReassignOrderAsync(int orderId, string adminId)
        {
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == orderId);
            if (order == null)
                throw new InvalidOperationException("Order not found.");

            string? previousWriterId = order.AssignedWriterId;
            order.AssignedWriterId = null;
            order.AssignedAt = null;
            order.AssignedByAdminId = null;
            order.Status = OrderStatus.Open;
            order.IsMarketplaceOpen = true;
            order.UpdatedAt = DateTime.UtcNow;

            // Reset all writer applications to Pending so they can re-apply
            var applications = await _context.OrderApplications
                .Where(a => a.OrderId == orderId)
                .ToListAsync();
            foreach (var a in applications)
            {
                a.Status = OrderApplicationStatus.Pending;
                a.UpdatedAt = DateTime.UtcNow;
            }

            _context.AuditLogs.Add(new AuditLog
            {
                Action = "Order Reassigned",
                PerformedById = adminId,
                TargetUserId = previousWriterId,
                Description =
                    $"Order {order.OrderNumber} was returned to the marketplace by admin {adminId}.",
                CreatedDate = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            await _notificationService.CreateNotificationAsync(
                order.ClientId,
                "Order Returned to Marketplace",
                $"Order {order.OrderNumber} has been returned to the marketplace and is open for new applications.",
                NotificationType.OrderReassigned,
                order.Id.ToString());
        }
    }
}
