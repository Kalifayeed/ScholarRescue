using ScholarRescue.Models;
using ScholarRescue.Models.Matching;

namespace ScholarRescue.Services.Matching
{
    /// <summary>
    /// Intelligent Writer Matching Engine.
    /// Calculates writer suitability scores and provides recommendations.
    /// </summary>
    public interface IWriterMatchingService
    {
        /// <summary>Calculate match scores for all eligible writers for an order. Returns top matches.</summary>
        Task<List<WriterMatchScore>> CalculateMatchScoresAsync(int orderId);

        /// <summary>Get top N recommended writers for an order (sorted by match percentage desc).</summary>
        Task<List<WriterMatchScore>> GetTopRecommendationsAsync(int orderId, int maxResults = 10);

        /// <summary>Get the top match for an order (highest scoring eligible writer).</summary>
        Task<WriterMatchScore?> GetTopMatchAsync(int orderId);

        /// <summary>Check if a writer is eligible for a specific order (capacity, reliability, penalties).</summary>
        Task<bool> IsWriterEligibleAsync(int orderId, string writerId);

        /// <summary>Get match score for a specific writer and order.</summary>
        Task<WriterMatchScore?> GetWriterMatchScoreAsync(int orderId, string writerId);

        /// <summary>Auto-assign the best writer to an order if auto-assignment is enabled and rules are met.</summary>
        Task<(bool Assigned, string? WriterId, string Message)> TryAutoAssignAsync(int orderId, string adminId);

        /// <summary>Generate matching explanation text for a given match score.</summary>
        string GenerateExplanation(WriterMatchScore score);

        /// <summary>Record assignment history for ML preparation.</summary>
        Task RecordAssignmentHistoryAsync(int orderId, List<string> recommendedWriterIds,
            string? assignedWriterId, bool wasAutoAssigned, double assignedMatchScore);

        /// <summary>Update assignment history outcome when order completes.</summary>
        Task UpdateAssignmentOutcomeAsync(int orderId, bool completedSuccessfully,
            double? clientRating, bool wasOnTime, int revisionCount, bool hadDispute);

        /// <summary>Get matching analytics for admin dashboard.</summary>
        Task<MatchingAnalytics> GetMatchingAnalyticsAsync();

        /// <summary>Notify writers about high-match available orders.</summary>
        Task NotifyHighMatchWritersAsync(int orderId);

        /// <summary>Rank writer applications for admin view.</summary>
        Task<List<WriterMatchScore>> RankApplicationsAsync(int orderId);
    }

    /// <summary>Analytics model for matching engine performance.</summary>
    public class MatchingAnalytics
    {
        public double AverageMatchScore { get; set; }
        public double AssignmentSuccessRate { get; set; }
        public double AutoAssignmentSuccessRate { get; set; }
        public int TotalRecommendationsGenerated { get; set; }
        public int TotalAutoAssignments { get; set; }
        public int TotalManualAssignments { get; set; }
        public List<TopWriterInfo> TopPerformingWriters { get; set; } = new();
        public List<string> BestSpecializations { get; set; } = new();
        public TimeSpan AverageAssignmentTime { get; set; }
    }

    /// <summary>Top writer info for analytics.</summary>
    public class TopWriterInfo
    {
        public string WriterId { get; set; } = string.Empty;
        public string WriterName { get; set; } = string.Empty;
        public double AverageMatchScore { get; set; }
        public int AssignmentsCompleted { get; set; }
        public double SuccessRate { get; set; }
    }
}