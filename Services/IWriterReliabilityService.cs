using ScholarRescue.Models;

namespace ScholarRescue.Services
{
    public interface IWriterReliabilityService
    {
        /// <summary>Get or create reliability record for a writer.</summary>
        Task<WriterReliability> GetOrCreateAsync(string writerId);

        /// <summary>Apply a penalty or bonus and log it.</summary>
        Task ApplyPenaltyAsync(string writerId, string action, string reason, int points,
            bool isDeduction, string? createdBy = null);

        /// <summary>Get tier label based on score.</summary>
        string GetTier(int score);

        /// <summary>Check if writer can accept premium orders.</summary>
        Task<bool> CanAcceptPremiumOrdersAsync(string writerId);

        /// <summary>Get max concurrent orders allowed for this writer.</summary>
        Task<int> GetMaxAllowedOrdersAsync(string writerId);

        /// <summary>Get penalty history for a writer.</summary>
        Task<List<WriterPenaltyLog>> GetPenaltyLogAsync(string writerId, int page = 1, int pageSize = 50);

        /// <summary>Get all reliability records for admin.</summary>
        Task<List<WriterReliability>> GetAllAsync(string? search = null, int? minScore = null, int? maxScore = null);

        /// <summary>Get risk level label.</summary>
        string GetRiskLevel(int score);
    }
}