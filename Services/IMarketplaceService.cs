using ScholarRescue.Models;
using ScholarRescue.Models.Enums;

namespace ScholarRescue.Services
{
    /// <summary>
    /// Writer marketplace & specialization engine. Matches writers to eligible orders.
    /// </summary>
    public interface IMarketplaceService
    {
        /// <summary>Add/update writer specialization.</summary>
        Task<WriterSpecialization> SetSpecializationAsync(string writerId, string subject, string expertiseLevel, int yearsExperience);

        /// <summary>Get writer's specializations.</summary>
        Task<List<WriterSpecialization>> GetSpecializationsAsync(string writerId);

        /// <summary>Check if writer is eligible for an order based on subject + academic level.</summary>
        Task<bool> IsEligibleForOrderAsync(string writerId, int orderId);

        /// <summary>Get all eligible orders for a writer with match scores.</summary>
        Task<List<OrderMatch>> GetEligibleOrdersAsync(string writerId, string? subjectFilter = null, AcademicLevel? levelFilter = null);

        /// <summary>Get recommended writers for an order (admin assignment assistant).</summary>
        Task<List<MarketplaceWriterRecommendation>> GetRecommendedWritersAsync(int orderId, int maxResults = 10);

        /// <summary>Calculate match score between writer and order.</summary>
        int CalculateMatchScore(string writerSubjects, AcademicLevel writerMaxLevel, int orderSubject, AcademicLevel orderLevel, double writerRating, int reliability);
    }

    public class OrderMatch
    {
        public TutoringRequest Order { get; set; } = null!;
        public int MatchScore { get; set; }
        public bool SubjectMatch { get; set; }
        public bool AcademicLevelMatch { get; set; }
        public bool IsPremium { get; set; }
        public bool CanApply { get; set; }
        public string RestrictionReason { get; set; } = string.Empty;
    }

    public class MarketplaceWriterRecommendation
    {
        public string WriterId { get; set; } = string.Empty;
        public string WriterName { get; set; } = string.Empty;
        public int MatchScore { get; set; }
        public double Rating { get; set; }
        public int Reliability { get; set; }
        public int CompletedOrders { get; set; }
        public int ActiveOrders { get; set; }
        public int MaxOrders { get; set; }
        public string Rank { get; set; } = string.Empty;
        public bool IsRecommended { get; set; }
        public bool SubjectMatch { get; set; }
        public List<string> Specializations { get; set; } = new();
    }
}