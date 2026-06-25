using Microsoft.EntityFrameworkCore;
using ScholarRescue.Data;
using ScholarRescue.Models;

namespace ScholarRescue.Services
{
    /// <summary>
    /// Implementation of wallet and payout management.
    /// </summary>
    public class WalletService : IWalletService
    {
        private readonly ScholarRescueDbContext _context;
        private readonly ILogger<WalletService> _logger;

        public WalletService(ScholarRescueDbContext context, ILogger<WalletService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<WriterWallet> GetOrCreateWalletAsync(string writerId)
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
                    LastUpdated = DateTime.UtcNow
                };
                _context.WriterWallets.Add(wallet);
                await _context.SaveChangesAsync();
            }

            return wallet;
        }

        public async Task CreditWriterEarningsAsync(string writerId, decimal orderAmount, decimal commission, decimal earnings)
        {
            var wallet = await GetOrCreateWalletAsync(writerId);

            wallet.PendingBalance += earnings;
            wallet.LifetimeEarnings += earnings;
            wallet.LifetimeCommissionPaid += commission;
            wallet.LastUpdated = DateTime.UtcNow;

            await _context.SaveChangesAsync();
        }

        public async Task MovePendingToAvailableAsync(string writerId)
        {
            var wallet = await GetOrCreateWalletAsync(writerId);
            wallet.AvailableBalance += wallet.PendingBalance;
            wallet.PendingBalance = 0;
            wallet.LastUpdated = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        public async Task<WriterWallet?> GetWalletAsync(string writerId)
        {
            return await _context.WriterWallets
                .FirstOrDefaultAsync(w => w.WriterId == writerId);
        }

        public bool IsPayoutWindowOpen()
        {
            var now = DateTime.UtcNow;
            // Day 1 or Day 15 of the month, any time within that UTC day
            return now.Day == 1 || now.Day == 15;
        }

        public string GetPayoutWindowMessage()
        {
            if (IsPayoutWindowOpen())
                return "Payout requests are currently open.";

            var now = DateTime.UtcNow;
            var nextWindow = GetNextPayoutWindow(now);
            return $"Payout requests are currently closed. Next window: {nextWindow:MMMM dd, yyyy} (00:00 - 23:59 UTC).";
        }

        private static DateTime GetNextPayoutWindow(DateTime now)
        {
            // Check 1st of this month or next
            var firstOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var fifteenthOfMonth = new DateTime(now.Year, now.Month, 15, 0, 0, 0, DateTimeKind.Utc);

            if (now < firstOfMonth) return firstOfMonth;
            if (now < fifteenthOfMonth && now.Day < 15) return fifteenthOfMonth;

            // Next month 1st
            var nextMonth = now.AddMonths(1);
            return new DateTime(nextMonth.Year, nextMonth.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        }

        public async Task<PayoutRequest> RequestPayoutAsync(string writerId, decimal amount)
        {
            if (!IsPayoutWindowOpen())
                throw new InvalidOperationException("Payout requests are currently closed.");

            var wallet = await GetOrCreateWalletAsync(writerId);

            if (amount <= 0)
                throw new InvalidOperationException("Amount must be greater than zero.");

            if (amount > wallet.AvailableBalance)
                throw new InvalidOperationException("Insufficient available balance.");

            var payout = new PayoutRequest
            {
                WriterId = writerId,
                Amount = amount,
                RequestedDate = DateTime.UtcNow,
                Status = PayoutStatus.Pending
            };

            // Deduct from available balance
            wallet.AvailableBalance -= amount;
            wallet.LastUpdated = DateTime.UtcNow;

            _context.PayoutRequests.Add(payout);
            await _context.SaveChangesAsync();

            return payout;
        }

        public async Task<List<PayoutRequest>> GetWriterPayoutsAsync(string writerId)
        {
            return await _context.PayoutRequests
                .Where(p => p.WriterId == writerId)
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

        public async Task<List<PayoutRequest>> GetAllPayoutsAsync()
        {
            return await _context.PayoutRequests
                .Include(p => p.Writer)
                .Include(p => p.ProcessedBy)
                .OrderByDescending(p => p.RequestedDate)
                .ToListAsync();
        }

        public async Task ApprovePayoutAsync(int payoutId, string adminId)
        {
            var payout = await _context.PayoutRequests.FindAsync(payoutId);
            if (payout == null) throw new InvalidOperationException("Payout not found.");

            payout.Status = PayoutStatus.Approved;
            payout.ApprovedDate = DateTime.UtcNow;
            payout.ProcessedById = adminId;

            await _context.SaveChangesAsync();
        }

        public async Task RejectPayoutAsync(int payoutId, string adminId, string? notes = null)
        {
            var payout = await _context.PayoutRequests.FindAsync(payoutId);
            if (payout == null) throw new InvalidOperationException("Payout not found.");

            // Refund the amount back to available balance
            var wallet = await GetOrCreateWalletAsync(payout.WriterId);
            wallet.AvailableBalance += payout.Amount;
            wallet.LastUpdated = DateTime.UtcNow;

            payout.Status = PayoutStatus.Rejected;
            payout.ApprovedDate = DateTime.UtcNow;
            payout.ProcessedById = adminId;
            payout.AdminNotes = notes;

            await _context.SaveChangesAsync();
        }

        public async Task MarkPayoutPaidAsync(int payoutId, string adminId)
        {
            var payout = await _context.PayoutRequests.FindAsync(payoutId);
            if (payout == null) throw new InvalidOperationException("Payout not found.");

            payout.Status = PayoutStatus.Paid;
            payout.ProcessedById = adminId;

            await _context.SaveChangesAsync();
        }

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

        public async Task AddFundsAsync(string writerId, decimal amount)
        {
            var wallet = await GetOrCreateWalletAsync(writerId);
            wallet.AvailableBalance += amount;
            wallet.LifetimeEarnings += amount;
            wallet.LastUpdated = DateTime.UtcNow;
            await _context.SaveChangesAsync();
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
    }
}