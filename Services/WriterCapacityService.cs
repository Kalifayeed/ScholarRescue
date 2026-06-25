using Microsoft.EntityFrameworkCore;
using ScholarRescue.Data;
using ScholarRescue.Models;
using ScholarRescue.Models.Enums;

namespace ScholarRescue.Services
{
    public class WriterCapacityService : IWriterCapacityService
    {
        private readonly ScholarRescueDbContext _context;
        private readonly INotificationService _notificationService;
        private readonly ILogger<WriterCapacityService> _logger;

        public WriterCapacityService(
            ScholarRescueDbContext context,
            INotificationService notificationService,
            ILogger<WriterCapacityService> logger)
        {
            _context = context;
            _notificationService = notificationService;
            _logger = logger;
        }

        public async Task UpdateAvailabilityStatusAsync(string writerId)
        {
            var writer = await _context.Users.FindAsync(writerId);
            if (writer == null || writer.MaxActiveOrders <= 0) return;

            var pct = (int)((double)writer.CurrentActiveOrders / writer.MaxActiveOrders * 100);
            var previousStatus = writer.AvailabilityStatus;

            if (pct <= 60) writer.AvailabilityStatus = WriterAvailabilityStatus.Available;
            else if (pct <= 80) writer.AvailabilityStatus = WriterAvailabilityStatus.Busy;
            else writer.AvailabilityStatus = WriterAvailabilityStatus.Full;

            writer.IsAcceptingOrders = writer.AvailabilityStatus != WriterAvailabilityStatus.Full
                && writer.AvailabilityStatus != WriterAvailabilityStatus.Suspended;

            writer.LastActivityDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            if (previousStatus != writer.AvailabilityStatus)
            {
                // Audit log
                _context.AuditLogs.Add(new AuditLog
                {
                    Action = "Writer Status Changed",
                    PerformedById = writerId,
                    TargetUserId = writerId,
                    Description = $"Availability changed from {previousStatus} to {writer.AvailabilityStatus} ({pct}% capacity)",
                    CreatedDate = DateTime.UtcNow
                });
                await _context.SaveChangesAsync();
            }
        }

        public async Task<int> GetCapacityPercentageAsync(string writerId)
        {
            var writer = await _context.Users.FindAsync(writerId);
            if (writer == null || writer.MaxActiveOrders <= 0) return 0;
            return (int)((double)writer.CurrentActiveOrders / writer.MaxActiveOrders * 100);
        }

        public async Task<bool> CanAcceptOrderAsync(string writerId)
        {
            var writer = await _context.Users.FindAsync(writerId);
            return writer != null && writer.IsAcceptingOrders
                && writer.CurrentActiveOrders < writer.MaxActiveOrders
                && writer.AvailabilityStatus != WriterAvailabilityStatus.Suspended;
        }

        public async Task IncrementActiveOrdersAsync(string writerId)
        {
            var writer = await _context.Users.FindAsync(writerId);
            if (writer == null) return;
            writer.CurrentActiveOrders++;
            writer.LastActivityDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            await UpdateAvailabilityStatusAsync(writerId);

            if (!writer.IsAcceptingOrders)
            {
                await _notificationService.CreateNotificationAsync(writerId,
                    "Capacity Reached",
                    "You have reached your maximum active order capacity.",
                    NotificationType.SystemAlert, writerId, "User");
            }
        }

        public async Task DecrementActiveOrdersAsync(string writerId)
        {
            var writer = await _context.Users.FindAsync(writerId);
            if (writer == null || writer.CurrentActiveOrders <= 0) return;
            writer.CurrentActiveOrders--;
            writer.LastActivityDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            await UpdateAvailabilityStatusAsync(writerId);

            if (writer.CurrentActiveOrders < writer.MaxActiveOrders && !writer.IsAcceptingOrders)
            {
                await _notificationService.CreateNotificationAsync(writerId,
                    "Capacity Available",
                    "You may now accept additional orders.",
                    NotificationType.SystemAlert, writerId, "User");
            }
        }

        public async Task UpdateQualityScoreAsync(string writerId, string eventType)
        {
            var writer = await _context.Users.FindAsync(writerId);
            if (writer == null) return;

            int delta = eventType switch
            {
                "OrderCompleted" => 1,
                "FiveStarRating" => 2,
                "RevisionRequested" => -2,
                "LateDelivery" => -5,
                "DisputeLost" => -10,
                _ => 0
            };

            writer.QualityScore = Math.Clamp(writer.QualityScore + delta, 0, 100);

            if (eventType == "OrderCompleted") writer.TotalCompletedOrders++;
            if (eventType == "RevisionRequested") writer.TotalRevisionRequests++;
            if (eventType == "LateDelivery") writer.TotalLateOrders++;

            await _context.SaveChangesAsync();

            _context.AuditLogs.Add(new AuditLog
            {
                Action = "Quality Score Updated",
                PerformedById = writerId,
                TargetUserId = writerId,
                Description = $"Quality score changed by {delta} to {writer.QualityScore} (event: {eventType})",
                CreatedDate = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
        }

        public async Task<List<WriterRecommendation>> GetRecommendedWritersAsync(int orderId, int maxResults = 5)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null) return new List<WriterRecommendation>();

            var writers = await _context.Users
                .Where(u => u.UserType == "Writer" && u.IsActive && !u.IsDeleted)
                .ToListAsync();

            var recommendations = new List<WriterRecommendation>();

            foreach (var w in writers)
            {
                var capPct = w.MaxActiveOrders > 0
                    ? (int)((double)w.CurrentActiveOrders / w.MaxActiveOrders * 100) : 0;
                var subjectMatch = SubjectMatches(w.SubjectSpecializations ?? "", order.Subject);

                double score = 0;
                score += subjectMatch ? 50 : 0;
                score += Math.Max(0, 100 - capPct) * 0.3;
                score += w.QualityScore * 0.1;
                score += w.TotalCompletedOrders * 0.05;
                score += w.AverageRating * 5;

                recommendations.Add(new WriterRecommendation
                {
                    WriterId = w.Id,
                    WriterName = $"{w.FirstName} {w.LastName}",
                    Email = w.Email ?? "",
                    Rank = GetComputedRank(w.TotalCompletedOrders),
                    RankName = GetRankDisplayName(GetComputedRank(w.TotalCompletedOrders)),
                    ActiveOrders = w.CurrentActiveOrders,
                    MaxOrders = w.MaxActiveOrders,
                    CapacityPercent = capPct,
                    Availability = w.AvailabilityStatus,
                    QualityScore = w.QualityScore,
                    CompletedOrders = w.TotalCompletedOrders,
                    AverageRating = w.AverageRating,
                    SubjectMatch = subjectMatch,
                    RecommendationScore = Math.Round(score, 1),
                    IsRecommended = subjectMatch && w.CurrentActiveOrders < w.MaxActiveOrders
                });
            }

            return recommendations.OrderByDescending(r => r.RecommendationScore).Take(maxResults).ToList();
        }

        public bool SubjectMatches(string writerSubjects, string orderSubject)
        {
            if (string.IsNullOrWhiteSpace(writerSubjects) || string.IsNullOrWhiteSpace(orderSubject))
                return false;

            var subjects = writerSubjects.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            return subjects.Any(s => s.Equals(orderSubject, StringComparison.OrdinalIgnoreCase));
        }

        public WriterRank GetComputedRank(int completedOrders)
        {
            if (completedOrders >= 1000) return WriterRank.Elite;
            if (completedOrders >= 301) return WriterRank.Expert;
            if (completedOrders >= 101) return WriterRank.Advanced;
            if (completedOrders >= 21) return WriterRank.Intermediate;
            return WriterRank.Beginner;
        }

        public string GetRankDisplayName(WriterRank rank) => rank switch
        {
            WriterRank.Beginner => "Beginner",
            WriterRank.Intermediate => "Intermediate",
            WriterRank.Advanced => "Advanced",
            WriterRank.Expert => "Expert",
            WriterRank.Elite => "Elite",
            _ => "Beginner"
        };
    }
}