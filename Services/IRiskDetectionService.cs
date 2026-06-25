using ScholarRescue.Models;
using ScholarRescue.Models.Enums;

namespace ScholarRescue.Services
{
    /// <summary>
    /// Service for AI-powered risk detection, communication monitoring, and risk scoring.
    /// </summary>
    public interface IRiskDetectionService
    {
        // --- Message Scanning ---
        /// <summary>Scans message content for prohibited content. Returns modified text if blocked.</summary>
        Task<(bool IsBlocked, string ModifiedText, RiskAssessment? Assessment)> ScanMessageAsync(
            int? messageId, string senderId, string content, string entityType, string entityId, int? orderId = null);

        /// <summary>Schedules a background scan for an existing message.</summary>
        Task ScanExistingMessageAsync(int messageId);

        // --- Pattern Detection ---
        /// <summary>Detects phone numbers in text.</summary>
        bool ContainsPhoneNumber(string text, out string? detected);

        /// <summary>Detects email addresses in text.</summary>
        bool ContainsEmailAddress(string text, out string? detected);

        /// <summary>Detects social media handles/URLs in text.</summary>
        bool ContainsSocialMedia(string text, out string? detected);

        /// <summary>Detects external payment requests in text.</summary>
        bool ContainsExternalPaymentRequest(string text, out string? detected);

        // --- Risk Assessment ---
        /// <summary>Creates a risk assessment entry.</summary>
        Task<RiskAssessment> CreateRiskAssessmentAsync(string entityType, string entityId,
            RiskCategory category, int score, string reason, int? messageId = null, int? orderId = null,
            string? detectedContent = null, bool isBlocked = false);

        /// <summary>Updates user risk profile based on assessment.</summary>
        Task UpdateRiskProfileAsync(string userId, int additionalScore, string userRole);

        /// <summary>Gets or creates a risk profile for a user.</summary>
        Task<WriterRiskProfile> GetWriterRiskProfileAsync(string writerId);
        Task<ClientRiskProfile> GetClientRiskProfileAsync(string clientId);

        // --- Query ---
        /// <summary>Gets open risk assessments with pagination.</summary>
        Task<(List<RiskAssessment> Risks, int TotalCount)> GetOpenRisksAsync(int page = 1, int pageSize = 25,
            RiskCategory? category = null, RiskLevel? level = null);

        /// <summary>Gets all risk assessments for an entity.</summary>
        Task<List<RiskAssessment>> GetEntityRisksAsync(string entityType, string entityId);

        /// <summary>Gets risk statistics for the dashboard.</summary>
        Task<RiskDashboardStats> GetDashboardStatsAsync();

        // --- Admin Actions ---
        Task ResolveRiskAsync(int riskId, string resolvedById);
        Task WarnUserAsync(string userId, string reason, string adminId);
        Task RestrictMessagingAsync(string userId, string userRole, string adminId);
        Task FreezeAccountAsync(string userId, string userRole, string adminId);
    }

    /// <summary>
    /// Statistics for the risk dashboard.
    /// </summary>
    public class RiskDashboardStats
    {
        public int OpenRiskCount { get; set; }
        public int HighRiskUserCount { get; set; }
        public int BlockedMessageCount { get; set; }
        public int CommissionAvoidanceCount { get; set; }
        public int PredictedDisputeCount { get; set; }
        public int PredictedLateDeliveryCount { get; set; }
        public int TotalViolations { get; set; }
        public Dictionary<RiskCategory, int> RiskByCategory { get; set; } = new();
    }
}