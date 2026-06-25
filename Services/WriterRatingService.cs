using Microsoft.EntityFrameworkCore;
using ScholarRescue.Data;
using ScholarRescue.Models;
using ScholarRescue.Models.Enums;

namespace ScholarRescue.Services
{
    public class WriterRatingService : IWriterRatingService
    {
        private readonly ScholarRescueDbContext _context;
        private readonly INotificationService _notificationService;
        private readonly ILogger<WriterRatingService> _logger;

        public WriterRatingService(ScholarRescueDbContext context, INotificationService notificationService, ILogger<WriterRatingService> logger)
        {
            _context = context;
            _notificationService = notificationService;
            _logger = logger;
        }

        public async Task<WriterRating> SubmitRatingAsync(int orderId, string clientId, int overallRating,
            int qualityRating, int communicationRating, int deadlineRating,
            string? reviewText, bool wouldHireAgain)
        {
            var order = await _context.Orders.Include(o => o.AssignedWriter).FirstOrDefaultAsync(o => o.Id == orderId);
            if (order == null) throw new InvalidOperationException("Order not found.");
            if (order.Status != OrderStatus.Completed) throw new InvalidOperationException("Only completed orders can be rated.");
            if (order.ClientId != clientId) throw new InvalidOperationException("You are not the owner of this order.");
            if (order.AssignedWriterId == null) throw new InvalidOperationException("No writer assigned.");

            if (await _context.Set<WriterRating>().AnyAsync(r => r.OrderId == orderId))
                throw new InvalidOperationException("This order has already been rated.");

            var rating = new WriterRating
            {
                OrderId = orderId, WriterId = order.AssignedWriterId, ClientId = clientId,
                OverallRating = overallRating, QualityRating = qualityRating,
                CommunicationRating = communicationRating, DeadlineRating = deadlineRating,
                ReviewText = reviewText, WouldHireAgain = wouldHireAgain, CreatedAt = DateTime.UtcNow
            };

            _context.Set<WriterRating>().Add(rating);

            // Update writer's average rating on ApplicationUser
            var writer = await _context.Users.FindAsync(order.AssignedWriterId);
            if (writer != null)
            {
                var stats = await GetWriterStatsAsync(order.AssignedWriterId);
                writer.AverageRating = stats.AvgRating;
                writer.LastActivityDate = DateTime.UtcNow;
            }

            // Audit log
            _context.AuditLogs.Add(new AuditLog
            {
                Action = "Rating Submitted",
                PerformedById = clientId, TargetUserId = order.AssignedWriterId,
                Description = $"Rating {overallRating}/5 for order {order.OrderNumber}",
                CreatedDate = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            // Notify writer
            await _notificationService.CreateNotificationAsync(order.AssignedWriterId,
                "New Rating Received",
                $"You received a {overallRating}/5 rating for order {order.OrderNumber}.",
                NotificationType.OrderCompleted, order.Id.ToString(), "Order");

            // Negative rating alert
            if (overallRating <= 2)
            {
                var admins = await _context.Users.Where(u => u.UserType == "Administrator" && u.IsActive).ToListAsync();
                foreach (var admin in admins)
                {
                    await _notificationService.CreateNotificationAsync(admin.Id,
                        "Negative Rating Alert",
                        $"Writer {writer?.FirstName} {writer?.LastName} received a {overallRating}/5 rating on order {order.OrderNumber}.",
                        NotificationType.SystemAlert, order.AssignedWriterId, "User");
                }
            }

            return rating;
        }

        public async Task<bool> HasRatingAsync(int orderId) =>
            await _context.Set<WriterRating>().AnyAsync(r => r.OrderId == orderId);

        public async Task<(double AvgRating, int TotalReviews, double RepeatHireRate)> GetWriterStatsAsync(string writerId)
        {
            var ratings = await _context.Set<WriterRating>().Where(r => r.WriterId == writerId).ToListAsync();
            if (!ratings.Any()) return (0, 0, 0);

            var avg = ratings.Average(r => r.OverallRating);
            var repeatHire = ratings.Count(r => r.WouldHireAgain) / (double)ratings.Count * 100;
            return (Math.Round(avg, 2), ratings.Count, Math.Round(repeatHire, 1));
        }

        public async Task<List<WriterRating>> GetWriterRatingsAsync(string writerId, int page = 1, int pageSize = 20) =>
            await _context.Set<WriterRating>().Include(r => r.Client).Include(r => r.Order)
                .Where(r => r.WriterId == writerId)
                .OrderByDescending(r => r.CreatedAt)
                .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        public async Task<bool> IsFeaturedWriterAsync(string writerId)
        {
            var writer = await _context.Users.FindAsync(writerId);
            if (writer == null || writer.TotalCompletedOrders < 50) return false;
            var stats = await GetWriterStatsAsync(writerId);
            return stats.AvgRating >= 4.8 && stats.TotalReviews >= 10;
        }

        public async Task<List<(string WriterId, string Name, double AvgRating, int Reviews)>> GetTopRatedWritersAsync(int count = 10)
        {
            var writers = await _context.Users.Where(u => u.UserType == "Writer" && u.IsActive && !u.IsDeleted).ToListAsync();
            var result = new List<(string, string, double, int)>();
            foreach (var w in writers)
            {
                var stats = await GetWriterStatsAsync(w.Id);
                if (stats.TotalReviews > 0)
                    result.Add((w.Id, $"{w.FirstName} {w.LastName}", stats.AvgRating, stats.TotalReviews));
            }
            return result.OrderByDescending(r => r.Item3).Take(count).ToList();
        }

        public async Task<(List<WriterRating> Ratings, int Total)> GetAllRatingsAsync(int page = 1, int pageSize = 50,
            string? writerSearch = null, int? minRating = null, DateTime? dateFrom = null, DateTime? dateTo = null)
        {
            IQueryable<WriterRating> query = _context.Set<WriterRating>().Include(r => r.Writer).Include(r => r.Client).Include(r => r.Order);

            if (!string.IsNullOrWhiteSpace(writerSearch))
            {
                var term = writerSearch.ToLowerInvariant();
                query = query.Where(r => (r.Writer.FirstName + " " + r.Writer.LastName).ToLower().Contains(term));
            }
            if (minRating.HasValue) query = query.Where(r => r.OverallRating >= minRating.Value);
            if (dateFrom.HasValue) query = query.Where(r => r.CreatedAt >= dateFrom.Value);
            if (dateTo.HasValue) query = query.Where(r => r.CreatedAt <= dateTo.Value);

            var total = await query.CountAsync();
            var ratings = await query.OrderByDescending(r => r.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            return (ratings, total);
        }
    }
}