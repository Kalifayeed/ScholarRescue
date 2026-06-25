using ScholarRescue.Models.Security;

namespace ScholarRescue.Services
{
    public interface ITwoFactorService
    {
        Task<string> GenerateOtpAsync(string userId, string purpose = "Login");
        Task<bool> VerifyOtpAsync(string userId, string otpCode, string purpose = "Login");
        Task<bool> ResendOtpAsync(string userId, string purpose = "Login");
        Task<bool> IsOtpLockedOutAsync(string userId);
        Task<TwoFactorVerification?> GetLatestOtpAsync(string userId, string purpose = "Login");
    }
}

public interface IWriterQualityService
{
    Task<WriterQualityScore> CalculateScoreAsync(string writerId);
    Task<WriterQualityScore?> GetLatestScoreAsync(string writerId);
    Task<List<WriterQualityScore>> GetScoreHistoryAsync(string writerId, int count = 10);
    Task<double> GetPerformanceTrendAsync(string writerId);
}

public interface IWriterTierService
{
    Task<WriterTier> EvaluateTierAsync(string writerId);
    Task<WriterTier?> GetCurrentTierAsync(string writerId);
    Task<bool> UpdateTierAsync(string writerId);
    Task<decimal> GetMaxOrderValueAsync(string writerId);
    Task<List<WriterTier>> GetAllTiersAsync();
}

public interface IFraudDetectionService
{
    Task<FraudIncident> DetectAndHandleAsync(string userId, string detectionType, string description);
    Task<List<FraudIncident>> GetIncidentsAsync(string? userId = null, bool? resolved = null);
    Task ResolveIncidentAsync(int incidentId, string reviewedById, string resolutionNotes);
    Task<int> GetActiveSuspensionCountAsync();
}

public interface ILoginSecurityService
{
    Task LogLoginAttemptAsync(string userId, string ipAddress, bool successful, string? browser = null, string? os = null, string? failureReason = null);
    Task<List<LoginSecurityLog>> GetLoginHistoryAsync(string userId, int count = 20);
    Task<bool> IsSuspiciousLoginAsync(string userId, string ipAddress, string? browser = null, string? os = null);
    Task<List<LoginSecurityLog>> GetSuspiciousLoginsAsync();
}

public interface IAdminAuditService
{
    Task LogActionAsync(string action, string performedById, string? targetUserId = null, int? targetOrderId = null, string? oldValue = null, string? newValue = null, string? reason = null, string? ipAddress = null);
    Task<List<AdministrativeActionLog>> GetLogsAsync(string? action = null, int page = 1, int pageSize = 50);
}