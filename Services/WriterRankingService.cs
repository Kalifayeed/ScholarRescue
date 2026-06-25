using Microsoft.EntityFrameworkCore;
using ScholarRescue.Data;
using ScholarRescue.Models;
using ScholarRescue.Models.Enums;
using ScholarRescue.ViewModels.WriterResources;

namespace ScholarRescue.Services
{
    /// <summary>
    /// Manages writer rankings: metric aggregation, auto-promotion, and admin overrides.
    /// </summary>
    public class WriterRankingService : IWriterRankingService
    {
        private readonly ScholarRescueDbContext _context;
        private readonly ILogger<WriterRankingService> _logger;
        private readonly INotificationService _notificationService;

        // Promotion thresholds
        private static readonly List<RankCriteria> Criteria = new()
        {
            new() { Rank = WriterRank.Intermediate, MinCompletedOrders = 21,  MinAverageRating = 4.0 },
            new() { Rank = WriterRank.Advanced,     MinCompletedOrders = 101, MinAverageRating = 4.5 },
            new() { Rank = WriterRank.Expert,       MinCompletedOrders = 301, MinAverageRating = 4.7 },
            new() { Rank = WriterRank.Elite,        MinCompletedOrders = 1000, MinAverageRating = 4.8 }
        };

        public WriterRankingService(
            ScholarRescueDbContext context,
            ILogger<WriterRankingService> logger,
            INotificationService notificationService)
        {
            _context = context;
            _logger = logger;
            _notificationService = notificationService;
        }

        public async Task<WriterRanking> GetOrCreateAsync(string writerId)
        {
            var ranking = await _context.WriterRankings
                .FirstOrDefaultAsync(r => r.WriterId == writerId);

            if (ranking == null)
            {
                ranking = new WriterRanking
                {
                    WriterId = writerId,
                    CurrentRank = WriterRank.Beginner,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.WriterRankings.Add(ranking);
                await _context.SaveChangesAsync();
            }

            return ranking;
        }

        public async Task<List<WriterRanking>> GetAllAsync()
        {
            return await _context.WriterRankings
                .Include(r => r.Writer)
                .OrderByDescending(r => r.CurrentRank)
                .ThenByDescending(r => r.CompletedOrders)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<WriterRank> GetCurrentRankAsync(string writerId)
        {
            var ranking = await _context.WriterRankings
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.WriterId == writerId);
            return ranking?.CurrentRank ?? WriterRank.Beginner;
        }

        public async Task UpdateMetricsOnCompletionAsync(string writerId)
        {
            if (string.IsNullOrEmpty(writerId)) return;

            var ranking = await GetOrCreateAsync(writerId);

            // Recompute metrics from the source-of-truth (orders).
            var completedOrders = await _context.Orders
                .Where(o => o.AssignedWriterId == writerId && o.Status == OrderStatus.Completed)
                .AsNoTracking()
                .ToListAsync();

            ranking.CompletedOrders = completedOrders.Count;
            ranking.OnTimeDeliveries = completedOrders.Count(o =>
                o.CompletedAt.HasValue && o.CompletedAt.Value <= o.Deadline);

            // Orders that had at least one revision request
            var orderIds = completedOrders.Select(o => o.Id).ToList();
            ranking.OrdersWithRevisions = orderIds.Count == 0 ? 0 :
                _context.RevisionRequests.Count(rr => orderIds.Contains(rr.OrderId));

            ranking.DisputedOrders = completedOrders.Count(o => o.IsDisputed);
            ranking.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            await EvaluateAndApplyRankAsync(writerId);
        }

        public async Task AddRatingAsync(string writerId, int rating)
        {
            if (string.IsNullOrEmpty(writerId) || rating < 1 || rating > 5) return;

            var ranking = await GetOrCreateAsync(writerId);
            ranking.TotalRating += rating;
            ranking.TotalRatings += 1;
            ranking.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            await EvaluateAndApplyRankAsync(writerId);
        }

        public async Task IncrementRevisionsAsync(string writerId)
        {
            if (string.IsNullOrEmpty(writerId)) return;
            var ranking = await GetOrCreateAsync(writerId);
            ranking.OrdersWithRevisions += 1;
            ranking.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        public async Task IncrementDisputesAsync(string writerId)
        {
            if (string.IsNullOrEmpty(writerId)) return;
            var ranking = await GetOrCreateAsync(writerId);
            ranking.DisputedOrders += 1;
            ranking.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        public async Task<WriterRanking> OverrideRankAsync(string writerId, WriterRank newRank, string adminId, string? notes)
        {
            var ranking = await GetOrCreateAsync(writerId);
            var previousRank = ranking.CurrentRank;
            ranking.CurrentRank = newRank;
            ranking.IsOverridden = true;
            ranking.OverrideAdminId = adminId;
            ranking.OverriddenAt = DateTime.UtcNow;
            ranking.OverrideNotes = notes;
            ranking.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Rank override by admin {AdminId}: writer {WriterId} {Prev} -> {New}",
                adminId, writerId, previousRank, newRank);

            if (previousRank != newRank)
            {
                await _notificationService.CreateNotificationAsync(
                    writerId,
                    "Rank Updated",
                    $"An administrator has changed your writer rank from {previousRank} to {newRank}." +
                    (string.IsNullOrEmpty(notes) ? string.Empty : $" Reason: {notes}"),
                    NotificationType.SystemAlert,
                    newRank.ToString());
            }

            return ranking;
        }

        public async Task ClearOverrideAsync(string writerId)
        {
            var ranking = await GetOrCreateAsync(writerId);
            ranking.IsOverridden = false;
            ranking.OverrideNotes = null;
            ranking.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            await EvaluateAndApplyRankAsync(writerId);
        }

        public async Task<WriterRank> EvaluateAndApplyRankAsync(string writerId)
        {
            var ranking = await GetOrCreateAsync(writerId);

            // If admin has overridden, do not auto-promote.
            if (ranking.IsOverridden)
                return ranking.CurrentRank;

                var newRank = GetComputedRank(ranking.CompletedOrders);

            if (newRank != ranking.CurrentRank)
            {
                var previousRank = ranking.CurrentRank;
                ranking.CurrentRank = newRank;
                ranking.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Writer {WriterId} auto-promoted: {Prev} -> {New}",
                    writerId, previousRank, newRank);

                await _notificationService.CreateNotificationAsync(
                    writerId,
                    "Rank Promoted",
                    $"Congratulations! You have been promoted from {previousRank} to {newRank} writer.",
                    NotificationType.WriterApplicationRejected,
                    newRank.ToString());
            }

            return newRank;
        }

        // Delegate rank computation to WriterCapacityService
        private WriterRank GetComputedRank(int completedOrders)
        {
            if (completedOrders >= 1000) return WriterRank.Elite;
            if (completedOrders >= 301) return WriterRank.Expert;
            if (completedOrders >= 101) return WriterRank.Advanced;
            if (completedOrders >= 21) return WriterRank.Intermediate;
            return WriterRank.Beginner;
        }

        public IReadOnlyList<RankCriteria> GetPromotionCriteria() => Criteria;

        public async Task<WriterAnalyticsViewModel> GetAnalyticsAsync(string writerId)
        {
            var ranking = await GetOrCreateAsync(writerId);
            var writer = await _context.Users.FindAsync(writerId);

            // Current orders (in progress or assigned)
            var currentOrders = await _context.Orders
                .CountAsync(o => o.AssignedWriterId == writerId
                    && (o.Status == OrderStatus.InProgress || o.Status == OrderStatus.Assigned));

            // Financial data from wallet
            var totalEarnings = ranking.CompletedOrders > 0
                ? (await _context.OrderFinancialRecords
                    .Where(r => r.Order != null && r.Order.AssignedWriterId == writerId)
                    .SumAsync(r => (decimal?)r.WriterAmount) ?? 0)
                : 0m;
            var commissionPaid = ranking.CompletedOrders > 0
                ? (await _context.OrderFinancialRecords
                    .Where(r => r.Order != null && r.Order.AssignedWriterId == writerId)
                    .SumAsync(r => (decimal?)r.CommissionAmount) ?? 0)
                : 0m;

            // Rank progress
            var criteriaList = GetPromotionCriteria();
            string currentRankStr = ranking.CurrentRank.ToString();
            string? nextRankStr = null;
            int ordersToNext = 0;
            double ratingToNext = 0;

            var nextCriteria = criteriaList.FirstOrDefault(c => c.Rank == (WriterRank)((int)ranking.CurrentRank + 1));
            if (nextCriteria != null)
            {
                nextRankStr = nextCriteria.Rank.ToString();
                ordersToNext = Math.Max(0, nextCriteria.MinCompletedOrders - ranking.CompletedOrders);
                ratingToNext = Math.Max(0, nextCriteria.MinAverageRating - ranking.AverageRating);
            }

            // Monthly stats (last 12 months)
            var monthlyStats = new List<MonthlyStatEntry>();
            var twelveMonthsAgo = DateTime.UtcNow.AddMonths(-12);
            var completedOrders = await _context.Orders
                .Where(o => o.AssignedWriterId == writerId && o.Status == OrderStatus.Completed
                    && o.CompletedAt >= twelveMonthsAgo)
                .AsNoTracking()
                .ToListAsync();

            var records = await _context.OrderFinancialRecords
                .Where(r => r.Order != null && r.Order.AssignedWriterId == writerId
                    && r.CreatedDate >= twelveMonthsAgo)
                .ToListAsync();

            for (int i = 11; i >= 0; i--)
            {
                var monthStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(-i);
                var monthEnd = monthStart.AddMonths(1);

                var monthOrders = completedOrders.Count(o => o.CompletedAt >= monthStart && o.CompletedAt < monthEnd);
                var monthEarnings = records.Where(r => r.CreatedDate >= monthStart && r.CreatedDate < monthEnd).Sum(r => r.WriterAmount);
                var monthCommission = records.Where(r => r.CreatedDate >= monthStart && r.CreatedDate < monthEnd).Sum(r => r.CommissionAmount);

                monthlyStats.Add(new MonthlyStatEntry
                {
                    Month = monthStart.ToString("MMM"),
                    Year = monthStart.Year,
                    MonthNumber = monthStart.Month,
                    OrdersCompleted = monthOrders,
                    Earnings = monthEarnings,
                    Commission = monthCommission
                });
            }

            return new WriterAnalyticsViewModel
            {
                WriterId = writerId,
                WriterName = writer != null ? $"{writer.FirstName} {writer.LastName}" : writerId,
                CompletedOrders = ranking.CompletedOrders,
                CurrentOrders = currentOrders,
                AverageRating = ranking.AverageRating,
                TotalRatings = ranking.TotalRatings,
                RevisionPercentage = ranking.RevisionRate * 100,
                DisputePercentage = ranking.DisputeRate * 100,
                OnTimeDeliveryPercentage = ranking.OnTimeDeliveryRate * 100,
                TotalEarnings = totalEarnings,
                CommissionPaid = commissionPaid,
                CurrentRank = currentRankStr,
                NextRankName = nextRankStr,
                CompletedOrdersToNextRank = ordersToNext,
                RatingToNextRank = ratingToNext,
                MonthlyStats = monthlyStats
            };
        }
    }
}
