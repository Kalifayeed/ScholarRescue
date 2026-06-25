using ScholarRescue.Models;

namespace ScholarRescue.Services
{
    /// <summary>
    /// Multi-Account Fraud Detection Engine (Phase 12C extension).
    /// Adds screen names, Writer IDs, duplicate account detection, and device tracking.
    /// </summary>
    public interface IAccountFraudService
    {
        /// <summary>Scan a user for all fraud indicators after registration.</summary>
        Task ScanUserAsync(string userId);

        /// <summary>Check if an email is already used by another account.</summary>
        Task<List<ApplicationUser>> FindUsersByEmailAsync(string email);

        /// <summary>Check if a phone number is already used (normalized).</summary>
        Task<List<ApplicationUser>> FindUsersByPhoneAsync(string phone);

        /// <summary>Detect shared IP addresses across accounts.</summary>
        Task<List<AccountFraudAlert>> DetectSharedIPsAsync(string ipAddress, string excludeUserId);

        /// <summary>Get all open fraud alerts.</summary>
        Task<List<AccountFraudAlert>> GetOpenAlertsAsync();

        /// <summary>Get fraud alerts for a specific user.</summary>
        Task<List<AccountFraudAlert>> GetUserAlertsAsync(string userId);

        /// <summary>Calculate risk score for a user.</summary>
        Task<int> CalculateRiskScoreAsync(string userId);

        /// <summary>Resolve a fraud alert.</summary>
        Task ResolveAlertAsync(int alertId, string adminId, string resolution);

        /// <summary>Generate a unique Writer ID (e.g., SRW-10458).</summary>
        Task<string> GenerateWriterIdAsync();

        /// <summary>Normalize a phone number for comparison.</summary>
        string NormalizePhone(string phone);

        /// <summary>Validate screen name format.</summary>
        (bool Valid, string Message) ValidateScreenName(string screenName);
    }
}