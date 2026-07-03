using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ScholarRescue.Data;
using ScholarRescue.Models;
using ScholarRescue.Models.Security;

namespace ScholarRescue.Services
{
    /// <summary>
    /// Production Security & Compliance Framework implementation.
    /// Provides device tracking, session management, anomaly detection,
    /// security incidents, MFA support, data privacy, and security health scoring.
    /// </summary>
    public class SecurityService : ISecurityService
    {
        private readonly ScholarRescueDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<SecurityService> _logger;

        private static readonly HashSet<string> BlockedExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".exe", ".bat", ".cmd", ".ps1", ".vbs", ".js", ".sh", ".scr", ".com", ".msi", ".reg", ".inf"
        };

        public SecurityService(
            ScholarRescueDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<SecurityService> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        // ----------------------------------------------------------------
        // DEVICE TRACKING
        // ----------------------------------------------------------------

        public async Task<UserDevice> TrackDeviceAsync(string userId, string? deviceName,
            string? browser, string? os, string? ipAddress, string? country)
        {
            var existing = await _context.UserDevices
                .FirstOrDefaultAsync(d => d.UserId == userId && d.IPAddress == ipAddress
                    && d.Browser == browser && d.IsActive);

            if (existing != null)
            {
                existing.LastSeen = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return existing;
            }

            var device = new UserDevice
            {
                UserId = userId,
                DeviceName = deviceName,
                Browser = browser,
                OperatingSystem = os,
                IPAddress = ipAddress,
                Country = country,
                FirstSeen = DateTime.UtcNow,
                LastSeen = DateTime.UtcNow,
                IsActive = true
            };

            _context.UserDevices.Add(device);
            await _context.SaveChangesAsync();

            // Detect new device
            if (await DetectNewDeviceAsync(userId, browser, os))
            {
                await CreateIncidentAsync("New Device Login",
                    $"User {userId} logged in from a new device: {browser ?? "Unknown"} / {os ?? "Unknown"}",
                    "Medium", "SuspiciousLogin");
            }

            return device;
        }

        public async Task<List<UserDevice>> GetUserDevicesAsync(string userId)
        {
            return await _context.UserDevices
                .Where(d => d.UserId == userId && d.IsActive)
                .OrderByDescending(d => d.LastSeen)
                .ToListAsync();
        }

        public async Task<UserDevice?> GetDeviceByIdAsync(int deviceId, string userId)
        {
            return await _context.UserDevices
                .FirstOrDefaultAsync(d => d.Id == deviceId && d.UserId == userId);
        }

        public async Task RevokeDeviceAsync(int deviceId, string userId)
        {
            var device = await GetDeviceByIdAsync(deviceId, userId);
            if (device != null)
            {
                device.IsActive = false;
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> IsKnownDeviceAsync(string userId, string? ipAddress, string? browser)
        {
            return await _context.UserDevices
                .AnyAsync(d => d.UserId == userId && d.IPAddress == ipAddress
                    && d.Browser == browser && d.IsActive);
        }

        // ----------------------------------------------------------------
        // SESSION MANAGEMENT
        // ----------------------------------------------------------------

        public async Task<List<UserDevice>> GetActiveSessionsAsync(string userId)
        {
            return await _context.UserDevices
                .Where(d => d.UserId == userId && d.IsActive)
                .OrderByDescending(d => d.LastSeen)
                .ToListAsync();
        }

        public async Task TerminateSessionAsync(int deviceId, string userId)
        {
            await RevokeDeviceAsync(deviceId, userId);
            await LogSecurityEventAsync(userId, "Session Terminated", null, null, $"Device #{deviceId}");
        }

        public async Task TerminateAllSessionsAsync(string userId)
        {
            var devices = await _context.UserDevices
                .Where(d => d.UserId == userId && d.IsActive).ToListAsync();
            foreach (var d in devices) d.IsActive = false;
            await _context.SaveChangesAsync();
            await LogSecurityEventAsync(userId, "All Sessions Terminated", null, null, null);
        }

        // ----------------------------------------------------------------
        // LOGIN ANOMALY DETECTION
        // ----------------------------------------------------------------

        public async Task<bool> DetectImpossibleTravelAsync(string userId, string ipAddress, string? country)
        {
            var lastLogin = await _context.UserDevices
                .Where(d => d.UserId == userId && d.IsActive)
                .OrderByDescending(d => d.LastSeen)
                .FirstOrDefaultAsync();

            if (lastLogin == null || string.IsNullOrEmpty(country) || string.IsNullOrEmpty(lastLogin.Country))
                return false;

            // If last login was less than 1 hour ago from a different country, flag it
            if (lastLogin.Country != country && (DateTime.UtcNow - lastLogin.LastSeen).TotalHours < 1)
            {
                await CreateIncidentAsync("Impossible Travel Detected",
                    $"User {userId} logged in from {country} < 1 hour after {lastLogin.Country}. " +
                    $"IP: {ipAddress}, Previous IP: {lastLogin.IPAddress}",
                    "High", "SuspiciousLogin");
                return true;
            }

            return false;
        }

        public async Task<bool> DetectNewDeviceAsync(string userId, string? browser, string? os)
        {
            var count = await _context.UserDevices
                .CountAsync(d => d.UserId == userId && d.Browser == browser && d.OperatingSystem == os);
            return count <= 1; // First time seeing this browser/OS combo
        }

        public async Task<bool> DetectNewCountryAsync(string userId, string? country)
        {
            if (string.IsNullOrEmpty(country)) return false;
            var hasLoggedInFromCountry = await _context.UserDevices
                .AnyAsync(d => d.UserId == userId && d.Country == country);
            return !hasLoggedInFromCountry;
        }

        // ----------------------------------------------------------------
        // SECURITY INCIDENTS
        // ----------------------------------------------------------------

        public async Task<SecurityIncident> CreateIncidentAsync(string title, string description,
            string severity, string? category = null, string? assignedTo = null)
        {
            var incident = new SecurityIncident
            {
                Title = title,
                Description = description,
                Severity = severity,
                Status = "Open",
                Category = category,
                AssignedToId = assignedTo,
                CreatedAt = DateTime.UtcNow
            };

            _context.SecurityIncidents.Add(incident);
            await _context.SaveChangesAsync();

            _logger.LogWarning("Security Incident Created: [{Severity}] {Title}", severity, title);
            return incident;
        }

        public async Task<List<SecurityIncident>> GetIncidentsAsync(string? status = null, string? severity = null)
        {
            IQueryable<SecurityIncident> query = _context.SecurityIncidents;
            if (!string.IsNullOrEmpty(status)) query = query.Where(i => i.Status == status);
            if (!string.IsNullOrEmpty(severity)) query = query.Where(i => i.Severity == severity);
            return await query.OrderByDescending(i => i.CreatedAt).ToListAsync();
        }

        public async Task UpdateIncidentStatusAsync(int incidentId, string status, string? resolution = null)
        {
            var incident = await _context.SecurityIncidents.FindAsync(incidentId);
            if (incident == null) return;

            incident.Status = status;
            if (resolution != null) incident.Resolution = resolution;
            if (status is "Resolved" or "Closed") incident.ResolvedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        public async Task<SecurityIncident?> GetIncidentByIdAsync(int incidentId)
        {
            return await _context.SecurityIncidents.FindAsync(incidentId);
        }

        // ----------------------------------------------------------------
        // SECURITY AUDIT
        // ----------------------------------------------------------------

        public async Task LogSecurityEventAsync(string userId, string action, string? ipAddress,
            string? device, string? details = null)
        {
            _context.AuditLogs.Add(new AuditLog
            {
                Action = action,
                PerformedById = userId,
                PerformedBy = await _context.Users.FindAsync(userId) ?? null!,
                TargetUserId = userId,
                Description = details != null ? $"{action}: {details}" : action,
                CreatedDate = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
        }

        public async Task<List<AuditLog>> GetSecurityAuditLogsAsync(int page = 1, int pageSize = 50,
            string? userId = null, string? action = null)
        {
            IQueryable<AuditLog> query = _context.AuditLogs;
            if (!string.IsNullOrEmpty(userId)) query = query.Where(l => l.PerformedById == userId);
            if (!string.IsNullOrEmpty(action)) query = query.Where(l => l.Action == action);
            return await query.OrderByDescending(l => l.CreatedDate)
                .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        }

        // ----------------------------------------------------------------
        // MFA
        // ----------------------------------------------------------------

        public async Task<bool> IsMfaEnabledAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            return user != null && user.TwoFactorEnabled;
        }

        public async Task EnableMfaAsync(string userId, string mfaType)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return;
            user.TwoFactorEnabled = true;
            await _userManager.UpdateAsync(user);
            await LogSecurityEventAsync(userId, "MFA Enabled", null, null, $"Type: {mfaType}");
        }

        public async Task DisableMfaAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return;
            user.TwoFactorEnabled = false;
            await _userManager.UpdateAsync(user);
            await LogSecurityEventAsync(userId, "MFA Disabled", null, null, null);
        }

        // ----------------------------------------------------------------
        // DATA PRIVACY
        // ----------------------------------------------------------------

        public async Task<byte[]> ExportUserDataAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return Array.Empty<byte>();

            var devices = await GetUserDevicesAsync(userId);
            var roles = await _userManager.GetRolesAsync(user);

            var data = new
            {
                User = new
                {
                    user.Id,
                    user.UserName,
                    user.Email,
                    user.FirstName,
                    user.LastName,
                    user.PhoneNumber,
                    user.UserType,
                    user.CreatedDate,
                    Roles = roles,
                    TwoFactorEnabled = user.TwoFactorEnabled
                },
                Devices = devices.Select(d => new
                {
                    d.DeviceName, d.Browser, d.OperatingSystem, d.IPAddress,
                    d.Country, d.FirstSeen, d.LastSeen
                }),
                ExportedAt = DateTime.UtcNow
            };

            return JsonSerializer.SerializeToUtf8Bytes(data, new JsonSerializerOptions { WriteIndented = true });
        }

        public async Task DeleteUserDataAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return;

            user.Email = $"deleted_{userId}@scholarrescue.com";
            user.UserName = $"deleted_{userId}";
            user.FirstName = "Deleted";
            user.LastName = "User";
            user.PhoneNumber = null;
            user.IsDeleted = true;
            user.IsActive = false;

            await _userManager.UpdateAsync(user);
            await LogSecurityEventAsync(userId, "User Data Deleted", null, null, "GDPR/CCPA deletion request");
        }

        public async Task AnonymizeUserDataAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return;

            user.FirstName = "Anonymous";
            user.LastName = "User";
            user.PhoneNumber = null;
            await _userManager.UpdateAsync(user);
            await LogSecurityEventAsync(userId, "User Data Anonymized", null, null, null);
        }

        // ----------------------------------------------------------------
        // SECURITY HEALTH
        // ----------------------------------------------------------------

        public async Task<int> CalculateSecurityHealthScoreAsync()
        {
            var report = await GetSecurityHealthReportAsync();
            return report.TotalScore;
        }

        public async Task<SecurityHealthReport> GetSecurityHealthReportAsync()
        {
            var report = new SecurityHealthReport();

            // Authentication Score (25 pts)
            report.AuthenticationScore = 25;
            var totalUsers = await _context.Users.CountAsync();
            var mfaUsers = await _context.Users.CountAsync(u => u.TwoFactorEnabled);
            if (totalUsers > 0 && mfaUsers < totalUsers * 0.5) report.AuthenticationScore -= 5;
            if (mfaUsers < totalUsers * 0.2) report.AuthenticationScore -= 5;

            // Authorization Score (15 pts)
            report.AuthorizationScore = 15;
            var adminCount = (await _userManager.GetUsersInRoleAsync(RoleNames.Administrator)).Count;
            if (adminCount > 5) report.AuthorizationScore -= 3;
            if (adminCount > 10) report.AuthorizationScore -= 3;

            // Data Protection Score (20 pts)
            report.DataProtectionScore = 20;

            // Compliance Score (15 pts)
            report.ComplianceScore = 15;
            var openIncidents = await _context.SecurityIncidents.CountAsync(i => i.Status == "Open");
            if (openIncidents > 0) report.ComplianceScore -= 3;
            if (openIncidents > 5) report.ComplianceScore -= 3;

            // Monitoring Score (15 pts)
            report.MonitoringScore = 15;
            var recentAudits = await _context.AuditLogs.CountAsync(l =>
                l.CreatedDate > DateTime.UtcNow.AddDays(-1));
            if (recentAudits == 0) report.MonitoringScore -= 5;

            // Auditability Score (10 pts)
            report.AuditabilityScore = 10;

            report.TotalScore = report.AuthenticationScore + report.AuthorizationScore +
                report.DataProtectionScore + report.ComplianceScore +
                report.MonitoringScore + report.AuditabilityScore;

            report.OverallStatus = report.TotalScore switch
            {
                >= 90 => "Excellent",
                >= 70 => "Good",
                >= 50 => "Warning",
                _ => "Critical"
            };

            return report;
        }

        // ----------------------------------------------------------------
        // COMPLIANCE
        // ----------------------------------------------------------------

        public async Task<ComplianceStats> GetComplianceStatsAsync()
        {
            var today = DateTime.UtcNow.Date;
            return new ComplianceStats
            {
                TotalUsers = await _context.Users.CountAsync(),
                MfaEnabledCount = await _context.Users.CountAsync(u => u.TwoFactorEnabled),
                MfaAdoptionRate = await _context.Users.CountAsync() > 0
                    ? Math.Round((double)await _context.Users.CountAsync(u => u.TwoFactorEnabled) /
                        await _context.Users.CountAsync() * 100, 1)
                    : 0,
                FailedLoginsToday = await _context.AuditLogs.CountAsync(l =>
                    l.Action == "Login Failed" && l.CreatedDate > today),
                LockedAccounts = await _context.Users.CountAsync(u => u.LockoutEnd > DateTime.UtcNow),
                ActiveIncidents = await _context.SecurityIncidents.CountAsync(i => i.Status != "Resolved" && i.Status != "Closed"),
                AuditLogsToday = await _context.AuditLogs.CountAsync(l => l.CreatedDate > today),
                ComplianceScore = await CalculateSecurityHealthScoreAsync()
            };
        }

        // ----------------------------------------------------------------
        // FILE SECURITY
        // ----------------------------------------------------------------

        public bool IsFileTypeAllowed(string fileName)
        {
            var ext = Path.GetExtension(fileName);
            return !BlockedExtensions.Contains(ext);
        }

        public bool IsFileSizeValid(long fileSizeBytes, long maxSizeBytes)
        {
            return fileSizeBytes > 0 && fileSizeBytes <= maxSizeBytes;
        }
    }
}
