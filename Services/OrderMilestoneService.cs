using Microsoft.EntityFrameworkCore;
using ScholarRescue.Data;
using ScholarRescue.Models;
using ScholarRescue.Models.Enums;

namespace ScholarRescue.Services
{
    /// <summary>
    /// Manages progressive delivery milestones: creation, submission, approval,
    /// and ledger integration. Required for 40+ page orders.
    /// </summary>
    public class OrderMilestoneService : IOrderMilestoneService
    {
        private readonly ScholarRescueDbContext _context;
        private readonly IFinancialService _financialService;
        private readonly INotificationService _notificationService;
        private readonly ILogger<OrderMilestoneService> _logger;

        public OrderMilestoneService(
            ScholarRescueDbContext context,
            IFinancialService financialService,
            INotificationService notificationService,
            ILogger<OrderMilestoneService> logger)
        {
            _context = context;
            _financialService = financialService;
            _notificationService = notificationService;
            _logger = logger;
        }

        public bool IsProgressiveDeliveryRequired(int pageCount) => pageCount >= 40;
        public bool IsProgressiveDeliveryOptional(int pageCount) => pageCount >= 20 && pageCount < 40;

        public async Task<List<OrderMilestone>> GetMilestonesAsync(int orderId)
        {
            return await _context.OrderMilestones
                .Where(m => m.OrderId == orderId)
                .OrderBy(m => m.SortOrder)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<OrderMilestone?> GetByIdAsync(int id)
        {
            return await _context.OrderMilestones
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id);
        }

        public async Task<List<OrderMilestoneFile>> GetFilesAsync(int milestoneId)
        {
            return await _context.OrderMilestoneFiles
                .Where(f => f.MilestoneId == milestoneId)
                .OrderBy(f => f.UploadedAt)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<int> GetNextSortOrderAsync(int orderId)
        {
            var max = await _context.OrderMilestones
                .Where(m => m.OrderId == orderId)
                .MaxAsync(m => (int?)m.SortOrder);
            return (max ?? -1) + 1;
        }

        public async Task<OrderMilestone> CreateMilestoneAsync(OrderMilestone milestone)
        {
            if (string.IsNullOrWhiteSpace(milestone.Title))
                throw new InvalidOperationException("Milestone title is required.");

            var order = await _context.Orders.FindAsync(milestone.OrderId)
                ?? throw new InvalidOperationException("Order not found.");

            // Verify page count is valid for the order
            if (milestone.Pages <= 0)
                throw new InvalidOperationException("Milestone pages must be greater than zero.");

            milestone.Status = MilestoneStatus.Pending;
            milestone.CreatedAt = DateTime.UtcNow;
            milestone.UpdatedAt = DateTime.UtcNow;
            if (milestone.SortOrder <= 0)
                milestone.SortOrder = await GetNextSortOrderAsync(milestone.OrderId);

            _context.OrderMilestones.Add(milestone);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Milestone {Id} created for order {OrderId} by admin.",
                milestone.Id, milestone.OrderId);

            // Notify client + writer
            if (!string.IsNullOrEmpty(order.AssignedWriterId))
            {
                await _notificationService.CreateNotificationAsync(
                    order.AssignedWriterId,
                    "New Milestone Added",
                    $"A new milestone \"{milestone.Title}\" was added to order {order.OrderNumber}. Deadline: {milestone.Deadline:MMM dd, yyyy}.",
                    NotificationType.OrderAssigned,
                    milestone.Id.ToString());
            }

            await _notificationService.CreateNotificationAsync(
                order.ClientId,
                "Milestone Created",
                $"A new milestone \"{milestone.Title}\" was created for order {order.OrderNumber}.",
                NotificationType.SystemAlert,
                milestone.Id.ToString());

            return milestone;
        }

        public async Task<OrderMilestone?> UpdateMilestoneAsync(OrderMilestone milestone)
        {
            var existing = await _context.OrderMilestones.FindAsync(milestone.Id);
            if (existing == null) return null;

            if (existing.Status == MilestoneStatus.Approved)
                throw new InvalidOperationException("Cannot edit an approved milestone.");

            existing.Title = milestone.Title;
            existing.Pages = milestone.Pages;
            existing.Deadline = milestone.Deadline;
            existing.SortOrder = milestone.SortOrder;
            existing.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return existing;
        }

        public async Task<bool> DeleteMilestoneAsync(int id)
        {
            var existing = await _context.OrderMilestones
                .Include(m => m.Files)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (existing == null) return false;

            if (existing.Status == MilestoneStatus.Approved)
                throw new InvalidOperationException("Cannot delete an approved milestone.");

            _context.OrderMilestoneFiles.RemoveRange(existing.Files);
            _context.OrderMilestones.Remove(existing);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Milestone {Id} deleted.", id);
            return true;
        }

        public async Task<OrderMilestone> SubmitMilestoneAsync(
            int milestoneId,
            string writerId,
            List<(string FileName, string FilePath, long FileSize, string? Description)> files,
            string? notes)
        {
            var milestone = await _context.OrderMilestones
                .Include(m => m.Order)
                .FirstOrDefaultAsync(m => m.Id == milestoneId)
                ?? throw new InvalidOperationException("Milestone not found.");

            var order = milestone.Order
                ?? throw new InvalidOperationException("Order not found.");

            if (order.AssignedWriterId != writerId)
                throw new InvalidOperationException("You are not the assigned writer for this order.");

            if (milestone.Status == MilestoneStatus.Approved)
                throw new InvalidOperationException("This milestone has already been approved.");

            if (files == null || files.Count == 0)
                throw new InvalidOperationException("Please upload at least one file before submitting.");

            // Persist the uploaded files
            foreach (var (fileName, filePath, fileSize, description) in files)
            {
                _context.OrderMilestoneFiles.Add(new OrderMilestoneFile
                {
                    MilestoneId = milestone.Id,
                    FileName = fileName,
                    FilePath = filePath,
                    FileSizeBytes = fileSize,
                    Description = description,
                    UploadedAt = DateTime.UtcNow
                });
            }

            milestone.Status = MilestoneStatus.Submitted;
            milestone.SubmittedAt = DateTime.UtcNow;
            milestone.SubmissionNotes = notes;
            milestone.UpdatedAt = DateTime.UtcNow;

            _context.AuditLogs.Add(new AuditLog
            {
                Action = "Milestone Submitted",
                PerformedById = writerId,
                TargetUserId = order.ClientId,
                Description = $"Writer submitted milestone \"{milestone.Title}\" for order {order.OrderNumber}.",
                CreatedDate = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            // Notify client
            await _notificationService.CreateNotificationAsync(
                order.ClientId,
                "Milestone Ready for Review",
                $"Writer submitted \"{milestone.Title}\" for order {order.OrderNumber}. Please review and approve.",
                NotificationType.OrderSubmitted,
                milestone.Id.ToString());

            _logger.LogInformation("Milestone {Id} submitted by writer {WriterId}.", milestoneId, writerId);
            return milestone;
        }

        public async Task<OrderMilestone> ApproveMilestoneAsync(
            int milestoneId,
            string clientId,
            string? notes)
        {
            var milestone = await _context.OrderMilestones
                .Include(m => m.Order)
                .FirstOrDefaultAsync(m => m.Id == milestoneId)
                ?? throw new InvalidOperationException("Milestone not found.");

            var order = milestone.Order
                ?? throw new InvalidOperationException("Order not found.");

            if (order.ClientId != clientId)
                throw new InvalidOperationException("Only the client can approve milestones.");

            if (milestone.Status != MilestoneStatus.Submitted)
                throw new InvalidOperationException("Only submitted milestones can be approved.");

            // Compute earnings: 90% of (Pages/TotalPages * Budget)
            int totalPages = order.Pages ?? 1;
            var writerShare = order.Budget <= 0 || totalPages <= 0
                ? 0m
                : Math.Round((decimal)milestone.Pages / totalPages * order.Budget * 0.90m, 2, MidpointRounding.AwayFromZero);

            // Record the milestone earnings in the financial ledger
            var txnNumber = await _financialService.RecordMilestoneEarningsAsync(
                order.Id, milestone.Id, writerShare, order.AssignedWriterId, clientId,
                $"Milestone approved: {milestone.Title} ({milestone.Pages} pages)");

            milestone.Status = MilestoneStatus.Approved;
            milestone.ApprovedAt = DateTime.UtcNow;
            milestone.ApprovedById = clientId;
            milestone.ApprovalNotes = notes;
            milestone.ApprovedEarnings = writerShare;
            milestone.LedgerTransactionNumber = txnNumber;
            milestone.UpdatedAt = DateTime.UtcNow;

            _context.AuditLogs.Add(new AuditLog
            {
                Action = "Milestone Approved",
                PerformedById = clientId,
                TargetUserId = order.AssignedWriterId,
                Description = $"Client approved milestone \"{milestone.Title}\" for order {order.OrderNumber}. Writer earned ${writerShare:N2}.",
                CreatedDate = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            // Notify the writer
            if (!string.IsNullOrEmpty(order.AssignedWriterId))
            {
                await _notificationService.CreateNotificationAsync(
                    order.AssignedWriterId,
                    "Milestone Approved",
                    $"Your milestone \"{milestone.Title}\" was approved. ${writerShare:N2} has been credited to your wallet.",
                    NotificationType.OrderCompleted,
                    milestone.Id.ToString());
            }

            _logger.LogInformation("Milestone {Id} approved by client {ClientId}, writer earned {Amount:C}.",
                milestoneId, clientId, writerShare);
            return milestone;
        }

        public async Task<List<MilestoneTimelineEntry>> GetTimelineAsync(int orderId)
        {
            var milestones = await GetMilestonesAsync(orderId);
            var result = new List<MilestoneTimelineEntry>();

            foreach (var m in milestones)
            {
                var files = await GetFilesAsync(m.Id);
                result.Add(new MilestoneTimelineEntry
                {
                    Id = m.Id,
                    SortOrder = m.SortOrder,
                    Title = m.Title,
                    Pages = m.Pages,
                    Deadline = m.Deadline,
                    Status = m.Status,
                    SubmittedAt = m.SubmittedAt,
                    ApprovedAt = m.ApprovedAt,
                    ApprovedEarnings = m.ApprovedEarnings,
                    Files = files.Select(f => new MilestoneFileEntry
                    {
                        Id = f.Id,
                        FileName = f.FileName,
                        FilePath = f.FilePath,
                        FileSizeBytes = f.FileSizeBytes,
                        UploadedAt = f.UploadedAt
                    }).ToList()
                });
            }

            return result;
        }
    }
}
