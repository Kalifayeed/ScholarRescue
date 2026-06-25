using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using ScholarRescue.Data;
using ScholarRescue.Models;

namespace ScholarRescue.Services
{
    /// <summary>
    /// Multi-Account Fraud Detection Engine (Phase 12C).
    /// Detects duplicate accounts, shared IPs, generates Writer IDs, validates screen names.
    /// </summary>
    public class AccountFraudService : IAccountFraudService
    {
        private readonly ScholarRescueDbContext _context;
        private readonly INotificationService _notificationService;
        private readonly ILogger<AccountFraudService> _logger;

        public AccountFraudService(
            ScholarRescueDbContext context,
            INotificationService notificationService,
            ILogger<AccountFraudService> logger)
        {
            _context = context;
            _notificationService = notificationService;
            _logger = logger;
        }

        public async Task ScanUserAsync(string userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return;

            var emailMatches = await FindUsersByEmailAsync(user.Email ?? "");
            if (emailMatches.Count > 1)
                await CreateAlertAsync(userId, "DuplicateEmail", "Critical", 100,
                    $"Email {user.Email} is used by {emailMatches.Count} accounts.");

            if (!string.IsNullOrWhiteSpace(user.PhoneNumber))
            {
                var phoneMatches = await FindUsersByPhoneAsync(user.PhoneNumber);
                if (phoneMatches.Count > 1)
                    await CreateAlertAsync(userId, "DuplicatePhone", "Critical", 80,
                        $"Phone {user.PhoneNumber} is used by {phoneMatches.Count} accounts.");
            }

            if (!string.IsNullOrWhiteSpace(user.LastKnownIPAddress))
            {
                var sharedCount = await _context.Users
                    .CountAsync(u => u.Id != userId && u.LastKnownIPAddress == user.LastKnownIPAddress);
                if (sharedCount >= 2)
                {
                    var severity = sharedCount >= 4 ? "Critical" : sharedCount >= 3 ? "HighRisk" : "Warning";
                    await CreateAlertAsync(userId, "SharedIP", severity, 25,
                        $"IP {user.LastKnownIPAddress} shared by {sharedCount + 1} accounts.");
                }
            }

            await NotifyAdminsOfCriticalAlerts(userId);
        }

        public async Task<List<ApplicationUser>> FindUsersByEmailAsync(string email)
        {
            return await _context.Users
                .Where(u => u.Email != null && u.Email.ToLower() == email.ToLower())
                .AsNoTracking().ToListAsync();
        }

        public async Task<List<ApplicationUser>> FindUsersByPhoneAsync(string phone)
        {
            var normalized = NormalizePhone(phone);
            var all = await _context.Users
                .Where(u => u.PhoneNumber != null)
                .AsNoTracking().ToListAsync();
            return all.Where(u => NormalizePhone(u.PhoneNumber ?? "") == normalized).ToList();
        }

        public async Task<List<AccountFraudAlert>> DetectSharedIPsAsync(string ipAddress, string excludeUserId)
        {
            return await _context.Set<AccountFraudAlert>()
                .Where(a => a.AlertType == "SharedIP" && a.UserId != excludeUserId
                    && a.Description.Contains(ipAddress) && a.Status == "Open")
                .AsNoTracking().ToListAsync();
        }

        public async Task<List<AccountFraudAlert>> GetOpenAlertsAsync()
        {
            return await _context.Set<AccountFraudAlert>()
                .Include(a => a.User)
                .OrderByDescending(a => a.RiskScore)
                .ThenByDescending(a => a.CreatedAt)
                .Where(a => a.Status == "Open")
                .AsNoTracking().ToListAsync();
        }

        public async Task<List<AccountFraudAlert>> GetUserAlertsAsync(string userId)
        {
            return await _context.Set<AccountFraudAlert>()
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.CreatedAt)
                .AsNoTracking().ToListAsync();
        }

        public async Task<int> CalculateRiskScoreAsync(string userId)
        {
            var alerts = await _context.Set<AccountFraudAlert>()
                .Where(a => a.UserId == userId && a.Status == "Open")
                .AsNoTracking().ToListAsync();
            return alerts.Sum(a => a.RiskScore);
        }

        public async Task ResolveAlertAsync(int alertId, string adminId, string resolution)
        {
            var alert = await _context.Set<AccountFraudAlert>().FindAsync(alertId);
            if (alert == null) return;
            alert.Status = resolution == "dismiss" ? "Dismissed" : "Resolved";
            alert.ReviewedByAdminId = adminId;
            alert.ReviewedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            _logger.LogInformation("Fraud alert {AlertId} {Status} by {Admin}", alertId, alert.Status, adminId);
        }

        public async Task<string> GenerateWriterIdAsync()
        {
            var random = new Random();
            string writerId;
            do
            {
                writerId = $"SRW-{random.Next(10000, 99999):D5}";
            }
            while (await _context.Users.AnyAsync(u => u.WriterId == writerId));
            return writerId;
        }

        public string NormalizePhone(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone)) return "";
            var digits = Regex.Replace(phone, @"\D", "");
            if (digits.StartsWith("0") && digits.Length >= 10) digits = "254" + digits.Substring(1);
            if ((digits.StartsWith("7") || digits.StartsWith("1")) && digits.Length == 9) digits = "254" + digits;
            return digits;
        }

        public (bool Valid, string Message) ValidateScreenName(string screenName)
        {
            if (string.IsNullOrWhiteSpace(screenName)) return (false, "Screen name is required.");
            if (screenName.Length < 4) return (false, "Screen name must be at least 4 characters.");
            if (screenName.Length > 30) return (false, "Screen name must not exceed 30 characters.");
            if (!Regex.IsMatch(screenName, @"^[a-zA-Z0-9_]+$"))
                return (false, "Screen name may only contain letters, numbers, and underscores.");
            if (screenName.Contains("__")) return (false, "Screen name may not contain consecutive underscores.");
            var restricted = new[] { "admin", "root", "support", "help", "staff", "spam", "fraud" };
            if (restricted.Any(w => screenName.ToLower().Contains(w)))
                return (false, "Screen name contains restricted words.");
            return (true, "Screen name is valid.");
        }

        private async Task CreateAlertAsync(string userId, string alertType, string severity, int riskScore, string description)
        {
            var existing = await _context.Set<AccountFraudAlert>()
                .FirstOrDefaultAsync(a => a.UserId == userId && a.AlertType == alertType && a.Status == "Open");
            if (existing != null) return;

            _context.Set<AccountFraudAlert>().Add(new AccountFraudAlert
            {
                UserId = userId, AlertType = alertType, Severity = severity,
                RiskScore = riskScore, Description = description, CreatedAt = DateTime.UtcNow, Status = "Open"
            });
            _context.AuditLogs.Add(new AuditLog
            {
                Action = $"Fraud Alert: {alertType}", PerformedById = userId, TargetUserId = userId,
                Description = $"[{severity}] {description}", CreatedDate = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
            _logger.LogWarning("Fraud alert for {User}: {Type} ({Severity})", userId, alertType, severity);
        }

        private async Task NotifyAdminsOfCriticalAlerts(string userId)
        {
            var adminRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Administrator");
            if (adminRole == null) return;
            var adminIds = await _context.UserRoles.Where(ur => ur.RoleId == adminRole.Id)
                .Select(ur => ur.UserId).ToListAsync();
            foreach (var adminId in adminIds)
                await _notificationService.CreateNotificationAsync(adminId, "Fraud Alert",
                    "New fraud alerts detected. Review required.",
                    Models.Enums.NotificationType.SystemAlert, userId, "User");
        }
    }
}