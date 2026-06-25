using ScholarRescue.Models.Enums;

namespace ScholarRescue.Services
{
    /// <summary>
    /// Writer recommendation DTO for smart assignment (capacity service version).
    /// </summary>
    public class WriterRecommendation
    {
        public string WriterId { get; set; } = string.Empty;
        public string WriterName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public WriterRank Rank { get; set; }
        public string RankName { get; set; } = string.Empty;
        public int ActiveOrders { get; set; }
        public int MaxOrders { get; set; }
        public int CapacityPercent { get; set; }
        public WriterAvailabilityStatus Availability { get; set; }
        public int QualityScore { get; set; }
        public int CompletedOrders { get; set; }
        public double AverageRating { get; set; }
        public bool SubjectMatch { get; set; }
        public double RecommendationScore { get; set; }
        public bool IsRecommended { get; set; }
    }

    /// <summary>
    /// Manages writer capacity, workload tracking, quality scoring, and subject matching.
    /// </summary>
    public interface IWriterCapacityService
    {
        /// <summary>Recalculates and updates a writer's availability status based on current load.</summary>
        Task UpdateAvailabilityStatusAsync(string writerId);

        /// <summary>Gets the capacity percentage (0-100) for a writer.</summary>
        Task<int> GetCapacityPercentageAsync(string writerId);

        /// <summary>Checks if a writer can accept new orders.</summary>
        Task<bool> CanAcceptOrderAsync(string writerId);

        /// <summary>Increments active order count when order is assigned.</summary>
        Task IncrementActiveOrdersAsync(string writerId);

        /// <summary>Decrements active order count when order completes/reassigned/cancelled.</summary>
        Task DecrementActiveOrdersAsync(string writerId);

        /// <summary>Updates quality score based on event.</summary>
        Task UpdateQualityScoreAsync(string writerId, string eventType);

        /// <summary>Returns recommended writers for an order based on subject match + capacity.</summary>
        Task<List<WriterRecommendation>> GetRecommendedWritersAsync(int orderId, int maxResults = 5);

        /// <summary>Checks if a writer's subject specializations match an order subject.</summary>
        bool SubjectMatches(string writerSubjects, string orderSubject);

        /// <summary>Gets computed rank based on completed orders.</summary>
        WriterRank GetComputedRank(int completedOrders);

        /// <summary>Gets the display name for a WriterRank.</summary>
        string GetRankDisplayName(WriterRank rank);
    }

}