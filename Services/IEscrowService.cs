using ScholarRescue.Models;

namespace ScholarRescue.Services
{
    /// <summary>
    /// Escrow service for order funding. All funds pass through escrow - never directly.
    /// </summary>
    public interface IEscrowService
    {
        /// <summary>Create escrow account for a new order.</summary>
        Task<EscrowAccount> CreateEscrowAsync(int orderId, string clientId);

        /// <summary>Fund the escrow (mark payment received).</summary>
        Task<EscrowAccount> FundEscrowAsync(int orderId, string paymentMethod);

        /// <summary>Release funds to writer after client approval.</summary>
        Task<EscrowAccount> ReleaseFundsAsync(int orderId);

        /// <summary>Refund funds to client.</summary>
        Task<EscrowAccount> RefundEscrowAsync(int orderId, string adminId);

        /// <summary>Lock escrow when dispute opened.</summary>
        Task<EscrowAccount> LockEscrowForDisputeAsync(int orderId);

        /// <summary>Get escrow for an order.</summary>
        Task<EscrowAccount?> GetEscrowAsync(int orderId);

        /// <summary>Get all escrow accounts for admin.</summary>
        Task<List<EscrowAccount>> GetAllEscrowsAsync(string? filter = null);
    }
}