using ScholarRescue.Models.Enums;

namespace ScholarRescue.Models.Configuration
{
    /// <summary>
    /// Configuration for the Intelligent Writer Matching Engine.
    /// All weights are percentages (0-100) that sum to 100.
    /// Admin-adjustable via appsettings.json without code changes.
    /// </summary>
    public class MatchingConfiguration
    {
        /// <summary>Auto-assignment mode: Disabled, RecommendationOnly, or AutomaticAssignment.</summary>
        public AutoAssignmentMode AutoAssignmentMode { get; set; } = AutoAssignmentMode.RecommendationOnly;

        /// <summary>Weight for subject expertise match (default 30%).</summary>
        public int SubjectExpertiseWeight { get; set; } = 30;

        /// <summary>Weight for academic level qualification match (default 20%).</summary>
        public int AcademicLevelWeight { get; set; } = 20;

        /// <summary>Weight for writer reliability score (default 15%).</summary>
        public int ReliabilityWeight { get; set; } = 15;

        /// <summary>Weight for client rating score (default 10%).</summary>
        public int ClientRatingWeight { get; set; } = 10;

        /// <summary>Weight for quality score (default 10%).</summary>
        public int QualityScoreWeight { get; set; } = 10;

        /// <summary>Weight for current capacity/availability (default 5%).</summary>
        public int CapacityWeight { get; set; } = 5;

        /// <summary>Weight for deadline compatibility (default 5%).</summary>
        public int DeadlineCompatibilityWeight { get; set; } = 5;

        /// <summary>Weight for recent performance (default 5%).</summary>
        public int RecentPerformanceWeight { get; set; } = 5;

        /// <summary>Minimum reliability score for auto-assignment eligibility.</summary>
        public int MinReliabilityForAutoAssign { get; set; } = 90;

        /// <summary>Minimum quality score for auto-assignment eligibility.</summary>
        public int MinQualityForAutoAssign { get; set; } = 85;

        /// <summary>Minimum reliability score for premium order eligibility.</summary>
        public int MinReliabilityForPremium { get; set; } = 95;

        /// <summary>Minimum rating for premium order eligibility (e.g., 4.8).</summary>
        public double MinRatingForPremium { get; set; } = 4.8;

        /// <summary>Minimum quality score for premium order eligibility.</summary>
        public int MinQualityForPremium { get; set; } = 90;

        /// <summary>Maximum number of recommendations to return.</summary>
        public int MaxRecommendations { get; set; } = 10;

        /// <summary>Whether high-match orders trigger writer notifications.</summary>
        public bool NotifyWritersOnHighMatch { get; set; } = true;

        /// <summary>Whether admin gets notified when recommendations are generated.</summary>
        public bool NotifyAdminOnRecommendations { get; set; } = false;

        /// <summary>
        /// Validates that weights sum to 100.
        /// </summary>
        public bool AreWeightsValid()
        {
            var total = SubjectExpertiseWeight + AcademicLevelWeight + ReliabilityWeight
                + ClientRatingWeight + QualityScoreWeight + CapacityWeight
                + DeadlineCompatibilityWeight + RecentPerformanceWeight;
            return total == 100;
        }
    }
}