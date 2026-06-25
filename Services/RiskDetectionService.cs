using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ScholarRescue.Data;
using ScholarRescue.Models;
using ScholarRescue.Models.Enums;

namespace ScholarRescue.Services
{
    /// <summary>
    /// AI Risk Detection Engine - monitors communications for prohibited content,
    /// calculates risk scores, manages user risk profiles, and supports admin actions.
    /// </summary>
    public class RiskDetectionService : IRiskDetectionService
    {
        private readonly ScholarRescueDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<RiskDetectionService> _logger;

        // Regex patterns for detection
        private static readonly Regex PhonePattern = new(
            @"(?:\+?\d{1,3}[-.\s]?)?\(?\d{2,4}\)?[-.\s]?\d{3,4}[-.\s]?\d{3,4}(?:\s?(?:ext|x|xtn)\s?\d{1,5})?",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex EmailPattern = new(
            @"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex SocialMediaPattern = new(
            @"(?:telegram\.me|wa\.me|discord\.gg|t\.me|instagram\.com|facebook\.com|twitter\.com|x\.com|linkedin\.com|snapchat|tiktok\.com|tiktok\.com/@)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex SocialHandlePattern = new(
            @"(?:@\w{3,})",
            RegexOptions.Compiled);

        private static readonly Regex ExternalPaymentPattern = new(
            @"(?:pay\s+me\s+(?:directly|outside)|send\s+money\s+to|paypal\s+me|cashapp|venmo|m-pesa|bank\s+transfer\s+me|work\s+(?:outside|direct)|bypass\s+platform|avoid\s+fee)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public RiskDetectionService(
            ScholarRescueDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<RiskDetectionService> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        // ----------------------------------------------------------------
        // PATTERN DETECTION
        // ----------------------------------------------------------------

        public bool ContainsPhoneNumber(string text, out string? detected)
        {
            var match = PhonePattern.Match(text);
            detected = match.Success ? match.Value : null;
            return match.Success;
        }

        public bool ContainsEmailAddress(string text, out string? detected)
        {
            var match = EmailPattern.Match(text);
            detected = match.Success ? match.Value : null;
            return match.Success;
        }

        public bool ContainsSocialMedia(string text, out string? detected)
        {
            var match1 = SocialMediaPattern.Match(text);
            var match2 = SocialHandlePattern.Match(text);
            // Filter out @mentions that are just words (3+ chars)
            detected = match1.Success ? match1.Value : (match2.Success ? match2.Value : null);
            return match1.Success || match2.Success;
        }

        public bool ContainsExternalPaymentRequest(string text, out string? detected)
        {
            var match = ExternalPaymentPattern.Match(text);
            detected = match.Success ? match.Value : null;
            return match.Success;
        }

        // ----------------------------------------------------------------
        // MESSAGE SCANNING
        // ----------------------------------------------------------------

        public async Task<(bool IsBlocked, string ModifiedText, RiskAssessment? Assessment)> ScanMessageAsync(
            int? messageId, string senderId, string content, string entityType, string entityId, int? orderId = null)
        {
            var modifiedText = content;
            bool isBlocked = false;
            RiskAssessment? assessment = null;
            int totalScore = 0;
            var reasons = new List<string>();

            // Check for phone numbers
            if (ContainsPhoneNumber(content, out var phoneDetected))
            {
                modifiedText = "[Contact Information Removed]";
                isBlocked = true;
                totalScore += 25;
                reasons.Add($"Phone number detected: {phoneDetected}");
            }

            // Check for email addresses
            if (ContainsEmailAddress(content, out var emailDetected))
            {
                modifiedText = "[Contact Information Removed]";
                isBlocked = true;
                totalScore += 25;
                reasons.Add($"Email address detected: {emailDetected}");
            }

            // Check for social media
            if (!isBlocked && ContainsSocialMedia(content, out var socialDetected))
            {
                modifiedText = "[Contact Information Removed]";
                isBlocked = true;
                totalScore += 20;
                reasons.Add($"Social media detected: {socialDetected}");
            }

            // Check for external payment requests
            if (ContainsExternalPaymentRequest(content, out var paymentDetected))
            {
                modifiedText = "[Message blocked due to platform policy.]";
                isBlocked = true;
                totalScore += 40;
                reasons.Add($"External payment request detected: {paymentDetected}");
            }

            // If violations found, create risk assessment
            if (totalScore > 0)
            {
                var riskCategory = reasons.Any(r => r.Contains("payment", StringComparison.OrdinalIgnoreCase) || r.Contains("commission", StringComparison.OrdinalIgnoreCase))
                    ? RiskCategory.CommissionAvoidance
                    : RiskCategory.CommunicationRisk;

                assessment = await CreateRiskAssessmentAsync(
                    entityType, entityId, riskCategory, totalScore,
                    string.Join("; ", reasons), messageId, orderId,
                    isBlocked ? modifiedText : null, isBlocked);

                // Update user risk profile
                var user = await _userManager.FindByIdAsync(senderId);
                if (user != null)
                {
                    var roles = await _userManager.GetRolesAsync(user);
                    var role = roles.FirstOrDefault() ?? "Client";
                    await UpdateRiskProfileAsync(senderId, totalScore, role);
                }

                // Create audit log
                _context.AuditLogs.Add(new AuditLog
                {
                    Action = isBlocked ? "Message Blocked" : "Risk Detected",
                    PerformedById = senderId,
                    Description = $"Risk detected: {string.Join("; ", reasons)} | Score: {totalScore}",
                    CreatedDate = DateTime.UtcNow
                });
                await _context.SaveChangesAsync();

                _logger.LogWarning("Risk detected in message {MessageId}: {Reasons} (Score: {Score})",
                    messageId, string.Join(", ", reasons), totalScore);
            }

            return (isBlocked, modifiedText, assessment);
        }

        public async Task ScanExistingMessageAsync(int messageId)
        {
            var message = await _context.Messages.FindAsync(messageId);
            if (message == null) return;

            var conv = await _context.Conversations.FindAsync(message.ConversationId);
            await ScanMessageAsync(messageId, message.SenderId, message.MessageText,
                "Message", messageId.ToString(), conv?.OrderId);
        }

        // ----------------------------------------------------------------
        // RISK ASSESSMENT
        // ----------------------------------------------------------------

        public async Task<RiskAssessment> CreateRiskAssessmentAsync(string entityType, string entityId,
            RiskCategory category, int score, string reason, int? messageId = null, int? orderId = null,
            string? detectedContent = null, bool isBlocked = false)
        {
            var riskLevel = score switch
            {
                <= 24 => RiskLevel.Low,
                <= 49 => RiskLevel.Moderate,
                <= 74 => RiskLevel.High,
                _ => RiskLevel.Critical
            };

            var assessment = new RiskAssessment
            {
                EntityType = entityType,
                EntityId = entityId,
                RiskCategory = category,
                RiskScore = score,
                RiskLevel = riskLevel,
                Reason = reason,
                DetectedContent = detectedContent,
                IsBlocked = isBlocked,
                MessageId = messageId,
                OrderId = orderId,
                Status = "Open",
                DetectedAt = DateTime.UtcNow
            };

            _context.RiskAssessments.Add(assessment);
            await _context.SaveChangesAsync();
            return assessment;
        }

        // ----------------------------------------------------------------
        // RISK PROFILES
        // ----------------------------------------------------------------

        public async Task UpdateRiskProfileAsync(string userId, int additionalScore, string userRole)
        {
            if (userRole == "Writer")
            {
                var profile = await GetWriterRiskProfileAsync(userId);
                profile.CurrentRiskScore += additionalScore;
                profile.ViolationCount++;
                profile.RiskLevel = profile.CurrentRiskScore switch
                {
                    <= 24 => RiskLevel.Low,
                    <= 49 => RiskLevel.Moderate,
                    <= 74 => RiskLevel.High,
                    _ => RiskLevel.Critical
                };

                // Automatic actions
                if (profile.CurrentRiskScore >= 75)
                {
                    profile.IsFrozen = true;
                    _logger.LogWarning("Writer {UserId} risk score {Score} - ACCOUNT FROZEN", userId, profile.CurrentRiskScore);
                }
                else if (profile.CurrentRiskScore >= 50)
                {
                    profile.IsMessagingRestricted = true;
                    _logger.LogWarning("Writer {UserId} risk score {Score} - MESSAGING RESTRICTED", userId, profile.CurrentRiskScore);
                }

                profile.LastUpdated = DateTime.UtcNow;
            }
            else
            {
                var profile = await GetClientRiskProfileAsync(userId);
                profile.CurrentRiskScore += additionalScore;
                profile.ViolationCount++;
                profile.RiskLevel = profile.CurrentRiskScore switch
                {
                    <= 24 => RiskLevel.Low,
                    <= 49 => RiskLevel.Moderate,
                    <= 74 => RiskLevel.High,
                    _ => RiskLevel.Critical
                };

                if (profile.CurrentRiskScore >= 75)
                {
                    profile.IsFrozen = true;
                    _logger.LogWarning("Client {UserId} risk score {Score} - ACCOUNT FROZEN", userId, profile.CurrentRiskScore);
                }
                else if (profile.CurrentRiskScore >= 50)
                {
                    profile.IsMessagingRestricted = true;
                    _logger.LogWarning("Client {UserId} risk score {Score} - MESSAGING RESTRICTED", userId, profile.CurrentRiskScore);
                }

                profile.LastUpdated = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
        }

        public async Task<WriterRiskProfile> GetWriterRiskProfileAsync(string writerId)
        {
            var profile = await _context.WriterRiskProfiles
                .FirstOrDefaultAsync(p => p.WriterId == writerId);

            if (profile == null)
            {
                profile = new WriterRiskProfile { WriterId = writerId };
                _context.WriterRiskProfiles.Add(profile);
                await _context.SaveChangesAsync();
            }

            return profile;
        }

        public async Task<ClientRiskProfile> GetClientRiskProfileAsync(string clientId)
        {
            var profile = await _context.ClientRiskProfiles
                .FirstOrDefaultAsync(p => p.ClientId == clientId);

            if (profile == null)
            {
                profile = new ClientRiskProfile { ClientId = clientId };
                _context.ClientRiskProfiles.Add(profile);
                await _context.SaveChangesAsync();
            }

            return profile;
        }

        // ----------------------------------------------------------------
        // QUERIES
        // ----------------------------------------------------------------

        public async Task<(List<RiskAssessment> Risks, int TotalCount)> GetOpenRisksAsync(
            int page = 1, int pageSize = 25, RiskCategory? category = null, RiskLevel? level = null)
        {
            IQueryable<RiskAssessment> query = _context.RiskAssessments
                .Where(r => r.Status == "Open");

            if (category.HasValue)
                query = query.Where(r => r.RiskCategory == category.Value);

            if (level.HasValue)
                query = query.Where(r => r.RiskLevel == level.Value);

            var totalCount = await query.CountAsync();

            var risks = await query
                .OrderByDescending(r => r.RiskScore)
                .ThenByDescending(r => r.DetectedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (risks, totalCount);
        }

        public async Task<List<RiskAssessment>> GetEntityRisksAsync(string entityType, string entityId)
        {
            return await _context.RiskAssessments
                .Where(r => r.EntityType == entityType && r.EntityId == entityId)
                .OrderByDescending(r => r.DetectedAt)
                .ToListAsync();
        }

        public async Task<RiskDashboardStats> GetDashboardStatsAsync()
        {
            var openRisks = await _context.RiskAssessments
                .Where(r => r.Status == "Open").ToListAsync();

            var stats = new RiskDashboardStats
            {
                OpenRiskCount = openRisks.Count,
                HighRiskUserCount = await _context.WriterRiskProfiles
                    .CountAsync(p => p.RiskLevel == RiskLevel.High || p.RiskLevel == RiskLevel.Critical),
                BlockedMessageCount = openRisks.Count(r => r.IsBlocked),
                CommissionAvoidanceCount = openRisks.Count(r => r.RiskCategory == RiskCategory.CommissionAvoidance),
                PredictedDisputeCount = openRisks.Count(r => r.RiskCategory == RiskCategory.DisputeRisk),
                PredictedLateDeliveryCount = openRisks.Count(r => r.RiskCategory == RiskCategory.DeadlineRisk),
                TotalViolations = await _context.RiskAssessments.CountAsync(),
                RiskByCategory = openRisks.GroupBy(r => r.RiskCategory)
                    .ToDictionary(g => g.Key, g => g.Count())
            };

            return stats;
        }

        // ----------------------------------------------------------------
        // ADMIN ACTIONS
        // ----------------------------------------------------------------

        public async Task ResolveRiskAsync(int riskId, string resolvedById)
        {
            var risk = await _context.RiskAssessments.FindAsync(riskId);
            if (risk == null) return;

            risk.Status = "Resolved";
            risk.ResolvedAt = DateTime.UtcNow;
            risk.ResolvedById = resolvedById;

            _context.AuditLogs.Add(new AuditLog
            {
                Action = "Risk Resolved",
                PerformedById = resolvedById,
                Description = $"Risk #{riskId} ({risk.RiskCategory}) resolved. Score: {risk.RiskScore}",
                CreatedDate = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
        }

        public async Task WarnUserAsync(string userId, string reason, string adminId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return;

            var roles = await _userManager.GetRolesAsync(user);
            var role = roles.FirstOrDefault() ?? "Client";

            if (role == "Writer")
            {
                var profile = await GetWriterRiskProfileAsync(userId);
                profile.WarningCount++;
                profile.LastUpdated = DateTime.UtcNow;
            }
            else
            {
                var profile = await GetClientRiskProfileAsync(userId);
                profile.WarningCount++;
                profile.LastUpdated = DateTime.UtcNow;
            }

            _context.AuditLogs.Add(new AuditLog
            {
                Action = "Warning Issued",
                PerformedById = adminId,
                TargetUserId = userId,
                Description = $"Warning issued to {role} {userId}: {reason}",
                CreatedDate = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
        }

        public async Task RestrictMessagingAsync(string userId, string userRole, string adminId)
        {
            if (userRole == "Writer")
            {
                var profile = await GetWriterRiskProfileAsync(userId);
                profile.IsMessagingRestricted = true;
                profile.LastUpdated = DateTime.UtcNow;
            }
            else
            {
                var profile = await GetClientRiskProfileAsync(userId);
                profile.IsMessagingRestricted = true;
                profile.LastUpdated = DateTime.UtcNow;
            }

            _context.AuditLogs.Add(new AuditLog
            {
                Action = "Messaging Restricted",
                PerformedById = adminId,
                TargetUserId = userId,
                Description = $"Messaging restricted for {userRole} {userId}",
                CreatedDate = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
        }

        public async Task FreezeAccountAsync(string userId, string userRole, string adminId)
        {
            if (userRole == "Writer")
            {
                var profile = await GetWriterRiskProfileAsync(userId);
                profile.IsFrozen = true;
                profile.IsMessagingRestricted = true;
                profile.LastUpdated = DateTime.UtcNow;
            }
            else
            {
                var profile = await GetClientRiskProfileAsync(userId);
                profile.IsFrozen = true;
                profile.IsMessagingRestricted = true;
                profile.LastUpdated = DateTime.UtcNow;
            }

            _context.AuditLogs.Add(new AuditLog
            {
                Action = "Account Frozen",
                PerformedById = adminId,
                TargetUserId = userId,
                Description = $"Account frozen for {userRole} {userId} due to high risk",
                CreatedDate = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
        }
    }
}