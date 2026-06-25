using ScholarRescue.Models.Matching;

namespace ScholarRescue.ViewModels.Admin
{
    /// <summary>
    /// ViewModel for the Writer Recommendation Panel in the admin order view.
    /// Displays top matched writers with match scores and reasoning.
    /// </summary>
    public class WriterRecommendationsViewModel
    {
        public int OrderId { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public string OrderTitle { get; set; } = string.Empty;
        public string OrderSubject { get; set; } = string.Empty;
        public string AcademicLevel { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public decimal Budget { get; set; }
        public bool IsAssigned { get; set; }
        public string? AssignedWriterName { get; set; }
        public string? AssignedWriterId { get; set; }

        public List<RecommendationItem> Recommendations { get; set; } = new();
        public AutoAssignmentStatus AutoAssignment { get; set; } = new();
    }

    public class RecommendationItem
    {
        public int Rank { get; set; }
        public string WriterId { get; set; } = string.Empty;
        public string WriterName { get; set; } = string.Empty;
        public string WriterEmail { get; set; } = string.Empty;
        public double MatchPercentage { get; set; }
        public string Explanation { get; set; } = string.Empty;
        public double Rating { get; set; }
        public int CompletedOrders { get; set; }
        public double ReliabilityScore { get; set; }
        public int CapacityPercent { get; set; }
        public double QualityScore { get; set; }
    }

    public class AutoAssignmentStatus
    {
        public bool IsEnabled { get; set; }
        public bool IsRecommendationOnly { get; set; }
        public string StatusMessage { get; set; } = string.Empty;
        public bool CanAutoAssign { get; set; }
    }
}