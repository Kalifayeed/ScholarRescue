using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ScholarRescue.Data;
using ScholarRescue.Models;
using ScholarRescue.Models.Enums;

namespace ScholarRescue.Services
{
    /// <summary>
    /// Implements the writer work delivery workflow: submissions, revisions,
    /// client acceptance, admin overrides, and accounting integration.
    /// </summary>
    public class WorkDeliveryService : IWorkDeliveryService
    {
        private readonly ScholarRescueDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IFinancialService _financialService;
        private readonly IConfigurationService _configurationService;
        private readonly INotificationService _notificationService;
        private readonly IWriterRankingService _rankingService;
        private readonly ILogger<WorkDeliveryService> _logger;

        private static readonly string[] AllowedExtensions = { ".pdf", ".doc", ".docx", ".zip" };
        private const long MaxFileSize = 25L * 1024 * 1024;

        public WorkDeliveryService(
            ScholarRescueDbContext context,
            UserManager<ApplicationUser> userManager,
            IFinancialService financialService,
            IConfigurationService configurationService,
            INotificationService notificationService,
            IWriterRankingService rankingService,
            ILogger<WorkDeliveryService> logger)
        {
            _context = context;
            _userManager = userManager;
            _financialService = financialService;
            _configurationService = configurationService;
            _notificationService = notificationService;
            _rankingService = rankingService;
            _logger = logger;
        }

        public bool IsValidFileType(IFormFile file)
        {
            if (file == null || file.Length == 0) return false;
            if (file.Length > MaxFileSize) return false;
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            return AllowedExtensions.Contains(ext);
        }

        public async Task<OrderSubmission> SubmitWorkAsync(int orderId, string writerId, IFormFile file, string comments, SubmissionType submissionType, int? reviewedAttachmentId = null)
        {
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == orderId)
                ?? throw new InvalidOperationException("Order not found.");

            if (order.AssignedWriterId != writerId)
                throw new InvalidOperationException("You are not the assigned writer for this order.");

            if (!IsValidFileType(file))
                throw new InvalidOperationException("Invalid file type. Accepted: PDF, DOC, DOCX, ZIP (max 25MB).");

            // Phase 2: For DraftFeedback/ProofreadingOwnWork, validate reviewed attachment and comment length.
            bool requiresDraftReference = order.RequestType == RequestType.DraftFeedback ||
                                          order.RequestType == RequestType.ProofreadingOwnWork;

            if (requiresDraftReference)
            {
                if (!reviewedAttachmentId.HasValue)
                    throw new InvalidOperationException("You must select which client draft you are providing feedback on.");

                var attachment = await _context.OrderAttachments
                    .FirstOrDefaultAsync(a => a.Id == reviewedAttachmentId.Value)
                    ?? throw new InvalidOperationException("The selected attachment was not found.");

                if (attachment.OrderId != orderId)
                    throw new InvalidOperationException("The selected attachment does not belong to this order.");

                if (attachment.AttachmentPurpose != AttachmentPurpose.StudentDraft)
                    throw new InvalidOperationException("The selected attachment is not a student draft. Only files uploaded as Student Draft can be referenced as the original work being reviewed.");

                if (string.IsNullOrWhiteSpace(comments) || comments.Length < 20)
                    throw new InvalidOperationException("Please provide detailed feedback notes (minimum 20 characters).");
            }

            var allowedStatuses = submissionType switch
            {
                SubmissionType.Draft => new[] { OrderStatus.InProgress, OrderStatus.Assigned },
                SubmissionType.Revision => new[] { OrderStatus.RevisionRequested },
                SubmissionType.Final => new[] { OrderStatus.DraftSubmitted, OrderStatus.RevisionSubmitted, OrderStatus.RevisionRequested },
                _ => Array.Empty<OrderStatus>()
            };

            if (!allowedStatuses.Contains(order.Status))
                throw new InvalidOperationException($"Cannot submit {submissionType} when order status is {order.Status}.");

            var currentVersion = await _context.Set<OrderSubmission>()
                .Where(s => s.OrderId == orderId)
                .MaxAsync(s => (int?)s.VersionNumber) ?? 0;

            var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "submissions", orderId.ToString());
            Directory.CreateDirectory(uploadsDir);

            var safeName = $"{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid():N}_{file.FileName}";
            var filePath = Path.Combine(uploadsDir, safeName);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var submission = new OrderSubmission
            {
                OrderId = orderId,
                WriterId = writerId,
                VersionNumber = currentVersion + 1,
                SubmissionType = submissionType,
                FilePath = $"/uploads/submissions/{orderId}/{safeName}",
                FileName = file.FileName,
                Comments = comments,
                ReviewedAttachmentId = requiresDraftReference ? reviewedAttachmentId : null,
                SubmittedAt = DateTime.UtcNow
            };

            _context.Set<OrderSubmission>().Add(submission);

            order.Status = submissionType switch
            {
                SubmissionType.Draft => OrderStatus.DraftSubmitted,
                SubmissionType.Revision => OrderStatus.RevisionSubmitted,
                SubmissionType.Final => OrderStatus.PendingQA,
                _ => order.Status
            };
            order.UpdatedAt = DateTime.UtcNow;

            _context.AuditLogs.Add(new AuditLog
            {
                Action = $"Writer Uploaded {submissionType}",
                PerformedById = writerId,
                TargetUserId = order.ClientId,
                Description = $"Writer uploaded {submissionType} (v{submission.VersionNumber}) for order {order.OrderNumber}.",
                CreatedDate = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            await _notificationService.CreateNotificationAsync(
                order.ClientId,
                $"{submissionType} Uploaded",
                $"A {submissionType.ToString().ToLower()} has been uploaded for order {order.OrderNumber}.",
                NotificationType.OrderSubmitted,
                order.Id.ToString());

            _logger.LogInformation("Writer {WriterId} submitted {Type} v{Version} for order {OrderId}{Reviewed}.", 
                writerId, submissionType, submission.VersionNumber, orderId,
                reviewedAttachmentId.HasValue ? $", reviewed attachment {reviewedAttachmentId}" : "");

            return submission;
        }

        public async Task<List<OrderSubmission>> GetSubmissionsAsync(int orderId)
        {
            return await _context.Set<OrderSubmission>()
                .Include(s => s.Writer)
                .Include(s => s.ReviewedAttachment)
                .Where(s => s.OrderId == orderId)
                .OrderByDescending(s => s.VersionNumber)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<List<OrderSubmission>> GetSubmissionTimelineAsync(int orderId)
        {
            return await _context.Set<OrderSubmission>()
                .Include(s => s.Writer)
                .Include(s => s.ReviewedAttachment)
                .Where(s => s.OrderId == orderId)
                .OrderBy(s => s.VersionNumber)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<RevisionRequest> RequestRevisionAsync(int orderId, string clientId, string comments)
        {
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == orderId)
                ?? throw new InvalidOperationException("Order not found.");

            if (order.ClientId != clientId)
                throw new InvalidOperationException("Only the client can request revisions.");

            if (string.IsNullOrWhiteSpace(comments) || comments.Length < 10)
                throw new InvalidOperationException("Please provide detailed revision instructions (min 10 characters).");

            if (order.Status != OrderStatus.DraftSubmitted &&
                order.Status != OrderStatus.RevisionSubmitted &&
                order.Status != OrderStatus.FinalSubmitted)
                throw new InvalidOperationException($"Cannot request revision when order status is {order.Status}.");

            var revision = new RevisionRequest
            {
                OrderId = orderId,
                ClientId = clientId,
                WriterId = order.AssignedWriterId!,
                Comments = comments,
                RequestedAt = DateTime.UtcNow,
                Status = RevisionRequestStatus.Pending
            };

            _context.Set<RevisionRequest>().Add(revision);

            order.Status = OrderStatus.RevisionRequested;
            order.UpdatedAt = DateTime.UtcNow;

            _context.AuditLogs.Add(new AuditLog
            {
                Action = "Revision Requested",
                PerformedById = clientId,
                TargetUserId = order.AssignedWriterId,
                Description = $"Client requested revision for order {order.OrderNumber}: {comments[..Math.Min(100, comments.Length)]}",
                CreatedDate = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            await _notificationService.CreateNotificationAsync(
                order.AssignedWriterId!,
                "Revision Requested",
                $"The client has requested revisions on order {order.OrderNumber}.",
                NotificationType.RevisionRequested,
                order.Id.ToString());

            return revision;
        }

        public async Task<List<RevisionRequest>> GetPendingRevisionsAsync(string writerId)
        {
            return await _context.Set<RevisionRequest>()
                .Include(r => r.Order)
                .Include(r => r.Client)
                .Where(r => r.WriterId == writerId && r.Status == RevisionRequestStatus.Pending)
                .OrderByDescending(r => r.RequestedAt)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<List<RevisionRequest>> GetOrderRevisionsAsync(int orderId)
        {
            return await _context.Set<RevisionRequest>()
                .Include(r => r.Client)
                .Include(r => r.Writer)
                .Where(r => r.OrderId == orderId)
                .OrderByDescending(r => r.RequestedAt)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task AcceptWorkAsync(int orderId, string clientId)
        {
            var order = await _context.Orders
                .Include(o => o.Client)
                .FirstOrDefaultAsync(o => o.Id == orderId)
                ?? throw new InvalidOperationException("Order not found.");

            if (order.ClientId != clientId)
                throw new InvalidOperationException("Only the client can accept work.");

            if (order.Status != OrderStatus.DraftSubmitted &&
                order.Status != OrderStatus.RevisionSubmitted &&
                order.Status != OrderStatus.FinalSubmitted)
                throw new InvalidOperationException($"Cannot accept work when order status is {order.Status}.");

            // Payment deferral check: if the order was created with Pay Later, escrow must be funded
            if (order.PaymentDeferred)
            {
                var escrow = await _context.Set<EscrowAccount>()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(e => e.OrderId == orderId);

                if (escrow == null || escrow.Status != EscrowStatus.Funded)
                {
                    _logger.LogWarning("Blocked work acceptance on unfunded deferred order {OrderId}", orderId);
                    throw new InvalidOperationException("Please complete payment before accepting this work.");
                }
            }

            order.Status = OrderStatus.Completed;
            order.UpdatedAt = DateTime.UtcNow;
            order.CompletedAt = DateTime.UtcNow;
            order.IsMarketplaceOpen = false;

            var pendingRevisions = await _context.Set<RevisionRequest>()
                .Where(r => r.OrderId == orderId && r.Status == RevisionRequestStatus.Pending)
                .ToListAsync();
            foreach (var rev in pendingRevisions)
                rev.Status = RevisionRequestStatus.Completed;

            // Accounting: calculate earnings and commission
            var commissionRate = await _configurationService.GetCommissionRateAsync();
            var writerEarnings = order.Budget * (1 - commissionRate);
            var commissionAmount = order.Budget * commissionRate;

            order.WriterEarnings = writerEarnings;
            order.CommissionAmount = commissionAmount;

            // Use FinancialService for wallet crediting and platform revenue recording
            await _financialService.ProcessOrderCompletionAsync(orderId, clientId);

            _context.AuditLogs.Add(new AuditLog
            {
                Action = "Work Accepted",
                PerformedById = clientId,
                TargetUserId = order.AssignedWriterId,
                Description = $"Client accepted work for order {order.OrderNumber}. Writer earned ${writerEarnings:N2}.",
                CreatedDate = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            await _notificationService.CreateNotificationAsync(
                order.AssignedWriterId!,
                "Work Accepted",
                $"Your work for order {order.OrderNumber} has been accepted. ${writerEarnings:N2} has been credited to your wallet.",
                NotificationType.OrderCompleted,
                order.Id.ToString());

            _logger.LogInformation("Order {OrderId} completed. Writer {WriterId} earned {Earnings:C}.",
                orderId, order.AssignedWriterId, writerEarnings);

            // Update writer ranking metrics and trigger auto-promotion.
            if (!string.IsNullOrEmpty(order.AssignedWriterId))
            {
                await _rankingService.UpdateMetricsOnCompletionAsync(order.AssignedWriterId);
            }
        }

        public async Task AdminForceCompletionAsync(int orderId, string adminId)
        {
            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.Id == orderId)
                ?? throw new InvalidOperationException("Order not found.");

            order.Status = OrderStatus.Completed;
            order.UpdatedAt = DateTime.UtcNow;
            order.CompletedAt = DateTime.UtcNow;
            order.IsMarketplaceOpen = false;

            var commissionRate = await _configurationService.GetCommissionRateAsync();
            order.WriterEarnings = order.Budget * (1 - commissionRate);
            order.CommissionAmount = order.Budget * commissionRate;

            await _financialService.ProcessOrderCompletionAsync(orderId, adminId);

            _context.AuditLogs.Add(new AuditLog
            {
                Action = "Admin Force Completion",
                PerformedById = adminId,
                TargetUserId = order.AssignedWriterId,
                Description = $"Admin force-completed order {order.OrderNumber}.",
                CreatedDate = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
            _logger.LogInformation("Admin {AdminId} force-completed order {OrderId}.", adminId, orderId);

            // Trigger ranking metrics update.
            if (!string.IsNullOrEmpty(order.AssignedWriterId))
            {
                await _rankingService.UpdateMetricsOnCompletionAsync(order.AssignedWriterId);
            }
        }

        public async Task AdminForceRevisionAsync(int orderId, string adminId, string comments)
        {
            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.Id == orderId)
                ?? throw new InvalidOperationException("Order not found.");

            if (order.Status != OrderStatus.DraftSubmitted &&
                order.Status != OrderStatus.RevisionSubmitted &&
                order.Status != OrderStatus.FinalSubmitted)
                throw new InvalidOperationException($"Cannot force revision when order status is {order.Status}.");

            var revision = new RevisionRequest
            {
                OrderId = orderId,
                ClientId = order.ClientId,
                WriterId = order.AssignedWriterId!,
                Comments = comments,
                RequestedAt = DateTime.UtcNow,
                Status = RevisionRequestStatus.Pending
            };

            _context.Set<RevisionRequest>().Add(revision);
            order.Status = OrderStatus.RevisionRequested;
            order.UpdatedAt = DateTime.UtcNow;

            _context.AuditLogs.Add(new AuditLog
            {
                Action = "Admin Force Revision",
                PerformedById = adminId,
                TargetUserId = order.AssignedWriterId,
                Description = $"Admin forced revision on order {order.OrderNumber}.",
                CreatedDate = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
        }

    }
}