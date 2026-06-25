using ScholarRescue.Models;

namespace ScholarRescue.Services
{
    /// <summary>
    /// Service for writer wallet management and payout processing.
    /// </summary>
    public interface IWalletService
    {
        /// <summary>Gets or creates a wallet for a writer.</summary>
        Task<WriterWallet> GetOrCreateWalletAsync(string writerId);

        /// <summary>Credits a writer's pending balance after order completion.</summary>
        Task CreditWriterEarningsAsync(string writerId, decimal orderAmount, decimal commission, decimal earnings);

        /// <summary>Moves pending balance to available (after payout window conditions met).</summary>
        Task MovePendingToAvailableAsync(string writerId);

        /// <summary>Gets wallet with balance info.</summary>
        Task<WriterWallet?> GetWalletAsync(string writerId);

        /// <summary>Checks if payout window is currently open (1st or 15th).</summary>
        bool IsPayoutWindowOpen();

        /// <summary>Gets payout window status message.</summary>
        string GetPayoutWindowMessage();

        /// <summary>Creates a payout request.</summary>
        Task<PayoutRequest> RequestPayoutAsync(string writerId, decimal amount);

        /// <summary>Gets payout requests for a writer.</summary>
        Task<List<PayoutRequest>> GetWriterPayoutsAsync(string writerId);

        /// <summary>Gets all pending payout requests (admin).</summary>
        Task<List<PayoutRequest>> GetPendingPayoutsAsync();

        /// <summary>Gets all payout requests (admin).</summary>
        Task<List<PayoutRequest>> GetAllPayoutsAsync();

        /// <summary>Approves a payout request (admin).</summary>
        Task ApprovePayoutAsync(int payoutId, string adminId);

        /// <summary>Rejects a payout request (admin).</summary>
        Task RejectPayoutAsync(int payoutId, string adminId, string? notes = null);

        /// <summary>Marks a payout as paid (admin).</summary>
        Task MarkPayoutPaidAsync(int payoutId, string adminId);

        /// <summary>Gets or creates payment details for a writer.</summary>
        Task<WriterPaymentDetail> GetPaymentDetailsAsync(string writerId);

        /// <summary>Adds funds directly to writer's available balance (used by escrow release).</summary>
        Task AddFundsAsync(string writerId, decimal amount);

        /// <summary>Saves payment details for a writer.</summary>
        Task SavePaymentDetailsAsync(string writerId, string paymentMethod, string? accountName,
            string? accountNumber, string? phoneNumber, string? bankName, string? payPalEmail);
    }
}