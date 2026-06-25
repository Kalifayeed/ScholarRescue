using Microsoft.EntityFrameworkCore;
using ScholarRescue.Data;
using ScholarRescue.Models;
using ScholarRescue.Models.Enums;

namespace ScholarRescue.Services
{
    public class MarketplaceService : IMarketplaceService
    {
        private readonly ScholarRescueDbContext _context;
        private readonly IWriterReliabilityService _reliabilityService;
        private readonly ILogger<MarketplaceService> _logger;

        public MarketplaceService(ScholarRescueDbContext context, IWriterReliabilityService reliabilityService, ILogger<MarketplaceService> logger)
        {
            _context = context;
            _reliabilityService = reliabilityService;
            _logger = logger;
        }

        public async Task<WriterSpecialization> SetSpecializationAsync(string writerId, string subject, string expertiseLevel, int yearsExperience)
        {
            var existing = await _context.Set<WriterSpecialization>()
                .FirstOrDefaultAsync(s => s.WriterId == writerId && s.Subject == subject);

            if (existing != null)
            {
                existing.ExpertiseLevel = expertiseLevel;
                existing.YearsExperience = yearsExperience;
            }
            else
            {
                var count = await _context.Set<WriterSpecialization>().CountAsync(s => s.WriterId == writerId);
                if (count >= 5) throw new InvalidOperationException("Maximum 5 specializations allowed.");

                existing = new WriterSpecialization { WriterId = writerId, Subject = subject, ExpertiseLevel = expertiseLevel, YearsExperience = yearsExperience };
                _context.Set<WriterSpecialization>().Add(existing);
            }

            // Update ApplicationUser SubjectSpecializations field
            var user = await _context.Users.FindAsync(writerId);
            if (user != null)
            {
                var subjects = await _context.Set<WriterSpecialization>().Where(s => s.WriterId == writerId).Select(s => s.Subject).ToListAsync();
                user.SubjectSpecializations = string.Join(",", subjects);
            }

            await _context.SaveChangesAsync();
            return existing;
        }

        public async Task<List<WriterSpecialization>> GetSpecializationsAsync(string writerId) =>
            await _context.Set<WriterSpecialization>().Where(s => s.WriterId == writerId).ToListAsync();

        public async Task<bool> IsEligibleForOrderAsync(string writerId, int orderId)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null) return false;

            var specializations = await _context.Set<WriterSpecialization>().Where(s => s.WriterId == writerId).ToListAsync();
            if (!specializations.Any()) return false;

            var subjects = specializations.Select(s => s.Subject).ToList();
            var subjectMatch = subjects.Any(s => s.Equals(order.Subject, StringComparison.OrdinalIgnoreCase));
            return subjectMatch;
        }

        public async Task<List<OrderMatch>> GetEligibleOrdersAsync(string writerId, string? subjectFilter = null, AcademicLevel? levelFilter = null)
        {
            var specializations = await _context.Set<WriterSpecialization>().Where(s => s.WriterId == writerId).ToListAsync();
            var writerSubjects = specializations.Select(s => s.Subject.ToLowerInvariant()).ToList();

            var query = _context.Orders.Where(o => o.Status == OrderStatus.Open && o.IsMarketplaceOpen && o.AssignedWriterId == null);

            if (!string.IsNullOrWhiteSpace(subjectFilter))
                query = query.Where(o => o.Subject.ToLower().Contains(subjectFilter.ToLower()));

            if (levelFilter.HasValue)
                query = query.Where(o => o.AcademicLevel == levelFilter.Value);

            var orders = await query.OrderByDescending(o => o.CreatedAt).ToListAsync();
            var results = new List<OrderMatch>();

            foreach (var o in orders)
            {
                var subjectMatch = writerSubjects.Contains(o.Subject.ToLowerInvariant());
                var score = CalculateMatchScore(string.Join(",", writerSubjects), AcademicLevel.Masters,
                    specializations.Count, o.AcademicLevel, 0, 80);

                results.Add(new OrderMatch
                {
                    Order = o,
                    MatchScore = subjectMatch ? score : 0,
                    SubjectMatch = subjectMatch,
                    AcademicLevelMatch = true,
                    IsPremium = false,
                    CanApply = subjectMatch && o.AssignedWriterId == null,
                    RestrictionReason = subjectMatch ? "" : "Subject not in your specialization"
                });
            }

            return results.OrderByDescending(r => r.MatchScore).ToList();
        }

        public async Task<List<MarketplaceWriterRecommendation>> GetRecommendedWritersAsync(int orderId, int maxResults = 10)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null) return new();

            var writers = await _context.Users.Where(u => u.UserType == "Writer" && u.IsActive && !u.IsDeleted && u.IsAcceptingOrders).ToListAsync();
            var result = new List<MarketplaceWriterRecommendation>();

            foreach (var w in writers)
            {
                var specializations = await _context.Set<WriterSpecialization>().Where(s => s.WriterId == w.Id).ToListAsync();
                var subjects = specializations.Select(s => s.Subject.ToLowerInvariant()).ToList();
                var subjectMatch = subjects.Contains(order.Subject.ToLowerInvariant());
                var reliability = await _reliabilityService.GetOrCreateAsync(w.Id);

                var score = subjectMatch ? 50 : 0;
                score += Math.Max(0, 100 - (int)((double)w.CurrentActiveOrders / Math.Max(1, w.MaxActiveOrders) * 100));
                score += w.QualityScore / 2;
                score += (int)w.AverageRating * 5;
                score += reliability.ReliabilityScore / 5;

                if (subjectMatch && w.CurrentActiveOrders < w.MaxActiveOrders)
                {
                    result.Add(new MarketplaceWriterRecommendation
                    {
                        WriterId = w.Id,
                        WriterName = $"{w.FirstName} {w.LastName}",
                        MatchScore = Math.Min(100, score),
                        Rating = w.AverageRating,
                        Reliability = reliability.ReliabilityScore,
                        CompletedOrders = w.TotalCompletedOrders,
                        ActiveOrders = w.CurrentActiveOrders,
                        MaxOrders = w.MaxActiveOrders,
                        Rank = _reliabilityService.GetTier(reliability.ReliabilityScore),
                        IsRecommended = subjectMatch && w.CurrentActiveOrders < w.MaxActiveOrders,
                        SubjectMatch = subjectMatch,
                        Specializations = subjects
                    });
                }
            }

            return result.OrderByDescending(r => r.MatchScore).Take(maxResults).ToList();
        }

        public int CalculateMatchScore(string writerSubjects, AcademicLevel writerMaxLevel, int subjectCount, AcademicLevel orderLevel, double writerRating, int reliability)
        {
            var score = 0;
            if (orderLevel <= writerMaxLevel) score += 20;
            score += (int)(writerRating * 5);
            score += reliability / 5;
            score += Math.Min(subjectCount * 5, 15);
            return Math.Min(100, score);
        }
    }
}