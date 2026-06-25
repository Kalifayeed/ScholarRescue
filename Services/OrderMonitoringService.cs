using Microsoft.EntityFrameworkCore;
using ScholarRescue.Data;
using ScholarRescue.Models;
using ScholarRescue.Models.Enums;

namespace ScholarRescue.Services
{
    /// <summary>
    /// Implements the Order Monitoring Engine. Detects problematic order conditions
    /// (no applicants, inactive writers, overdue milestones/revisions) and generates
    /// alerts for the admin escalation dashboard.
    /// </summary>
    public class OrderMonitoringService : IOrderMonitoringService
    {
        private readonly ScholarRescueDbContext _context;
        private readonly INotificationService _notificationService;
        private readonly ILogger<OrderMonitoringService> _logger;

        public OrderMonitoringService(
            ScholarRescueDbContext context,
            INotificationService notificationService,
            ILogger<OrderMonitoringService> logger)
        {
            _context = context;
            _notificationService = notificationService;
            _logger = logger;
        }

        public async Task RunMonitoringCheckAsync()
        {
            _logger.LogInformation("Starting order monitoring check at {Time}.", DateTime.UtcNow);

            await CheckNoApplicantsAfter2HoursAsync();
            await CheckUrgentNoApplicantsAfter30MinAsync();
            await CheckWriterInactive24HoursAsync();
            await CheckMilestonesOverdueAsync();
            await CheckRevisionsOverdueAsync();

            _logger.LogInformation("Order monitoring check completed at {Time}.", DateTime.UtcNow);
        }

        /// <summary>
        /// Orders open in the marketplace for 2+ hours with zero applications.
        /// </summary>
        private async Task CheckNoApplicantsAfter2HoursAsync()
        {
            var threshold = DateTime.UtcNow.AddHours(-2);
            var staleOrders = await _context.Orders
                .Include(o => o.Applications)
                .Where(o => o.Status == OrderStatus.Open
                    && o.IsMarketplaceOpen
                    && o.CreatedAt <= threshold
                    && o.Applications.Count == 0)
                .ToListAsync();

            foreach (var order in staleOrders)
            {
                await CreateAlertIfNotDuplicateAsync(
                    MonitoringAlertType.NoApplicantsAfter2Hours,
                    order.Id,
                    null,
                    $"Order {order.OrderNumber} has been in the marketplace for over 2 hours with no applications.",
                    $"Order {order.OrderNumber} - No Applicants"
                );
            }
        }

        /// <summary>
        /// Urgent/high priority orders open in the marketplace for 30+ minutes with zero applications.
        /// </summary>
        private async Task CheckUrgentNoApplicantsAfter30MinAsync()
        {
            var threshold = DateTime.UtcNow.AddMinutes(-30);
            var urgentStaleOrders = await _context.Orders
                .Include(o => o.Applications)
                .Where(o => o.Status == OrderStatus.Open
                    && o.IsMarketplaceOpen
                    && (o.Priority == PriorityLevel.Urgent || o.Priority == PriorityLevel.High)
                    && o.CreatedAt <= threshold
                    && o.Applications.Count == 0)
                .ToListAsync();

            foreach (var order in urgentStaleOrders)
            {
                await CreateAlertIfNotDuplicateAsync(
                    MonitoringAlertType.UrgentNoApplicantsAfter30Min,
                    order.Id,
                    null,
                    $"Urgent order {order.OrderNumber} has been in the marketplace for over 30 minutes with no applications.",
                    $"Urgent Order {order.OrderNumber} - No Applicants"
                );
            }
        }

        /// <summary>
        /// Assigned writers who haven't updated the order in 24+ hours.
        /// </summary>
        private async Task CheckWriterInactive24HoursAsync()
        {
            var threshold = DateTime.UtcNow.AddHours(-24);
            var inactiveWriterOrders = await _context.Orders
                .Include(o => o.AssignedWriter)
                .Where(o => o.Status == OrderStatus.Assigned || o.Status == OrderStatus.InProgress)
                .ToListAsync();

            foreach (var order in inactiveWriterOrders)
            {
                // Check last activity: either order UpdatedAt or writer LastActivityDate
                var writer = order.AssignedWriter;
                if (writer == null) continue;

                var lastActivity = writer.LastActivityDate ?? order.UpdatedAt;
                if (lastActivity <= threshold)
                {
                    await CreateAlertIfNotDuplicateAsync(
                        MonitoringAlertType.WriterInactive24Hours,
                        order.Id,
                        writer.Id,
                        $"Writer {writer.FirstName} {writer.LastName} has been inactive on order {order.OrderNumber} for over 24 hours (last activity: {lastActivity:yyyy-MM-dd HH:mm}).",
                        $"Order {order.OrderNumber} - Writer Inactive"
                    );
                }
            }
        }

        /// <summary>
        /// Milestones past their deadline with Pending status.
        /// </summary>
        private async Task CheckMilestonesOverdueAsync()
        {
            var now = DateTime.UtcNow;
            var overdueMilestones = await _context.OrderMilestones
                .Include(m => m.Order)
                .ThenInclude(o => o.AssignedWriter)
                .Where(m => m.Status == MilestoneStatus.Pending
                    && m.Deadline <= now)
                .ToListAsync();

            foreach (var milestone in overdueMilestones)
            {
                var order = milestone.Order;
                await CreateAlertIfNotDuplicateAsync(
                    MonitoringAlertType.MilestoneOverdue,
                    order.Id,
                    order.AssignedWriterId,
                    $"Milestone '{milestone.Title}' for order {order.OrderNumber} is overdue (deadline was {milestone.Deadline:yyyy-MM-dd HH:mm}).",
                    $"Order {order.OrderNumber} - Milestone Overdue: {milestone.Title}",
                    milestone.Id
                );
            }
        }

        /// <summary>
        /// Revision requests that are still Pending (not completed) after the deadline.
        /// We consider 48 hours from request as the implicit deadline.
        /// </summary>
        private async Task CheckRevisionsOverdueAsync()
        {
            var threshold = DateTime.UtcNow.AddHours(-48);
            var overdueRevisions = await _context.RevisionRequests
                .Include(r => r.Order)
                .ThenInclude(o => o.AssignedWriter)
                .Where(r => r.Status == RevisionRequestStatus.Pending
                    && r.RequestedAt <= threshold)
                .ToListAsync();

            foreach (var revision in overdueRevisions)
            {
                var order = revision.Order;
                await CreateAlertIfNotDuplicateAsync(
                    MonitoringAlertType.RevisionOverdue,
                    order.Id,
                    order.AssignedWriterId,
                    $"Revision request for order {order.OrderNumber} is overdue (requested {revision.RequestedAt:yyyy-MM-dd HH:mm}, still pending after 48 hours).",
                    $"Order {order.OrderNumber} - Revision Overdue"
                );
            }
        }

        /// <summary>
        /// Creates an alert only if an identical unresolved one doesn't already exist.
        /// </summary>
        private async Task CreateAlertIfNotDuplicateAsync(
            MonitoringAlertType type,
            int orderId,
            string? writerId,
            string description,
            string notificationTitle,
            int? milestoneId = null)
        {
            var existing = await _context.MonitoringAlerts
                .AnyAsync(a => a.AlertType == type
                    && a.OrderId == orderId
                    && !a.IsAcknowledged
                    && a.ResolvedAt == null);

            if (existing) return;

            var alert = new MonitoringAlert
            {
                AlertType = type,
                OrderId = orderId,
                WriterId = writerId,
                MilestoneId = milestoneId,
                Description = description,
                CreatedAt = DateTime.UtcNow
            };

            _context.MonitoringAlerts.Add(alert);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Monitoring alert created: {Type} for order {OrderId}.", type, orderId);
        }

        // ──────────────────────────────────────────────
        // Query Methods
        // ──────────────────────────────────────────────

        public async Task<List<MonitoringAlert>> GetActiveAlertsAsync()
        {
            return await _context.MonitoringAlerts
                .Include(a => a.Order)
                .Include(a => a.Writer)
                .Where(a => !a.IsAcknowledged && a.ResolvedAt == null)
                .OrderByDescending(a => a.CreatedAt)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<List<MonitoringAlert>> GetAllAlertsAsync(
            MonitoringAlertType? typeFilter = null, bool? acknowledged = null)
        {
            IQueryable<MonitoringAlert> query = _context.MonitoringAlerts
                .Include(a => a.Order)
                .Include(a => a.Writer)
                .Include(a => a.AcknowledgedBy);

            if (typeFilter.HasValue)
                query = query.Where(a => a.AlertType == typeFilter.Value);

            if (acknowledged.HasValue)
                query = query.Where(a => a.IsAcknowledged == acknowledged.Value);

            return await query
                .OrderByDescending(a => a.CreatedAt)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task AcknowledgeAlertAsync(int alertId, string adminId)
        {
            var alert = await _context.MonitoringAlerts.FindAsync(alertId);
            if (alert == null)
                throw new InvalidOperationException("Alert not found.");

            alert.IsAcknowledged = true;
            alert.AcknowledgedById = adminId;
            alert.AcknowledgedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        public async Task ResolveAlertAsync(int alertId)
        {
            var alert = await _context.MonitoringAlerts.FindAsync(alertId);
            if (alert == null)
                throw new InvalidOperationException("Alert not found.");

            alert.ResolvedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        public async Task<int> GetActiveAlertCountAsync()
        {
            return await _context.MonitoringAlerts
                .CountAsync(a => !a.IsAcknowledged && a.ResolvedAt == null);
        }
    }
}