using Microsoft.EntityFrameworkCore;
using ScholarRescue.Data;
using ScholarRescue.Models;
using ScholarRescue.Models.Security;

namespace ScholarRescue.Services
{
    public class TwoFactorService : ITwoFactorService
    {
        private readonly ScholarRescueDbContext _context;
        private readonly ILogger<TwoFactorService> _logger;
        private const int MaxAttempts = 5;
        private const int OtpExpiryMinutes = 5;

        public TwoFactorService(ScholarRescueDbContext context, ILogger<TwoFactorService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<string> GenerateOtpAsync(string userId, string purpose = "Login")
        {
            // Invalidate previous unused OTPs
            var existing = await _context.Set<TwoFactorVerification>()
                .Where(t => t.UserId == userId && t.Purpose == purpose && !t.IsUsed)
                .ToListAsync();
            foreach (var otp in existing) otp.IsUsed = true;

            var code = new Random().Next(100000, 999999).ToString();
            var verification = new TwoFactorVerification
            {
                UserId = userId,
                OtpCode = code,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(OtpExpiryMinutes),
                Purpose = purpose
            };
            _context.Set<TwoFactorVerification>().Add(verification);
            await _context.SaveChangesAsync();
            return code;
        }

        public async Task<bool> VerifyOtpAsync(string userId, string otpCode, string purpose = "Login")
        {
            if (await IsOtpLockedOutAsync(userId)) return false;

            var otp = await _context.Set<TwoFactorVerification>()
                .Where(t => t.UserId == userId && t.Purpose == purpose && !t.IsUsed)
                .OrderByDescending(t => t.CreatedAt)
                .FirstOrDefaultAsync();

            if (otp == null) return false;

            otp.AttemptCount++;

            if (otp.OtpCode == otpCode && otp.ExpiresAt > DateTime.UtcNow && otp.AttemptCount <= MaxAttempts)
            {
                otp.IsUsed = true;
                otp.VerifiedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return true;
            }

            await _context.SaveChangesAsync();
            return false;
        }

        public async Task<bool> ResendOtpAsync(string userId, string purpose = "Login")
        {
            var existing = await _context.Set<TwoFactorVerification>()
                .Where(t => t.UserId == userId && t.Purpose == purpose && !t.IsUsed)
                .ToListAsync();
            foreach (var otp in existing) otp.IsUsed = true;
            await _context.SaveChangesAsync();

            await GenerateOtpAsync(userId, purpose);
            return true;
        }

        public async Task<bool> IsOtpLockedOutAsync(string userId)
        {
            var recent = await _context.Set<TwoFactorVerification>()
                .Where(t => t.UserId == userId && t.CreatedAt > DateTime.UtcNow.AddMinutes(-15))
                .ToListAsync();
            return recent.Sum(t => t.AttemptCount) >= MaxAttempts;
        }

        public async Task<TwoFactorVerification?> GetLatestOtpAsync(string userId, string purpose = "Login")
        {
            return await _context.Set<TwoFactorVerification>()
                .Where(t => t.UserId == userId && t.Purpose == purpose)
                .OrderByDescending(t => t.CreatedAt)
                .FirstOrDefaultAsync();
        }
    }

    public class WriterQualityService : IWriterQualityService
    {
        private readonly ScholarRescueDbContext _context;

        public WriterQualityService(ScholarRescueDbContext context) { _context = context; }

        public async Task<WriterQualityScore> CalculateScoreAsync(string writerId)
        {
            var completed = await _context.Orders
                .Where(o => o.AssignedWriterId == writerId && o.Status == Models.Enums.OrderStatus.Completed)
                .ToListAsync();
            var allOrders = await _context.Orders
                .Where(o => o.AssignedWriterId == writerId).ToListAsync();

            var rating = completed.Any() ? completed.Average(o => o.Rating ?? 3) : 3;
            var onTime = completed.Any() ? completed.Count(o => o.CompletedAt <= o.Deadline) * 100.0 / completed.Count : 100;
            var revisions = allOrders.Any() ? allOrders.Count(o => o.Status == Models.Enums.OrderStatus.RevisionRequested || o.Status == Models.Enums.OrderStatus.RevisionSubmitted) * 100.0 / allOrders.Count : 0;
            var disputes = allOrders.Any() ? allOrders.Count(o => o.IsDisputed) * 100.0 / allOrders.Count : 0;

            var reliability = await _context.Set<WriterReliability>()
                .Where(r => r.WriterId == writerId)
                .Select(r => (double?)r.ReliabilityScore)
                .FirstOrDefaultAsync() ?? 100;

            var score = new WriterQualityScore
            {
                WriterId = writerId,
                ClientRatingScore = rating * 20, // 40% weight: 5*20=100
                OnTimeDeliveryScore = onTime * 0.25, // 25%
                RevisionRateScore = Math.Max(0, 100 - revisions) * 0.15, // 15%
                DisputeRateScore = Math.Max(0, 100 - disputes) * 0.10, // 10%
                ReliabilityScore = reliability * 0.10, // 10%
                OverallScore = (rating / 5.0 * 40) + (onTime / 100.0 * 25) + (Math.Max(0, 100 - revisions) / 100.0 * 15) + (Math.Max(0, 100 - disputes) / 100.0 * 10) + (reliability / 100.0 * 10),
                CompletedOrders = completed.Count,
                OnTimePercentage = onTime,
                RevisionPercentage = revisions,
                DisputePercentage = disputes,
                CalculatedAt = DateTime.UtcNow
            };

            _context.Set<WriterQualityScore>().Add(score);
            await _context.SaveChangesAsync();
            return score;
        }

        public async Task<WriterQualityScore?> GetLatestScoreAsync(string writerId) =>
            await _context.Set<WriterQualityScore>().Where(s => s.WriterId == writerId)
                .OrderByDescending(s => s.CalculatedAt).FirstOrDefaultAsync();

        public async Task<List<WriterQualityScore>> GetScoreHistoryAsync(string writerId, int count = 10) =>
            await _context.Set<WriterQualityScore>().Where(s => s.WriterId == writerId)
                .OrderByDescending(s => s.CalculatedAt).Take(count).ToListAsync();

        public async Task<double> GetPerformanceTrendAsync(string writerId)
        {
            var scores = await GetScoreHistoryAsync(writerId, 5);
            if (scores.Count < 2) return 0;
            return scores.First().OverallScore - scores.Last().OverallScore;
        }
    }

    public class WriterTierService : IWriterTierService
    {
        private readonly ScholarRescueDbContext _context;
        private readonly IWriterQualityService _qualityService;

        public WriterTierService(ScholarRescueDbContext context, IWriterQualityService qualityService)
        { _context = context; _qualityService = qualityService; }

        public async Task<WriterTier> EvaluateTierAsync(string writerId)
        {
            var score = await _qualityService.GetLatestScoreAsync(writerId) ?? await _qualityService.CalculateScoreAsync(writerId);
            var completed = score?.CompletedOrders ?? 0;
            var quality = score?.OverallScore ?? 0;

            string tier; decimal maxValue;
            if (completed >= 50 && quality >= 90) { tier = "Elite"; maxValue = 999999; }
            else if (completed >= 20 && quality >= 75) { tier = "Senior"; maxValue = 500; }
            else if (completed >= 5 && quality >= 60) { tier = "Intermediate"; maxValue = 150; }
            else { tier = "Beginner"; maxValue = 50; }

            var existing = await _context.Set<WriterTier>().FirstOrDefaultAsync(t => t.WriterId == writerId);
            if (existing != null)
            {
                var prevTier = existing.Tier;
                existing.Tier = tier;
                existing.MaxOrderValue = maxValue;
                existing.CompletedOrders = completed;
                existing.QualityScore = quality;
                existing.IsAutoPromoted = true;
                if (prevTier != tier)
                {
                    if (Array.IndexOf(new[] { "Beginner", "Intermediate", "Senior", "Elite" }, tier) >
                        Array.IndexOf(new[] { "Beginner", "Intermediate", "Senior", "Elite" }, prevTier))
                        existing.LastPromotedAt = DateTime.UtcNow;
                    else
                        existing.LastDemotedAt = DateTime.UtcNow;
                    existing.PromotionReason = $"Completed: {completed}, Quality: {quality:F1}";
                }
                await _context.SaveChangesAsync();
                return existing;
            }

            var newTier = new WriterTier
            {
                WriterId = writerId, Tier = tier, MaxOrderValue = maxValue,
                CompletedOrders = completed, QualityScore = quality,
                IsAutoPromoted = true, AssignedAt = DateTime.UtcNow,
                PromotionReason = $"Initial tier. Completed: {completed}, Quality: {quality:F1}"
            };
            _context.Set<WriterTier>().Add(newTier);
            await _context.SaveChangesAsync();
            return newTier;
        }

        public async Task<WriterTier?> GetCurrentTierAsync(string writerId) =>
            await _context.Set<WriterTier>().FirstOrDefaultAsync(t => t.WriterId == writerId);

        public async Task<bool> UpdateTierAsync(string writerId) { await EvaluateTierAsync(writerId); return true; }

        public async Task<decimal> GetMaxOrderValueAsync(string writerId)
        {
            var tier = await GetCurrentTierAsync(writerId);
            return tier?.MaxOrderValue ?? 50;
        }

        public async Task<List<WriterTier>> GetAllTiersAsync() =>
            await _context.Set<WriterTier>().ToListAsync();
    }

    public class FraudDetectionService : IFraudDetectionService
    {
        private readonly ScholarRescueDbContext _context;
        public FraudDetectionService(ScholarRescueDbContext context) { _context = context; }

        public async Task<FraudIncident> DetectAndHandleAsync(string userId, string detectionType, string description)
        {
            var incident = new FraudIncident
            {
                UserId = userId, DetectionType = detectionType, Description = description,
                Severity = "Warning", Action = "Warning", DetectedAt = DateTime.UtcNow
            };
            _context.Set<FraudIncident>().Add(incident);
            await _context.SaveChangesAsync();
            return incident;
        }

        public async Task<List<FraudIncident>> GetIncidentsAsync(string? userId = null, bool? resolved = null)
        {
            var q = _context.Set<FraudIncident>().AsQueryable();
            if (userId != null) q = q.Where(i => i.UserId == userId);
            if (resolved.HasValue) q = q.Where(i => i.IsResolved == resolved.Value);
            return await q.OrderByDescending(i => i.DetectedAt).ToListAsync();
        }

        public async Task ResolveIncidentAsync(int incidentId, string reviewedById, string resolutionNotes)
        {
            var i = await _context.Set<FraudIncident>().FindAsync(incidentId);
            if (i != null) { i.IsResolved = true; i.ReviewedById = reviewedById; i.ReviewedAt = DateTime.UtcNow; i.ResolutionNotes = resolutionNotes; await _context.SaveChangesAsync(); }
        }

        public async Task<int> GetActiveSuspensionCountAsync() =>
            await _context.Set<FraudIncident>().CountAsync(i => !i.IsResolved);
    }

    public class LoginSecurityService : ILoginSecurityService
    {
        private readonly ScholarRescueDbContext _context;
        public LoginSecurityService(ScholarRescueDbContext context) { _context = context; }

        public async Task LogLoginAttemptAsync(string userId, string ipAddress, bool successful, string? browser = null, string? os = null, string? failureReason = null)
        {
            var log = new LoginSecurityLog
            {
                UserId = userId, IPAddress = ipAddress, Browser = browser, OperatingSystem = os,
                IsSuccessful = successful, FailureReason = failureReason, LoginAt = DateTime.UtcNow
            };
            // Detect suspicious - multiple failed attempts from different IPs
            var recentFailures = await _context.Set<LoginSecurityLog>()
                .CountAsync(l => l.UserId == userId && !l.IsSuccessful && l.LoginAt > DateTime.UtcNow.AddMinutes(-15));
            log.IsSuspicious = recentFailures >= 5;
            if (log.IsSuspicious) log.AlertMessage = $"Suspicious: {recentFailures} failed login attempts in 15 min";

            _context.Set<LoginSecurityLog>().Add(log);
            await _context.SaveChangesAsync();
        }

        public async Task<List<LoginSecurityLog>> GetLoginHistoryAsync(string userId, int count = 20) =>
            await _context.Set<LoginSecurityLog>().Where(l => l.UserId == userId)
                .OrderByDescending(l => l.LoginAt).Take(count).ToListAsync();

        public async Task<bool> IsSuspiciousLoginAsync(string userId, string ipAddress, string? browser = null, string? os = null)
        {
            var lastLogin = await _context.Set<LoginSecurityLog>()
                .Where(l => l.UserId == userId && l.IsSuccessful)
                .OrderByDescending(l => l.LoginAt).Skip(1).FirstOrDefaultAsync();
            return lastLogin != null && lastLogin.IPAddress != ipAddress;
        }

        public async Task<List<LoginSecurityLog>> GetSuspiciousLoginsAsync() =>
            await _context.Set<LoginSecurityLog>().Where(l => l.IsSuspicious)
                .OrderByDescending(l => l.LoginAt).Take(100).ToListAsync();
    }

    public class AdminAuditService : IAdminAuditService
    {
        private readonly ScholarRescueDbContext _context;
        public AdminAuditService(ScholarRescueDbContext context) { _context = context; }

        public async Task LogActionAsync(string action, string performedById, string? targetUserId = null, int? targetOrderId = null, string? oldValue = null, string? newValue = null, string? reason = null, string? ipAddress = null)
        {
            var log = new AdministrativeActionLog
            {
                Action = action, PerformedById = performedById, TargetUserId = targetUserId,
                TargetOrderId = targetOrderId, OldValue = oldValue, NewValue = newValue,
                Reason = reason, CreatedAt = DateTime.UtcNow, IPAddress = ipAddress
            };
            _context.Set<AdministrativeActionLog>().Add(log);
            await _context.SaveChangesAsync();
        }

        public async Task<List<AdministrativeActionLog>> GetLogsAsync(string? action = null, int page = 1, int pageSize = 50)
        {
            var q = _context.Set<AdministrativeActionLog>().AsQueryable();
            if (action != null) q = q.Where(l => l.Action == action);
            return await q.OrderByDescending(l => l.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        }
    }
}