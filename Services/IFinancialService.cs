using ScholarRescue.Models;

namespace ScholarRescue.Services
{
    /// <summary>
    /// Core financial ledger service for the Scholar Rescue platform.
    /// All financial events create immutable ledger entries.
    /// </summary>
    public interface IFinancialService
    {
        /// <summary>Gets or creates the singleton platform wallet.</summary>
        Task<PlatformWallet> GetPlatformWalletAsync();

        /// <summary>Records an order completion and processes commission.</summary>
        Task ProcessOrderCompletionAsync(int orderId, string adminId);

        /// <summary>
        /// Records writer earnings from an approved progressive-delivery milestone.
        /// Returns the generated transaction number.
        /// </summary>
        Task<string> RecordMilestoneEarningsAsync(int orderId, int milestoneId, decimal amount, string? writerId, string? createdBy, string? description);

        /// <summary>Releases writer earnings from pending to available.</summary>
        Task ReleaseWriterEarningsAsync(string writerId, string adminId);

        /// <summary>Processes a payout request.</summary>
        Task<PayoutRequest> ProcessPayoutRequestAsync(string writerId, decimal amount);

        /// <summary>Approves a payout request.</summary>
        Task ApprovePayoutAsync(int payoutId, string adminId);

        /// <summary>Rejects a payout request and refunds the balance.</summary>
        Task RejectPayoutAsync(int payoutId, string adminId, string? notes);

        /// <summary>Marks a payout as paid.</summary>
        Task MarkPayoutPaidAsync(int payoutId, string adminId);

        /// <summary>Gets transaction history for a user.</summary>
        Task<List<FinancialTransaction>> GetUserTransactionsAsync(string userId, int page = 1, int pageSize = 20);

        /// <summary>Gets all transactions (admin).</summary>
        Task<List<FinancialTransaction>> GetAllTransactionsAsync(int page = 1, int pageSize = 50);

        /// <summary>Gets transactions by reference.</summary>
        Task<List<FinancialTransaction>> GetTransactionsByReferenceAsync(string referenceType, int referenceId);

        /// <summary>Gets the last transaction number for auto-generation.</summary>
        Task<string> GetNextTransactionNumberAsync();

        /// <summary>Gets writer wallet with full details.</summary>
        Task<WriterWallet?> GetWriterWalletAsync(string writerId);

        /// <summary>Gets all writer wallets (admin).</summary>
        Task<List<WriterWallet>> GetAllWriterWalletsAsync();

        /// <summary>Gets order financial record for an order.</summary>
        Task<OrderFinancialRecord?> GetOrderFinancialRecordAsync(int orderId);

        /// <summary>Gets all order financial records.</summary>
        Task<List<OrderFinancialRecord>> GetAllOrderFinancialRecordsAsync();

        /// <summary>Gets financial dashboard data for admin.</summary>
        Task<FinancialDashboardViewModel> GetAdminDashboardAsync();

        /// <summary>Gets financial dashboard data for a writer.</summary>
        Task<WriterFinancialDashboardViewModel> GetWriterDashboardAsync(string writerId);

        /// <summary>Gets revenue report data.</summary>
        Task<List<RevenueReportRow>> GetRevenueReportAsync(DateTime? fromDate = null, DateTime? toDate = null);

        /// <summary>Gets commission report data.</summary>
        Task<List<CommissionReportRow>> GetCommissionReportAsync(DateTime? fromDate = null, DateTime? toDate = null);

        /// <summary>Gets writer earnings report data.</summary>
        Task<List<WriterEarningsReportRow>> GetWriterEarningsReportAsync(DateTime? fromDate = null, DateTime? toDate = null, string? writerId = null);

        /// <summary>Gets payout report data.</summary>
        Task<List<PayoutReportRow>> GetPayoutReportAsync(DateTime? fromDate = null, DateTime? toDate = null, string? writerId = null);

        /// <summary>Gets all payout requests (admin).</summary>
        Task<List<PayoutRequest>> GetAllPayoutsAsync();

        /// <summary>Gets pending payout requests (admin).</summary>
        Task<List<PayoutRequest>> GetPendingPayoutsAsync();

        /// <summary>Gets writer's payout requests.</summary>
        Task<List<PayoutRequest>> GetWriterPayoutsAsync(string writerId);

        /// <summary>Gets or creates writer wallet.</summary>
        Task<WriterWallet> GetOrCreateWriterWalletAsync(string writerId);

        /// <summary>Gets writer payment details.</summary>
        Task<WriterPaymentDetail> GetPaymentDetailsAsync(string writerId);

        /// <summary>Saves writer payment details.</summary>
        Task SavePaymentDetailsAsync(string writerId, string paymentMethod, string? accountName,
            string? accountNumber, string? phoneNumber, string? bankName, string? payPalEmail);
    }

    // Dashboard ViewModels

    public class FinancialDashboardViewModel
    {
        public decimal TotalRevenue { get; set; }
        public decimal TotalCommission { get; set; }
        public decimal PendingPayouts { get; set; }
        public decimal ApprovedPayouts { get; set; }
        public decimal PaidPayouts { get; set; }
        public decimal PlatformBalance { get; set; }
        public int TotalTransactions { get; set; }
        public int PendingPayoutCount { get; set; }
        public PlatformWallet? PlatformWallet { get; set; }
    }

    public class WriterFinancialDashboardViewModel
    {
        public decimal AvailableBalance { get; set; }
        public decimal PendingBalance { get; set; }
        public decimal LifetimeEarnings { get; set; }
        public decimal LifetimePayouts { get; set; }
        public bool PayoutWindowOpen { get; set; }
        public string PayoutWindowMessage { get; set; } = string.Empty;
        public TimeSpan TimeUntilNextWindow { get; set; }
        public List<FinancialTransaction> RecentTransactions { get; set; } = new();
        public WriterWallet? Wallet { get; set; }
    }

    // Report row models

    public class RevenueReportRow
    {
        public string Period { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public int OrderCount { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal TotalCommission { get; set; }
        public decimal TotalWriterEarnings { get; set; }
    }

    public class CommissionReportRow
    {
        public string Period { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public int OrderCount { get; set; }
        public decimal TotalCommission { get; set; }
        public decimal AverageCommission { get; set; }
    }

    public class WriterEarningsReportRow
    {
        public string WriterId { get; set; } = string.Empty;
        public string WriterName { get; set; } = string.Empty;
        public int OrderCount { get; set; }
        public decimal TotalEarnings { get; set; }
        public decimal TotalCommissionPaid { get; set; }
        public decimal PendingBalance { get; set; }
        public decimal AvailableBalance { get; set; }
    }

    public class PayoutReportRow
    {
        public int PayoutId { get; set; }
        public string WriterId { get; set; } = string.Empty;
        public string WriterName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime RequestedDate { get; set; }
        public DateTime? ApprovedDate { get; set; }
        public DateTime? PayoutDate { get; set; }
        public string? TransactionNumber { get; set; }
    }
}