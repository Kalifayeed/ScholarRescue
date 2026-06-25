using ScholarRescue.Models;
using ScholarRescue.Models.Enums;
using ScholarRescue.ViewModels.WriterResources;

namespace ScholarRescue.Services
{
    /// <summary>
    /// Manages writer rankings: metric aggregation, auto-promotion, and admin overrides.
    /// </summary>
    public interface IWriterRankingService
    {
        /// <summary>
        /// Get the ranking record for a specific writer. Creates one with rank=New if missing.
        /// </summary>
        Task<WriterRanking> GetOrCreateAsync(string writerId);

        /// <summary>
        /// Get all writer rankings (admin overview).
        /// </summary>
        Task<List<WriterRanking>> GetAllAsync();

        /// <summary>
        /// Get the rank progression history (current rank record) for a writer.
        /// </summary>
        Task<WriterRank> GetCurrentRankAsync(string writerId);

        /// <summary>
        /// Called when an order is completed to update writer metrics and trigger auto-promotion.
        /// </summary>
        Task UpdateMetricsOnCompletionAsync(string writerId);

        /// <summary>
        /// Called when a client submits a rating for an order.
        /// </summary>
        Task AddRatingAsync(string writerId, int rating);

        /// <summary>
        /// Increment revision count for a writer.
        /// </summary>
        Task IncrementRevisionsAsync(string writerId);

        /// <summary>
        /// Flag a writer order as a dispute.
        /// </summary>
        Task IncrementDisputesAsync(string writerId);

        /// <summary>
        /// Admin manually sets a writer's rank.
        /// </summary>
        Task<WriterRanking> OverrideRankAsync(string writerId, WriterRank newRank, string adminId, string? notes);

        /// <summary>
        /// Clear admin override so auto-promotion resumes.
        /// </summary>
        Task ClearOverrideAsync(string writerId);

        /// <summary>
        /// Auto-compute and apply the appropriate rank for a writer based on their metrics.
        /// </summary>
        Task<WriterRank> EvaluateAndApplyRankAsync(string writerId);

        /// <summary>
        /// Return the criteria thresholds (used for display).
        /// </summary>
        IReadOnlyList<RankCriteria> GetPromotionCriteria();

        /// <summary>
        /// Build a full writer performance analytics dashboard with monthly breakdown.
        /// </summary>
        Task<WriterAnalyticsViewModel> GetAnalyticsAsync(string writerId);
    }

    /// <summary>
    /// Display-friendly rank promotion criteria.
    /// </summary>
    public class RankCriteria
    {
        public WriterRank Rank { get; set; }
        public int MinCompletedOrders { get; set; }
        public double MinAverageRating { get; set; }
    }
}
