using Microsoft.EntityFrameworkCore;
using ScholarRescue.Data;
using ScholarRescue.Models;
using ScholarRescue.Models.Enums;

namespace ScholarRescue.Services
{
    /// <summary>
    /// Escrow engine. All funds pass through escrow accounts.
    /// No direct client-to-writer payments.
    /// </summary>
    public class EscrowService : IEscrowService
    {
        private readonly ScholarRescueDbContext _context;
        private readonly IWalletService _walletService;
        private readonly INotificationService _notificationService;
        private readonly IOrderTimelineService _timelineService;
        private readonly IConfigurationService _configurationService;
        private readonly ILogger<EscrowService> _logger;

        public EscrowService(
            ScholarRescueDbContext context,
            IWalletService walletService,
            INotificationService notificationService,
            IOrderTimelineService timelineService,
            IConfigurationService configurationService,
            ILogger<EscrowService> logger)
        {
            _context = context;
            _walletService = walletService;
            _notificationService = notificationService;
            _timelineService = timelineService;
            _configurationService = configurationService;
            _logger = logger;
        }

        public async Task<EscrowAccount> CreateEscrowAsync(int orderId, string clientId)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null) throw new InvalidOperationException("Order not found.");

            var commissionRate = await _configurationService.GetCommissionRateAsync();
            var commission = order.Budget * commissionRate;
            var writerAmount = order.Budget - commission;

            var escrow = new EscrowAccount
            {
                OrderId = orderId,
                ClientId = clientId,
                TotalAmount = order.Budget,
                CommissionAmount = commission,
                WriterAmount = writerAmount,
                Status = EscrowStatus.PendingFunding,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Set<EscrowAccount>().Add(escrow);

            _context.AuditLogs.Add(new AuditLog
            {
                Action = "Escrow Created",
                PerformedById = clientId,
                TargetUserId = clientId,
                Description = $"Escrow ${order.Budget:F2} created for order {order.OrderNumber}",
                CreatedDate = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            await _timelineService.AddEventAsync(orderId, clientId, "System", TimelineEventType.SystemAction,
                "Escrow Created", $"Escrow account of ${order.Budget:F2} created for this order.");

            return escrow;
        }

        public async Task<EscrowAccount> FundEscrowAsync(int orderId, string paymentMethod)
        {
            var escrow = await _context.Set<EscrowAccount>().Include(e => e.Order).FirstOrDefaultAsync(e => e.OrderId == orderId);
            if (escrow == null) throw new InvalidOperationException("Escrow not found.");
            if (escrow.Status != EscrowStatus.PendingFunding) throw new InvalidOperationException("Escrow is not in PendingFunding status.");

            escrow.FundedAmount = escrow.TotalAmount;
            escrow.Status = EscrowStatus.Funded;
            escrow.UpdatedAt = DateTime.UtcNow;

            // Update order status
            var order = escrow.Order;
            order.Status = OrderStatus.Open;
            order.IsMarketplaceOpen = true;
            order.UpdatedAt = DateTime.UtcNow;

            // Create financial transaction
            _context.FinancialTransactions.Add(new FinancialTransaction
            {
                TransactionNumber = $"TXN-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid():N}"[..30],
                TransactionType = TransactionType.OrderFunded,
                ReferenceId = orderId,
                ReferenceType = "Order",
                UserId = escrow.ClientId,
                Description = $"Order {order.OrderNumber} funded via {paymentMethod}",
                DebitAmount = 0,
                CreditAmount = escrow.TotalAmount,
                BalanceAfter = escrow.TotalAmount,
                CreatedDate = DateTime.UtcNow
            });

            // Order history
            _context.OrderHistories.Add(new OrderHistory
            {
                OrderId = orderId,
                OldStatus = OrderStatus.PendingPayment,
                NewStatus = OrderStatus.Open,
                ChangedById = escrow.ClientId,
                CreatedAt = DateTime.UtcNow,
                Notes = $"Order funded via {paymentMethod}"
            });

            // Audit log
            _context.AuditLogs.Add(new AuditLog
            {
                Action = "Escrow Funded",
                PerformedById = escrow.ClientId,
                TargetUserId = escrow.ClientId,
                Description = $"Order {order.OrderNumber} funded ${escrow.TotalAmount:F2} via {paymentMethod}",
                CreatedDate = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            // Notifications
            await _notificationService.CreateNotificationAsync(escrow.ClientId,
                "Order Funded", $"Your order {order.OrderNumber} has been funded successfully.",
                NotificationType.NewOrder, orderId.ToString(), "Order");

            var admins = await _context.Users.Where(u => u.UserType == "Administrator" && u.IsActive).ToListAsync();
            foreach (var admin in admins)
            {
                await _notificationService.NotifyNewOrderCreatedAsync(orderId, order.OrderNumber);
            }

            // Timeline
            await _timelineService.AddEventAsync(orderId, escrow.ClientId, "System", TimelineEventType.PaymentReceived,
                "Order Funded", $"Payment of ${escrow.TotalAmount:F2} received via {paymentMethod}.");

            return escrow;
        }

        public async Task<EscrowAccount> ReleaseFundsAsync(int orderId)
        {
            var escrow = await _context.Set<EscrowAccount>()
                .Include(e => e.Order).ThenInclude(o => o.AssignedWriter)
                .FirstOrDefaultAsync(e => e.OrderId == orderId);

            if (escrow == null) throw new InvalidOperationException("Escrow not found.");
            if (escrow.Status != EscrowStatus.Funded) throw new InvalidOperationException("Escrow not in Funded state.");
            if (escrow.AssignedWriterId == null) throw new InvalidOperationException("No writer assigned.");

            escrow.ReleasedAmount = escrow.WriterAmount;
            escrow.Status = EscrowStatus.Released;
            escrow.UpdatedAt = DateTime.UtcNow;

            var order = escrow.Order;
            order.Status = OrderStatus.Completed;
            order.CompletedAt = DateTime.UtcNow;
            order.UpdatedAt = DateTime.UtcNow;

            // Credit writer wallet
            await _walletService.AddFundsAsync(escrow.AssignedWriterId, escrow.WriterAmount);

            // Update platform wallet
            var platformWallet = await _context.PlatformWallets.FirstOrDefaultAsync();
            if (platformWallet == null)
            {
                platformWallet = new PlatformWallet { AvailableBalance = 0, LifetimeCommission = 0, LifetimeRevenue = 0 };
                _context.PlatformWallets.Add(platformWallet);
            }
            platformWallet.AvailableBalance += escrow.CommissionAmount;
            platformWallet.LifetimeCommission += escrow.CommissionAmount;
            platformWallet.LifetimeRevenue += escrow.CommissionAmount;

            // Financial transaction for writer payment
            _context.FinancialTransactions.Add(new FinancialTransaction
            {
                TransactionNumber = $"TXN-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid():N}"[..30],
                TransactionType = TransactionType.WriterEarningReleased,
                ReferenceId = orderId,
                ReferenceType = "Order",
                UserId = escrow.AssignedWriterId,
                Description = $"Funds released for order {order.OrderNumber}",
                DebitAmount = 0,
                CreditAmount = escrow.WriterAmount,
                BalanceAfter = escrow.WriterAmount,
                CreatedDate = DateTime.UtcNow
            });

            // Commission transaction
            _context.FinancialTransactions.Add(new FinancialTransaction
            {
                TransactionNumber = $"TXN-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid():N}"[..30],
                TransactionType = TransactionType.CommissionCharged,
                ReferenceId = orderId,
                ReferenceType = "Order",
                Description = $"Commission for order {order.OrderNumber}",
                DebitAmount = 0,
                CreditAmount = escrow.CommissionAmount,
                BalanceAfter = escrow.CommissionAmount,
                CreatedDate = DateTime.UtcNow
            });

            // Order financial record
            _context.OrderFinancialRecords.Add(new OrderFinancialRecord
            {
                OrderId = orderId,
                WriterAmount = escrow.WriterAmount,
                CommissionAmount = escrow.CommissionAmount,
                CreatedDate = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            // Notifications
            await _notificationService.CreateNotificationAsync(escrow.AssignedWriterId,
                "Funds Released", $"$${escrow.WriterAmount:F2} has been released to your wallet for order {order.OrderNumber}.",
                NotificationType.OrderCompleted, orderId.ToString(), "Order");

            // Timeline
            await _timelineService.AddEventAsync(orderId, escrow.ClientId, "System", TimelineEventType.OrderCompleted,
                "Funds Released", $"${escrow.WriterAmount:F2} released to writer, ${escrow.CommissionAmount:F2} commission retained.");

            return escrow;
        }

        public async Task<EscrowAccount> RefundEscrowAsync(int orderId, string adminId)
        {
            var escrow = await _context.Set<EscrowAccount>().Include(e => e.Order).FirstOrDefaultAsync(e => e.OrderId == orderId);
            if (escrow == null) throw new InvalidOperationException("Escrow not found.");
            if (escrow.Status != EscrowStatus.Funded && escrow.Status != EscrowStatus.PendingFunding)
                throw new InvalidOperationException("Escrow cannot be refunded in current state.");

            escrow.RefundedAmount = escrow.FundedAmount;
            escrow.Status = EscrowStatus.Refunded;
            escrow.UpdatedAt = DateTime.UtcNow;

            var order = escrow.Order;
            order.Status = OrderStatus.Cancelled;
            order.UpdatedAt = DateTime.UtcNow;

            _context.AuditLogs.Add(new AuditLog
            {
                Action = "Escrow Refunded",
                PerformedById = adminId,
                TargetUserId = escrow.ClientId,
                Description = $"Order {order.OrderNumber} refunded ${escrow.FundedAmount:F2}",
                CreatedDate = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            await _timelineService.AddEventAsync(orderId, adminId, "System", TimelineEventType.RefundApproved,
                "Order Refunded", $"${escrow.FundedAmount:F2} refunded to client.");

            return escrow;
        }

        public async Task<EscrowAccount> LockEscrowForDisputeAsync(int orderId)
        {
            var escrow = await _context.Set<EscrowAccount>().FirstOrDefaultAsync(e => e.OrderId == orderId);
            if (escrow == null) throw new InvalidOperationException("Escrow not found.");
            escrow.Status = EscrowStatus.Disputed;
            escrow.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            await _timelineService.AddEventAsync(orderId, "System", "System", TimelineEventType.SystemAction,
                "Dispute Locked", "Funds frozen pending dispute resolution.");

            return escrow;
        }

        public async Task<EscrowAccount?> GetEscrowAsync(int orderId) =>
            await _context.Set<EscrowAccount>().Include(e => e.Order).FirstOrDefaultAsync(e => e.OrderId == orderId);

        public async Task<List<EscrowAccount>> GetAllEscrowsAsync(string? filter = null)
        {
            IQueryable<EscrowAccount> query = _context.Set<EscrowAccount>().Include(e => e.Order).AsNoTracking();
            if (!string.IsNullOrWhiteSpace(filter) && Enum.TryParse<EscrowStatus>(filter, true, out var status))
                query = query.Where(e => e.Status == status);
            return await query.OrderByDescending(e => e.CreatedAt).ToListAsync();
        }
    }
}