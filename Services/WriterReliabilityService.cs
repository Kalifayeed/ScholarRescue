using Microsoft.EntityFrameworkCore;
using ScholarRescue.Data;
using ScholarRescue.Models;
using ScholarRescue.Models.Enums;

namespace ScholarRescue.Services
{
    /// <summary>
    /// Writer Reliability & Penalty Engine.
    /// Initial score: 100. Deductions/bonuses adjust score. Tiers determine writer privileges.
    /// </summary>
    public class WriterReliabilityService : IWriterReliabilityService
    {
        private readonly ScholarRescueDbContext _context;
        private readonly INotificationService _notificationService;
        private readonly ILogger<WriterReliabilityService> _logger;

        public WriterReliabilityService(
            ScholarRescueDbContext context,
            INotificationService notificationService,
            ILogger<WriterReliabilityService> logger)
        {
            _context = context;
            _notificationService = notificationService;
            _logger = logger;
        }

        public async Task<WriterReliability> GetOrCreateAsync(string writerId)
        {
            var record = await _context.Set<WriterReliability>()
                .FirstOrDefaultAsync(r => r.WriterId == writerId);

            if (record == null)
            {
                record = new WriterReliability { WriterId = writerId };
                _context.Set<WriterReliability>().Add(record);
                await _context.SaveChangesAsync();
            }

            return record;
        }

        public async Task ApplyPenaltyAsync(string writerId, string action, string reason,
            int points, bool isDeduction, string? createdBy = null)
        {
            var record = await GetOrCreateAsync(writerId);
            var previousScore = record.ReliabilityScore;

            // Apply score change
            if (isDeduction)
            {
                record.ReliabilityScore = Math.Max(0, record.ReliabilityScore - points);
                record.Warnings++;
            }
            else
            {
                record.ReliabilityScore = Math.Min(100, record.ReliabilityScore + points);
            }

            record.UpdatedAt = DateTime.UtcNow;

            // Log the penalty
            var log = new WriterPenaltyLog
            {
                WriterId = writerId,
                Action = action,
                PointsAdded = isDeduction ? 0 : points,
                PointsRemoved = isDeduction ? points : 0,
                Reason = reason,
                CreatedBy = createdBy,
                CreatedAt = DateTime.UtcNow
            };
            _context.Set<WriterPenaltyLog>().Add(log);

            // Trigger auto-suspension if score < 50
            if (record.ReliabilityScore < 50)
            {
                var writer = await _context.Users.FindAsync(writerId);
                if (writer != null)
                {
                    writer.AvailabilityStatus = WriterAvailabilityStatus.Suspended;
                    writer.IsAcceptingOrders = false;
                }
                record.Suspensions++;

                await _notificationService.CreateNotificationAsync(writerId,
                    "Account Suspended",
                    "Your reliability score has dropped below 50. Your account has been automatically suspended pending admin review.",
                    NotificationType.SystemAlert, writerId, "User");
            }

            await _context.SaveChangesAsync();

            // Notify writer
            await _notificationService.CreateNotificationAsync(writerId,
                isDeduction ? "Penalty Applied" : "Bonus Applied",
                $"{action}: {(isDeduction ? $"{points} points deducted" : $"{points} points added")}. Score: {previousScore} → {record.ReliabilityScore}",
                NotificationType.SystemAlert, writerId, "User");

            // Audit log
            _context.AuditLogs.Add(new AuditLog
            {
                Action = isDeduction ? "Penalty Applied" : "Bonus Applied",
                PerformedById = createdBy ?? writerId,
                TargetUserId = writerId,
                Description = $"{action}: {points} pts ({(isDeduction ? "deducted" : "added")}). Score: {previousScore} → {record.ReliabilityScore}",
                CreatedDate = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();

            _logger.LogInformation("Reliability {Type} for writer {Writer}: {Action}, {Points}pts, Score {Prev}→{New}",
                isDeduction ? "penalty" : "bonus", writerId, action, points, previousScore, record.ReliabilityScore);
        }

        public string GetTier(int score) => score switch
        {
            >= 95 => "Elite Writer",
            >= 85 => "Senior Writer",
            >= 70 => "Standard Writer",
            >= 50 => "Restricted Writer",
            _ => "Suspended - Review Required"
        };

        public string GetRiskLevel(int score) => score switch
        {
            >= 85 => "Low Risk",
            >= 70 => "Medium Risk",
            >= 50 => "High Risk",
            _ => "Critical"
        };

        public async Task<bool> CanAcceptPremiumOrdersAsync(string writerId)
        {
            var record = await GetOrCreateAsync(writerId);
            return record.ReliabilityScore >= 70;
        }

        public async Task<int> GetMaxAllowedOrdersAsync(string writerId)
        {
            var record = await GetOrCreateAsync(writerId);
            if (record.ReliabilityScore >= 70) return 5;
            if (record.ReliabilityScore >= 60) return 3;
            return 0; // Suspended
        }

        public async Task<List<WriterPenaltyLog>> GetPenaltyLogAsync(string writerId, int page = 1, int pageSize = 50) =>
            await _context.Set<WriterPenaltyLog>()
                .Where(l => l.WriterId == writerId)
                .OrderByDescending(l => l.CreatedAt)
                .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        public async Task<List<WriterReliability>> GetAllAsync(string? search = null, int? minScore = null, int? maxScore = null)
        {
            IQueryable<WriterReliability> query = _context.Set<WriterReliability>()
                .Include(r => r.Writer).AsNoTracking();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.ToLowerInvariant();
                query = query.Where(r =>
                    (r.Writer.FirstName + " " + r.Writer.LastName).ToLower().Contains(term));
            }
            if (minScore.HasValue) query = query.Where(r => r.ReliabilityScore >= minScore.Value);
            if (maxScore.HasValue) query = query.Where(r => r.ReliabilityScore <= maxScore.Value);

            return await query.OrderByDescending(r => r.ReliabilityScore).ToListAsync();
        }
    }
}