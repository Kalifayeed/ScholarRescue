using Microsoft.EntityFrameworkCore;
using ScholarRescue.Data;
using ScholarRescue.Models;
using ScholarRescue.Models.Enums;

namespace ScholarRescue.Services
{
    /// <summary>
    /// Core financial ledger service implementation.
    /// All financial events create immutable ledger entries.
    /// No balances are manually edited; they are always derived from transactions.
    /// </summary>
    public class FinancialService : IFinancialService
    {
        private readonly ScholarRescueDbContext _context;
        private readonly IPayoutWindowService _payoutWindowService;
        private readonly IConfigurationService _configurationService;
        private readonly ILogger<FinancialService> _logger;

        public FinancialService(
            ScholarRescueDbContext context,
            IPayoutWindowService payoutWindowService,
            IConfigurationService configurationService,
            ILogger<FinancialService> logger)
        {
            _context = context;
            _payoutWindowService = payoutWindowService;
            _configurationService = configurationService;
            _logger = logger;
        }

        // ──────────────────────────────────────────────
        // Platform Wallet
        // ──────────────────────────────────────────────

        public async Task<PlatformWallet> GetPlatformWalletAsync()
        {
            var wallet = await _context.PlatformWallets.FirstOrDefaultAsync();
            if (wallet == null)
            {
                wallet = new PlatformWallet
                {
                    AvailableBalance = 0,
                    PendingBalance = 0,
                    LifetimeRevenue = 0,
                    LifetimeCommission = 0,
                    TotalPayouts = 0,
                    LastUpdated = DateTime.UtcNow
                };
                _context.PlatformWallets.Add(wallet);
                await _context.SaveChangesAsync();
            }
            return wallet;
        }

        // ──────────────────────────────────────────────
        // Transaction Number Generation
        // ──────────────────────────────────────────────

        public async Task<string> GetNextTransactionNumberAsync()
        {
            var lastTxn = await _context.FinancialTransactions
                .OrderByDescending(t => t.Id)
                .FirstOrDefaultAsync();

            int nextNumber = 1;
            if (lastTxn != null && !string.IsNullOrEmpty(lastTxn.TransactionNumber))
            {
                var parts = lastTxn.TransactionNumber.Split('-');
                if (parts.Length == 3 && int.TryParse(parts[2], out int lastNum))
                {
                    nextNumber = lastNum + 1;
                }
            }

            return $"TXN-{DateTime.UtcNow.Year}-{nextNumber:D6}";
        }

        // ──────────────────────────────────────────────
        // Ledger Entry Creation
        // ──────────────────────────────────────────────

        private async Task<FinancialTransaction> CreateLedgerEntryAsync(
            TransactionType type,
            string description,
            decimal debitAmount,
            decimal creditAmount,
            decimal balanceAfter,
            string? userId,
            string? createdBy,
            int? referenceId,
            string? referenceType)
        {
            var txnNumber = await GetNextTransactionNumberAsync();

            var transaction = new FinancialTransaction
            {
                TransactionNumber = txnNumber,
                TransactionType = type,
                ReferenceId = referenceId,
                ReferenceType = referenceType,
                UserId = userId,
                Description = description,
                DebitAmount = debitAmount,
                CreditAmount = creditAmount,
                BalanceAfter = balanceAfter,
                CreatedDate = DateTime.UtcNow,
                CreatedBy = createdBy
            };

            _context.FinancialTransactions.Add(transaction);
            return transaction;
        }

        // ──────────────────────────────────────────────
        // Order Completion & Commission Processing
        // ──────────────────────────────────────────────

        public async Task ProcessOrderCompletionAsync(int orderId, string adminId)
        {
            var order = await _context.Orders
                .Include(o => o.AssignedWriter)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
                throw new InvalidOperationException("Order not found.");

            if (order.Status != OrderStatus.Completed)
                throw new InvalidOperationException("Order must be in Completed status to process financials.");

            if (string.IsNullOrEmpty(order.AssignedWriterId))
                throw new InvalidOperationException("Order has no assigned writer.");

            // Check if financial record already exists
            var existingRecord = await _context.OrderFinancialRecords
                .FirstOrDefaultAsync(r => r.OrderId == orderId);
            if (existingRecord != null)
                throw new InvalidOperationException("Financial record already exists for this order.");

            // Calculate commission from configuration
            var orderAmount = order.Budget;
            var commissionRate = await _configurationService.GetCommissionRateAsync();
            var commissionAmount = Math.Round(orderAmount * commissionRate, 2);
            var writerAmount = orderAmount - commissionAmount;

            // Create Order Financial Record
            var financialRecord = new OrderFinancialRecord
            {
                OrderId = orderId,
                OrderAmount = orderAmount,
                CommissionAmount = commissionAmount,
                WriterAmount = writerAmount,
                CreatedDate = DateTime.UtcNow
            };
            _context.OrderFinancialRecords.Add(financialRecord);

            // ── Writer Wallet ──
            var writerWallet = await GetOrCreateWriterWalletAsync(order.AssignedWriterId);
            writerWallet.PendingBalance += writerAmount;
            writerWallet.LifetimeEarnings += writerAmount;
            writerWallet.LifetimeCommissionPaid += commissionAmount;
            writerWallet.LastUpdated = DateTime.UtcNow;

            // ── Platform Wallet ──
            var platformWallet = await GetPlatformWalletAsync();
            platformWallet.PendingBalance += commissionAmount;
            platformWallet.LifetimeCommission += commissionAmount;
            platformWallet.LastUpdated = DateTime.UtcNow;

            // ── Ledger Entries ──

            // 1. Commission Charged (platform revenue pending)
            var commissionTxn = await CreateLedgerEntryAsync(
                TransactionType.CommissionCharged,
                $"Commission charged for Order #{order.OrderNumber}: ${commissionAmount:F2} (10% of ${orderAmount:F2})",
                0,
                commissionAmount,
                platformWallet.PendingBalance,
                null,
                adminId,
                orderId,
                "Order"
            );

            // 2. Writer Earning Added
            var writerTxn = await CreateLedgerEntryAsync(
                TransactionType.WriterEarningAdded,
                $"Earnings added for Order #{order.OrderNumber}: ${writerAmount:F2} (after {commissionAmount:F2} commission)",
                0,
                writerAmount,
                writerWallet.PendingBalance,
                order.AssignedWriterId,
                adminId,
                orderId,
                "Order"
            );

            // 3. Order Completed
            var orderTxn = await CreateLedgerEntryAsync(
                TransactionType.OrderCompleted,
                $"Order #{order.OrderNumber} completed. Amount: ${orderAmount:F2}, Commission: ${commissionAmount:F2}, Writer: ${writerAmount:F2}",
                0,
                orderAmount,
                orderAmount,
                null,
                adminId,
                orderId,
                "Order"
            );

            // ── Audit Log ──
            _context.AuditLogs.Add(new AuditLog
            {
                Action = "Order Financials Processed",
                PerformedById = adminId,
                TargetUserId = order.AssignedWriterId,
                Description = $"Processed financials for Order #{order.OrderNumber}. Amount: ${orderAmount:F2}, Commission: ${commissionAmount:F2}, Writer Earnings: ${writerAmount:F2}",
                CreatedDate = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
            _logger.LogInformation("Financials processed for Order #{OrderNumber}: Amount={OrderAmount}, Commission={Commission}, WriterEarnings={WriterAmount}",
                order.OrderNumber, orderAmount, commissionAmount, writerAmount);
        }

        // ──────────────────────────────────────────────
        // Progressive Delivery Milestone Earnings
        // ──────────────────────────────────────────────

        public async Task<string> RecordMilestoneEarningsAsync(int orderId, int milestoneId, decimal amount, string? writerId, string? createdBy, string? description)
        {
            if (amount < 0)
                throw new InvalidOperationException("Milestone earnings cannot be negative.");

            // Get or create writer wallet
            WriterWallet? wallet = null;
            if (!string.IsNullOrEmpty(writerId))
            {
                wallet = await GetOrCreateWriterWalletAsync(writerId);
                wallet.PendingBalance += amount;
                wallet.LifetimeEarnings += amount;
                wallet.LastUpdated = DateTime.UtcNow;
            }

            // Create the ledger entry
            var balanceAfter = wallet?.PendingBalance ?? 0m;
            var txn = await CreateLedgerEntryAsync(
                TransactionType.WriterEarningAdded,
                description ?? "Milestone earnings",
                amount,
                amount,
                balanceAfter,
                writerId,
                createdBy,
                milestoneId,
                "Milestone");
            var txnNumber = txn.TransactionNumber;

            // Audit log
            _context.AuditLogs.Add(new AuditLog
            {
                Action = "Milestone Earnings Recorded",
                PerformedById = createdBy ?? "System",
                TargetUserId = writerId,
                Description = $"Recorded milestone earnings of ${amount:F2} for Order #{orderId} (Milestone #{milestoneId}). Txn: {txnNumber}",
                CreatedDate = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
            _logger.LogInformation("Milestone {MilestoneId} on order {OrderId}: ${Amount} recorded to writer {WriterId}, txn {Txn}",
                milestoneId, orderId, amount, writerId ?? "(unassigned)", txnNumber);

            return txnNumber;
        }

        // ──────────────────────────────────────────────
        // Release Writer Earnings (Pending → Available)
        // ──────────────────────────────────────────────

        public async Task ReleaseWriterEarningsAsync(string writerId, string adminId)
        {
            var wallet = await GetOrCreateWriterWalletAsync(writerId);
            var pendingAmount = wallet.PendingBalance;

            if (pendingAmount <= 0)
                throw new InvalidOperationException("No pending earnings to release.");

            var platformWallet = await GetPlatformWalletAsync();

            wallet.AvailableBalance += pendingAmount;
            wallet.PendingBalance = 0;
            wallet.LastUpdated = DateTime.UtcNow;

            // Move commission from platform pending to available
            platformWallet.AvailableBalance += platformWallet.PendingBalance;
            platformWallet.PendingBalance = 0;
            platformWallet.LastUpdated = DateTime.UtcNow;

            // Ledger entry: WriterEarningReleased
            await CreateLedgerEntryAsync(
                TransactionType.WriterEarningReleased,
                $"Pending earnings released: ${pendingAmount:F2} moved to available balance",
                pendingAmount,
                pendingAmount,
                wallet.AvailableBalance,
                writerId,
                adminId,
                null,
                "WriterWallet"
            );

            // Audit log
            _context.AuditLogs.Add(new AuditLog
            {
                Action = "Writer Earnings Released",
                PerformedById = adminId,
                TargetUserId = writerId,
                Description = $"Released ${pendingAmount:F2} from pending to available balance.",
                CreatedDate = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
            _logger.LogInformation("Earnings released for writer {WriterId}: ${Amount}", writerId, pendingAmount);
        }

        // ──────────────────────────────────────────────
        // Payout Request Processing
        // ──────────────────────────────────────────────

        public async Task<PayoutRequest> ProcessPayoutRequestAsync(string writerId, decimal amount)
        {
            if (!_payoutWindowService.IsPayoutWindowOpen())
                throw new InvalidOperationException("Payout requests are currently closed. Windows are open on the 1st and 15th of each month.");

            if (amount <= 0)
                throw new InvalidOperationException("Amount must be greater than zero.");

            var wallet = await GetOrCreateWriterWalletAsync(writerId);
            if (amount > wallet.AvailableBalance)
                throw new InvalidOperationException($"Insufficient available balance. Available: ${wallet.AvailableBalance:F2}, Requested: ${amount:F2}.");

            // Deduct from available balance
            wallet.AvailableBalance -= amount;
            wallet.LastUpdated = DateTime.UtcNow;

            var txnNumber = await GetNextTransactionNumberAsync();

            var payout = new PayoutRequest
            {
                WriterId = writerId,
                Amount = amount,
                RequestedDate = DateTime.UtcNow,
                Status = PayoutStatus.Pending,
                TransactionNumber = txnNumber
            };
            _context.PayoutRequests.Add(payout);

            // Ledger entry: PayoutRequested
            await CreateLedgerEntryAsync(
                TransactionType.PayoutRequested,
                $"Payout requested: ${amount:F2}",
                amount,
                0,
                wallet.AvailableBalance,
                writerId,
                writerId,
                null,
                "PayoutRequest"
            );

            // Audit log
            _context.AuditLogs.Add(new AuditLog
            {
                Action = "Payout Requested",
                PerformedById = writerId,
                TargetUserId = writerId,
                Description = $"Requested payout of ${amount:F2}. Transaction: {txnNumber}",
                CreatedDate = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
            _logger.LogInformation("Payout requested by writer {WriterId}: ${Amount}", writerId, amount);

            return payout;
        }

        public async Task ApprovePayoutAsync(int payoutId, string adminId)
        {
            var payout = await _context.PayoutRequests.FindAsync(payoutId);
            if (payout == null)
                throw new InvalidOperationException("Payout not found.");
            if (payout.Status != PayoutStatus.Pending)
                throw new InvalidOperationException("Only pending payouts can be approved.");

            payout.Status = PayoutStatus.Approved;
            payout.ApprovedDate = DateTime.UtcNow;
            payout.ProcessedById = adminId;

            // Get writer wallet for balance context
            var wallet = await GetOrCreateWriterWalletAsync(payout.WriterId);

            // Ledger entry: PayoutApproved
            await CreateLedgerEntryAsync(
                TransactionType.PayoutApproved,
                $"Payout approved: ${payout.Amount:F2}",
                0,
                payout.Amount,
                wallet.AvailableBalance,
                payout.WriterId,
                adminId,
                payout.Id,
                "PayoutRequest"
            );

            // Audit log
            _context.AuditLogs.Add(new AuditLog
            {
                Action = "Payout Approved",
                PerformedById = adminId,
                TargetUserId = payout.WriterId,
                Description = $"Approved payout request #{payout.Id} for ${payout.Amount:F2}.",
                CreatedDate = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
            _logger.LogInformation("Payout {PayoutId} approved by admin {AdminId}", payoutId, adminId);
        }

        public async Task RejectPayoutAsync(int payoutId, string adminId, string? notes)
        {
            var payout = await _context.PayoutRequests.FindAsync(payoutId);
            if (payout == null)
                throw new InvalidOperationException("Payout not found.");
            if (payout.Status != PayoutStatus.Pending)
                throw new InvalidOperationException("Only pending payouts can be rejected.");

            // Refund the amount back to available balance
            var wallet = await GetOrCreateWriterWalletAsync(payout.WriterId);
            wallet.AvailableBalance += payout.Amount;
            wallet.LastUpdated = DateTime.UtcNow;

            payout.Status = PayoutStatus.Rejected;
            payout.ApprovedDate = DateTime.UtcNow;
            payout.ProcessedById = adminId;
            payout.AdminNotes = notes;

            // Ledger entry: PayoutRejected
            await CreateLedgerEntryAsync(
                TransactionType.PayoutRejected,
                $"Payout rejected: ${payout.Amount:F2} refunded to available balance. Reason: {notes ?? "Not specified"}",
                0,
                payout.Amount,
                wallet.AvailableBalance,
                payout.WriterId,
                adminId,
                payout.Id,
                "PayoutRequest"
            );

            // Audit log
            _context.AuditLogs.Add(new AuditLog
            {
                Action = "Payout Rejected",
                PerformedById = adminId,
                TargetUserId = payout.WriterId,
                Description = $"Rejected payout request #{payout.Id} for ${payout.Amount:F2}. Reason: {notes ?? "Not specified"}",
                CreatedDate = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
            _logger.LogInformation("Payout {PayoutId} rejected by admin {AdminId}", payoutId, adminId);
        }

        public async Task MarkPayoutPaidAsync(int payoutId, string adminId)
        {
            var payout = await _context.PayoutRequests
                .Include(p => p.Writer)
                .FirstOrDefaultAsync(p => p.Id == payoutId);

            if (payout == null)
                throw new InvalidOperationException("Payout not found.");
            if (payout.Status != PayoutStatus.Approved)
                throw new InvalidOperationException("Only approved payouts can be marked as paid.");

            payout.Status = PayoutStatus.Paid;
            payout.ProcessedById = adminId;
            payout.PayoutDate = DateTime.UtcNow;

            // Update writer wallet lifetime payouts
            var writerWallet = await GetOrCreateWriterWalletAsync(payout.WriterId);
            writerWallet.LifetimePayouts += payout.Amount;
            writerWallet.LastUpdated = DateTime.UtcNow;

            // Update platform wallet
            var platformWallet = await GetPlatformWalletAsync();
            platformWallet.AvailableBalance -= payout.Amount;
            platformWallet.TotalPayouts += payout.Amount;
            platformWallet.LastUpdated = DateTime.UtcNow;

            // Ledger entry: PayoutPaid
            await CreateLedgerEntryAsync(
                TransactionType.PayoutPaid,
                $"Payout paid: ${payout.Amount:F2} to {payout.Writer?.Email ?? payout.WriterId}",
                payout.Amount,
                0,
                writerWallet.AvailableBalance,
                payout.WriterId,
                adminId,
                payout.Id,
                "PayoutRequest"
            );

            // Audit log
            _context.AuditLogs.Add(new AuditLog
            {
                Action = "Payout Paid",
                PerformedById = adminId,
                TargetUserId = payout.WriterId,
                Description = $"Marked payout request #{payout.Id} for ${payout.Amount:F2} as paid.",
                CreatedDate = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
            _logger.LogInformation("Payout {PayoutId} marked as paid by admin {AdminId}", payoutId, adminId);
        }

        // ──────────────────────────────────────────────
        // Transaction Queries
        // ──────────────────────────────────────────────

        public async Task<List<FinancialTransaction>> GetUserTransactionsAsync(string userId, int page = 1, int pageSize = 20)
        {
            return await _context.FinancialTransactions
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.CreatedDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<List<FinancialTransaction>> GetAllTransactionsAsync(int page = 1, int pageSize = 50)
        {
            return await _context.FinancialTransactions
                .OrderByDescending(t => t.CreatedDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<List<FinancialTransaction>> GetTransactionsByReferenceAsync(string referenceType, int referenceId)
        {
            return await _context.FinancialTransactions
                .Where(t => t.ReferenceType == referenceType && t.ReferenceId == referenceId)
                .OrderByDescending(t => t.CreatedDate)
                .ToListAsync();
        }

        // ──────────────────────────────────────────────
        // Writer Wallet
        // ──────────────────────────────────────────────

        public async Task<WriterWallet> GetOrCreateWriterWalletAsync(string writerId)
        {
            var wallet = await _context.WriterWallets
                .FirstOrDefaultAsync(w => w.WriterId == writerId);

            if (wallet == null)
            {
                wallet = new WriterWallet
                {
                    WriterId = writerId,
                    AvailableBalance = 0,
                    PendingBalance = 0,
                    LifetimeEarnings = 0,
                    LifetimeCommissionPaid = 0,
                    LifetimePayouts = 0,
                    LastUpdated = DateTime.UtcNow
                };
                _context.WriterWallets.Add(wallet);
                await _context.SaveChangesAsync();
            }

            return wallet;
        }

        public async Task<WriterWallet?> GetWriterWalletAsync(string writerId)
        {
            return await _context.WriterWallets
                .FirstOrDefaultAsync(w => w.WriterId == writerId);
        }

        public async Task<List<WriterWallet>> GetAllWriterWalletsAsync()
        {
            return await _context.WriterWallets
                .Include(w => w.Writer)
                .OrderByDescending(w => w.LifetimeEarnings)
                .ToListAsync();
        }

        // ──────────────────────────────────────────────
        // Order Financial Records
        // ──────────────────────────────────────────────

        public async Task<OrderFinancialRecord?> GetOrderFinancialRecordAsync(int orderId)
        {
            return await _context.OrderFinancialRecords
                .FirstOrDefaultAsync(r => r.OrderId == orderId);
        }

        public async Task<List<OrderFinancialRecord>> GetAllOrderFinancialRecordsAsync()
        {
            return await _context.OrderFinancialRecords
                .Include(r => r.Order)
                .OrderByDescending(r => r.CreatedDate)
                .ToListAsync();
        }

        // ──────────────────────────────────────────────
        // Payout Queries
        // ──────────────────────────────────────────────

        public async Task<List<PayoutRequest>> GetAllPayoutsAsync()
        {
            return await _context.PayoutRequests
                .Include(p => p.Writer)
                .Include(p => p.ProcessedBy)
                .OrderByDescending(p => p.RequestedDate)
                .ToListAsync();
        }

        public async Task<List<PayoutRequest>> GetPendingPayoutsAsync()
        {
            return await _context.PayoutRequests
                .Include(p => p.Writer)
                .Where(p => p.Status == PayoutStatus.Pending)
                .OrderByDescending(p => p.RequestedDate)
                .ToListAsync();
        }

        public async Task<List<PayoutRequest>> GetWriterPayoutsAsync(string writerId)
        {
            return await _context.PayoutRequests
                .Where(p => p.WriterId == writerId)
                .OrderByDescending(p => p.RequestedDate)
                .ToListAsync();
        }

        // ──────────────────────────────────────────────
        // Payment Details
        // ──────────────────────────────────────────────

        public async Task<WriterPaymentDetail> GetPaymentDetailsAsync(string writerId)
        {
            var details = await _context.WriterPaymentDetails
                .FirstOrDefaultAsync(d => d.WriterId == writerId);

            if (details == null)
            {
                details = new WriterPaymentDetail { WriterId = writerId };
                _context.WriterPaymentDetails.Add(details);
                await _context.SaveChangesAsync();
            }

            return details;
        }

        public async Task SavePaymentDetailsAsync(string writerId, string paymentMethod, string? accountName,
            string? accountNumber, string? phoneNumber, string? bankName, string? payPalEmail)
        {
            var details = await GetPaymentDetailsAsync(writerId);

            details.PaymentMethod = paymentMethod;
            details.AccountName = accountName;
            details.AccountNumber = accountNumber;
            details.PhoneNumber = phoneNumber;
            details.BankName = bankName;
            details.PayPalEmail = payPalEmail;
            details.LastUpdated = DateTime.UtcNow;

            await _context.SaveChangesAsync();
        }

        // ──────────────────────────────────────────────
        // Financial Dashboards
        // ──────────────────────────────────────────────

        public async Task<FinancialDashboardViewModel> GetAdminDashboardAsync()
        {
            var platformWallet = await GetPlatformWalletAsync();

            var pendingPayouts = await _context.PayoutRequests
                .Where(p => p.Status == PayoutStatus.Pending)
                .SumAsync(p => p.Amount);

            var approvedPayouts = await _context.PayoutRequests
                .Where(p => p.Status == PayoutStatus.Approved)
                .SumAsync(p => p.Amount);

            var paidPayouts = await _context.PayoutRequests
                .Where(p => p.Status == PayoutStatus.Paid)
                .SumAsync(p => p.Amount);

            var totalTransactions = await _context.FinancialTransactions.CountAsync();
            var pendingCount = await _context.PayoutRequests
                .CountAsync(p => p.Status == PayoutStatus.Pending);

            return new FinancialDashboardViewModel
            {
                TotalRevenue = platformWallet.LifetimeRevenue,
                TotalCommission = platformWallet.LifetimeCommission,
                PendingPayouts = pendingPayouts,
                ApprovedPayouts = approvedPayouts,
                PaidPayouts = paidPayouts,
                PlatformBalance = platformWallet.AvailableBalance,
                TotalTransactions = totalTransactions,
                PendingPayoutCount = pendingCount,
                PlatformWallet = platformWallet
            };
        }

        public async Task<WriterFinancialDashboardViewModel> GetWriterDashboardAsync(string writerId)
        {
            var wallet = await GetWriterWalletAsync(writerId);
            var recentTransactions = await GetUserTransactionsAsync(writerId, 1, 10);

            return new WriterFinancialDashboardViewModel
            {
                AvailableBalance = wallet?.AvailableBalance ?? 0,
                PendingBalance = wallet?.PendingBalance ?? 0,
                LifetimeEarnings = wallet?.LifetimeEarnings ?? 0,
                LifetimePayouts = wallet?.LifetimePayouts ?? 0,
                PayoutWindowOpen = _payoutWindowService.IsPayoutWindowOpen(),
                PayoutWindowMessage = _payoutWindowService.GetPayoutWindowMessage(),
                TimeUntilNextWindow = _payoutWindowService.GetTimeUntilNextWindow(),
                RecentTransactions = recentTransactions,
                Wallet = wallet
            };
        }

        // ──────────────────────────────────────────────
        // Financial Reports
        // ──────────────────────────────────────────────

        public async Task<List<RevenueReportRow>> GetRevenueReportAsync(DateTime? fromDate = null, DateTime? toDate = null)
        {
            var query = _context.OrderFinancialRecords
                .Include(r => r.Order)
                .AsQueryable();

            if (fromDate.HasValue)
                query = query.Where(r => r.CreatedDate >= fromDate.Value);
            if (toDate.HasValue)
                query = query.Where(r => r.CreatedDate <= toDate.Value);

            var records = await query.OrderByDescending(r => r.CreatedDate).ToListAsync();

            return records.GroupBy(r => r.CreatedDate.Date)
                .Select(g => new RevenueReportRow
                {
                    Period = g.Key.ToString("yyyy-MM-dd"),
                    Date = g.Key,
                    OrderCount = g.Count(),
                    TotalRevenue = g.Sum(r => r.OrderAmount),
                    TotalCommission = g.Sum(r => r.CommissionAmount),
                    TotalWriterEarnings = g.Sum(r => r.WriterAmount)
                })
                .OrderByDescending(r => r.Date)
                .ToList();
        }

        public async Task<List<CommissionReportRow>> GetCommissionReportAsync(DateTime? fromDate = null, DateTime? toDate = null)
        {
            var query = _context.OrderFinancialRecords.AsQueryable();

            if (fromDate.HasValue)
                query = query.Where(r => r.CreatedDate >= fromDate.Value);
            if (toDate.HasValue)
                query = query.Where(r => r.CreatedDate <= toDate.Value);

            var records = await query.OrderByDescending(r => r.CreatedDate).ToListAsync();

            return records.GroupBy(r => r.CreatedDate.Date)
                .Select(g => new CommissionReportRow
                {
                    Period = g.Key.ToString("yyyy-MM-dd"),
                    Date = g.Key,
                    OrderCount = g.Count(),
                    TotalCommission = g.Sum(r => r.CommissionAmount),
                    AverageCommission = g.Count() > 0 ? Math.Round(g.Average(r => r.CommissionAmount), 2) : 0
                })
                .OrderByDescending(r => r.Date)
                .ToList();
        }

        public async Task<List<WriterEarningsReportRow>> GetWriterEarningsReportAsync(
            DateTime? fromDate = null, DateTime? toDate = null, string? writerId = null)
        {
            var query = _context.OrderFinancialRecords
                .Include(r => r.Order)
                .AsQueryable();

            if (fromDate.HasValue)
                query = query.Where(r => r.CreatedDate >= fromDate.Value);
            if (toDate.HasValue)
                query = query.Where(r => r.CreatedDate <= toDate.Value);

            var records = await query.ToListAsync();

            // Get wallets for balance info
            var wallets = await _context.WriterWallets
                .Include(w => w.Writer)
                .ToListAsync();

            var result = records
                .Where(r => r.Order != null && r.Order.AssignedWriterId != null)
                .GroupBy(r => r.Order!.AssignedWriterId!)
                .Select(g =>
                {
                    var wallet = wallets.FirstOrDefault(w => w.WriterId == g.Key);
                    var writer = wallet?.Writer;
                    return new WriterEarningsReportRow
                    {
                        WriterId = g.Key,
                        WriterName = writer != null ? $"{writer.FirstName} {writer.LastName}" : g.Key,
                        OrderCount = g.Count(),
                        TotalEarnings = g.Sum(r => r.WriterAmount),
                        TotalCommissionPaid = g.Sum(r => r.CommissionAmount),
                        PendingBalance = wallet?.PendingBalance ?? 0,
                        AvailableBalance = wallet?.AvailableBalance ?? 0
                    };
                })
                .OrderByDescending(r => r.TotalEarnings)
                .ToList();

            if (!string.IsNullOrEmpty(writerId))
                result = result.Where(r => r.WriterId == writerId).ToList();

            return result;
        }

        public async Task<List<PayoutReportRow>> GetPayoutReportAsync(
            DateTime? fromDate = null, DateTime? toDate = null, string? writerId = null)
        {
            var query = _context.PayoutRequests
                .Include(p => p.Writer)
                .AsQueryable();

            if (fromDate.HasValue)
                query = query.Where(p => p.RequestedDate >= fromDate.Value);
            if (toDate.HasValue)
                query = query.Where(p => p.RequestedDate <= toDate.Value);
            if (!string.IsNullOrEmpty(writerId))
                query = query.Where(p => p.WriterId == writerId);

            var payouts = await query.OrderByDescending(p => p.RequestedDate).ToListAsync();

            return payouts.Select(p => new PayoutReportRow
            {
                PayoutId = p.Id,
                WriterId = p.WriterId,
                WriterName = $"{p.Writer?.FirstName ?? ""} {p.Writer?.LastName ?? ""}".Trim(),
                Amount = p.Amount,
                Status = p.Status.ToString(),
                RequestedDate = p.RequestedDate,
                ApprovedDate = p.ApprovedDate,
                PayoutDate = p.PayoutDate,
                TransactionNumber = p.TransactionNumber
            }).ToList();
        }
    }
}