using ScholarRescue.Models;

namespace ScholarRescue.Services
{
    /// <summary>
    /// Centralized Security Framework - provides authentication security,
    /// device tracking, session management, threat detection, and security auditing.
    /// </summary>
    public interface ISecurityService
    {
        // --- Device Tracking ---
        Task<UserDevice> TrackDeviceAsync(string userId, string? deviceName, string? browser,
            string? os, string? ipAddress, string? country);
        Task<List<UserDevice>> GetUserDevicesAsync(string userId);
        Task<UserDevice?> GetDeviceByIdAsync(int deviceId, string userId);
        Task RevokeDeviceAsync(int deviceId, string userId);
        Task<bool> IsKnownDeviceAsync(string userId, string? ipAddress, string? browser);

        // --- Session Management ---
        Task<List<UserDevice>> GetActiveSessionsAsync(string userId);
        Task TerminateSessionAsync(int deviceId, string userId);
        Task TerminateAllSessionsAsync(string userId);

        // --- Login Anomaly Detection ---
        Task<bool> DetectImpossibleTravelAsync(string userId, string ipAddress, string? country);
        Task<bool> DetectNewDeviceAsync(string userId, string? browser, string? os);
        Task<bool> DetectNewCountryAsync(string userId, string? country);

        // --- Security Incidents ---
        Task<SecurityIncident> CreateIncidentAsync(string title, string description,
            string severity, string? category = null, string? assignedTo = null);
        Task<List<SecurityIncident>> GetIncidentsAsync(string? status = null, string? severity = null);
        Task UpdateIncidentStatusAsync(int incidentId, string status, string? resolution = null);
        Task<SecurityIncident?> GetIncidentByIdAsync(int incidentId);

        // --- Security Audit ---
        Task LogSecurityEventAsync(string userId, string action, string? ipAddress,
            string? device, string? details = null);
        Task<List<AuditLog>> GetSecurityAuditLogsAsync(int page = 1, int pageSize = 50,
            string? userId = null, string? action = null);

        // --- MFA ---
        Task<bool> IsMfaEnabledAsync(string userId);
        Task EnableMfaAsync(string userId, string mfaType);
        Task DisableMfaAsync(string userId);

        // --- Data Privacy ---
        Task<byte[]> ExportUserDataAsync(string userId);
        Task DeleteUserDataAsync(string userId);
        Task AnonymizeUserDataAsync(string userId);

        // --- Security Health ---
        Task<int> CalculateSecurityHealthScoreAsync();
        Task<SecurityHealthReport> GetSecurityHealthReportAsync();

        // --- Compliance ---
        Task<ComplianceStats> GetComplianceStatsAsync();

        // --- File Security ---
        bool IsFileTypeAllowed(string fileName);
        bool IsFileSizeValid(long fileSizeBytes, long maxSizeBytes);
    }

    public class SecurityHealthReport
    {
        public int TotalScore { get; set; } // 0-100
        public int AuthenticationScore { get; set; }
        public int AuthorizationScore { get; set; }
        public int DataProtectionScore { get; set; }
        public int ComplianceScore { get; set; }
        public int MonitoringScore { get; set; }
        public int AuditabilityScore { get; set; }
        public string OverallStatus { get; set; } = "Good"; // Excellent, Good, Warning, Critical
    }

    public class ComplianceStats
    {
        public int TotalUsers { get; set; }
        public int MfaEnabledCount { get; set; }
        public double MfaAdoptionRate { get; set; }
        public int FailedLoginsToday { get; set; }
        public int LockedAccounts { get; set; }
        public int ActiveIncidents { get; set; }
        public int AuditLogsToday { get; set; }
        public double ComplianceScore { get; set; }
    }
}