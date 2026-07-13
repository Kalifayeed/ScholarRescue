using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ScholarRescue.Data;
using ScholarRescue.Models;
using ScholarRescue.Models.Configuration;
using ScholarRescue.Models.Enums;
using ScholarRescue.Models.Matching;

namespace ScholarRescue.Services.Matching
{
    /// <summary>
    /// Intelligent Writer Matching Engine implementation.
    /// Calculates weighted suitability scores for writers against orders.
    /// Integrates with reliability, capacity, rating, and quality systems.
    /// </summary>
    public class WriterMatchingService : IWriterMatchingService
    {
        private readonly ScholarRescueDbContext _context;
        private readonly IWriterReliabilityService _reliabilityService;
        private readonly IWriterRatingService _ratingService;
        private readonly IWriterCapacityService _capacityService;
        private readonly INotificationService _notificationService;
        private readonly IConfigurationService _configurationService;
        private readonly ILogger<WriterMatchingService> _logger;
        private MatchingConfiguration _config;

        public WriterMatchingService(
            ScholarRescueDbContext context,
            IWriterReliabilityService reliabilityService,
            IWriterRatingService ratingService,
            IWriterCapacityService capacityService,
            INotificationService notificationService,
            IConfigurationService configurationService,
            ILogger<WriterMatchingService> logger)
        {
            _context = context;
            _reliabilityService = reliabilityService;
            _ratingService = ratingService;
            _capacityService = capacityService;
            _notificationService = notificationService;
            _configurationService = configurationService;
            _logger = logger;
            _config = LoadConfiguration();
        }

        private MatchingConfiguration LoadConfiguration()
        {
            var setting = _context.PlatformSettings
                .AsNoTracking()
                .FirstOrDefault(s => s.Key == "MatchingConfiguration");

            if (setting != null && !string.IsNullOrWhiteSpace(setting.Value))
            {
                try
                {
                    return JsonSerializer.Deserialize<MatchingConfiguration>(setting.Value)
                        ?? new MatchingConfiguration();
                }
                catch { }
            }

            return new MatchingConfiguration();
        }

        public async Task<List<WriterMatchScore>> CalculateMatchScoresAsync(int orderId)
        {
            var order = await _context.Orders
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null) return new List<WriterMatchScore>();

            // Get all approved writers who are accepting orders
            var writers = await _context.Users
                .AsNoTracking()
                .Where(u => u.UserType == "Writer" && u.IsActive && u.IsAcceptingOrders
                    && u.AvailabilityStatus == WriterAvailabilityStatus.Available)
                .ToListAsync();

            var scores = new List<WriterMatchScore>();

            foreach (var writer in writers)
            {
                var score = await CalculateSingleMatchScoreAsync(order, writer);
                if (score != null)
                    scores.Add(score);
            }

            // Save scores to DB
            var existing = await _context.Set<WriterMatchScore>()
                .Where(s => s.OrderId == orderId)
                .ToListAsync();
            _context.Set<WriterMatchScore>().RemoveRange(existing);

            foreach (var s in scores)
                _context.Set<WriterMatchScore>().Add(s);

            await _context.SaveChangesAsync();

            return scores.OrderByDescending(s => s.MatchPercentage).ToList();
        }

        private async Task<WriterMatchScore?> CalculateSingleMatchScoreAsync(TutoringRequest order, ApplicationUser writer)
        {
            // Check basic eligibility
            if (!await _capacityService.CanAcceptOrderAsync(writer.Id))
                return null;

            var writerReliability = await _reliabilityService.GetOrCreateAsync(writer.Id);
            var (avgRating, totalReviews, _) = await _ratingService.GetWriterStatsAsync(writer.Id);

            // Get specializations
            var specializations = await _context.Set<WriterSpecialization>()
                .AsNoTracking()
                .Where(s => s.WriterId == writer.Id)
                .ToListAsync();

            // --- SECTION 4: Subject Expertise Score (0-100) ---
            var expertiseScore = CalculateSubjectExpertiseScore(order.Subject, specializations);

            // --- SECTION 5: Academic Level Score (0-100) ---
            var academicLevelScore = CalculateAcademicLevelScore(order.AcademicLevel, writer);

            // --- SECTION 6: Reliability Score (0-100) ---
            var reliabilityScore = (double)writerReliability.ReliabilityScore;

            // --- SECTION 7: Client Rating Score (0-100) ---
            var ratingScore = totalReviews > 0 ? (avgRating / 5.0) * 100.0 : 50.0;

            // --- SECTION 8: Capacity Score (0-100) ---
            var capacityPercent = await _capacityService.GetCapacityPercentageAsync(writer.Id);
            var capacityScore = Math.Max(0, 100 - capacityPercent);

            // --- SECTION 9: Deadline Compatibility Score (0-100) ---
            var deadlineScore = CalculateDeadlineScore(order.Deadline, writer.Id);

            // --- Performance Score ---
            var performanceScore = CalculatePerformanceScore(writerReliability);

            // --- Quality Score ---
            var qualityScore = (double)writerReliability.ReliabilityScore;

            // Calculate weighted total
            double totalScore =
                (expertiseScore * _config.SubjectExpertiseWeight / 100.0) +
                (academicLevelScore * _config.AcademicLevelWeight / 100.0) +
                (reliabilityScore * _config.ReliabilityWeight / 100.0) +
                (ratingScore * _config.ClientRatingWeight / 100.0) +
                (qualityScore * _config.QualityScoreWeight / 100.0) +
                (capacityScore * _config.CapacityWeight / 100.0) +
                (deadlineScore * _config.DeadlineCompatibilityWeight / 100.0) +
                (performanceScore * _config.RecentPerformanceWeight / 100.0);

            double matchPercentage = Math.Round(totalScore, 1);

            // Premium order check: orders with budget >= $500 or Urgent priority
            bool isPremiumOrder = order.Priority == PriorityLevel.Urgent || order.Budget >= 500;
            if (isPremiumOrder)
            {
                var canPremium = await _reliabilityService.CanAcceptPremiumOrdersAsync(writer.Id);
                if (!canPremium || reliabilityScore < _config.MinReliabilityForPremium
                    || avgRating < _config.MinRatingForPremium)
                {
                    matchPercentage *= 0.5; // 50% penalty for premium ineligibility
                }
            }

            // Capacity at maximum = not eligible (score 0)
            if (!await _capacityService.CanAcceptOrderAsync(writer.Id))
                return null;

            var score = new WriterMatchScore
            {
                OrderId = order.Id,
                WriterId = writer.Id,
                MatchPercentage = Math.Round(matchPercentage, 1),
                ExpertiseScore = expertiseScore,
                AcademicLevelScore = academicLevelScore,
                ReliabilityScore = reliabilityScore,
                RatingScore = ratingScore,
                CapacityScore = capacityScore,
                DeadlineScore = deadlineScore,
                PerformanceScore = performanceScore,
                QualityScore = qualityScore,
                TotalScore = totalScore,
                CreatedAt = DateTime.UtcNow
            };

            score.Explanation = GenerateExplanation(score);
            return score;
        }

        /// <summary>Section 4: Calculate subject expertise match score.</summary>
        private double CalculateSubjectExpertiseScore(string orderSubject, List<WriterSpecialization> specializations)
        {
            if (string.IsNullOrWhiteSpace(orderSubject) || !specializations.Any())
                return 0;

            var orderSubjectLower = orderSubject.ToLowerInvariant().Trim();

            foreach (var spec in specializations)
            {
                var specSubject = spec.Subject.ToLowerInvariant().Trim();
                if (specSubject == orderSubjectLower)
                {
                    var levelMultiplier = spec.ExpertiseLevel.ToLowerInvariant() switch
                    {
                        "expert" => 1.0,
                        "advanced" => 0.9,
                        "intermediate" => 0.8,
                        "beginner" => 0.7,
                        _ => 0.75
                    };
                    return 100.0 * levelMultiplier;
                }

                if (specSubject.Contains(orderSubjectLower) || orderSubjectLower.Contains(specSubject))
                {
                    var levelMultiplier = spec.ExpertiseLevel.ToLowerInvariant() switch
                    {
                        "expert" => 0.85,
                        "advanced" => 0.75,
                        "intermediate" => 0.65,
                        "beginner" => 0.55,
                        _ => 0.6
                    };
                    return 80.0 * levelMultiplier;
                }
            }

            return 0;
        }

        /// <summary>Section 5: Calculate academic level qualification score.</summary>
        private double CalculateAcademicLevelScore(AcademicLevel orderLevel, ApplicationUser writer)
        {
            var writerLevel = MapEducationToAcademicLevel(writer.Qualification ?? "");

            if (writerLevel == AcademicLevel.Undergraduate && orderLevel == AcademicLevel.Undergraduate)
                return 100;
            if (writerLevel == AcademicLevel.Masters && orderLevel <= AcademicLevel.Masters)
                return 100;
            if (writerLevel == AcademicLevel.PhD && orderLevel <= AcademicLevel.PhD)
                return 100;
            if (writerLevel == AcademicLevel.College && orderLevel == AcademicLevel.College)
                return 100;
            if (writerLevel == AcademicLevel.HighSchool && orderLevel == AcademicLevel.HighSchool)
                return 100;

            // Partial matches: writer has higher level than required
            if (writerLevel > orderLevel)
                return 80;

            // Writer has lower level than required but has Masters+ 
            if (writerLevel < orderLevel && writerLevel >= AcademicLevel.Masters)
                return 50;

            return 0;
        }

        private static AcademicLevel MapEducationToAcademicLevel(string education)
        {
            var edu = education.ToLowerInvariant();
            if (edu.Contains("phd") || edu.Contains("doctorate") || edu.Contains("doctoral"))
                return AcademicLevel.PhD;
            if (edu.Contains("master") || edu.Contains("masters") || edu.Contains("graduate"))
                return AcademicLevel.Masters;
            if (edu.Contains("bachelor") || edu.Contains("undergraduate") || edu.Contains("bachelors") || edu.Contains("degree"))
                return AcademicLevel.Undergraduate;
            if (edu.Contains("college") || edu.Contains("associate") || edu.Contains("diploma"))
                return AcademicLevel.College;
            if (edu.Contains("high school") || edu.Contains("secondary") || edu.Contains("a level"))
                return AcademicLevel.HighSchool;
            return AcademicLevel.Undergraduate;
        }

        /// <summary>Section 9: Calculate deadline compatibility score.</summary>
        private double CalculateDeadlineScore(DateTime deadline, string writerId)
        {
            double hoursUntilDeadline = (deadline - DateTime.UtcNow).TotalHours;
            if (hoursUntilDeadline <= 0) return 0;

            var activeCount = _context.Orders
                .AsNoTracking()
                .Count(o => o.AssignedWriterId == writerId
                    && o.Status != OrderStatus.Completed
                    && o.Status != OrderStatus.Cancelled);

            var upcomingDeadlines = _context.Orders
                .AsNoTracking()
                .Where(o => o.AssignedWriterId == writerId
                    && o.Status != OrderStatus.Completed
                    && o.Status != OrderStatus.Cancelled
                    && o.Deadline > DateTime.UtcNow)
                .Select(o => o.Deadline)
                .ToList();

            double baseScore = Math.Min(100.0, hoursUntilDeadline / 24.0 * 10.0);
            double overlapPenalty = activeCount * 10;

            foreach (var existingDeadline in upcomingDeadlines)
            {
                double overlapHours = Math.Abs((existingDeadline - deadline).TotalHours);
                if (overlapHours < 48)
                    overlapPenalty += 15;
            }

            return Math.Max(0, Math.Min(100.0, baseScore - overlapPenalty));
        }

        /// <summary>Calculate performance score based on reliability record.</summary>
        private double CalculatePerformanceScore(WriterReliability reliability)
        {
            if (reliability == null) return 50;

            double baseScore = reliability.ReliabilityScore;
            double warningPenalty = reliability.Warnings * 5;
            double suspensionPenalty = reliability.Suspensions * 20;

            return Math.Max(0, Math.Min(100, baseScore - warningPenalty - suspensionPenalty));
        }

        public string GenerateExplanation(WriterMatchScore score)
        {
            var reasons = new List<string>();

            if (score.ExpertiseScore >= 80)
                reasons.Add("✓ Subject Expert");
            else if (score.ExpertiseScore >= 50)
                reasons.Add("✓ Partial Subject Match");
            else
                reasons.Add("✗ No Subject Match");

            if (score.AcademicLevelScore >= 80)
                reasons.Add("✓ Qualified Academic Level");
            else if (score.AcademicLevelScore >= 50)
                reasons.Add("✓ Partial Academic Match");
            else
                reasons.Add("✗ Academic Level Mismatch");

            if (score.ReliabilityScore >= 90)
                reasons.Add($"✓ Excellent Reliability ({score.ReliabilityScore}%)");
            else if (score.ReliabilityScore >= 70)
                reasons.Add($"✓ Good Reliability ({score.ReliabilityScore}%)");
            else
                reasons.Add($"⚠ Low Reliability ({score.ReliabilityScore}%)");

            if (score.CapacityScore >= 80)
                reasons.Add("✓ Available Capacity");
            else if (score.CapacityScore >= 50)
                reasons.Add("⚠ Limited Capacity");
            else
                reasons.Add("✗ At Capacity");

            if (score.RatingScore >= 80)
                reasons.Add($"✓ High Rating ({score.RatingScore:F0}/100)");

            if (score.DeadlineScore >= 70)
                reasons.Add("✓ Deadline Compatible");

            return string.Join("\n", reasons);
        }

        public async Task<List<WriterMatchScore>> GetTopRecommendationsAsync(int orderId, int maxResults = 10)
        {
            var scores = await _context.Set<WriterMatchScore>()
                .AsNoTracking()
                .Where(s => s.OrderId == orderId)
                .Include(s => s.Writer)
                .OrderByDescending(s => s.MatchPercentage)
                .Take(maxResults)
                .ToListAsync();

            if (!scores.Any())
            {
                scores = await CalculateMatchScoresAsync(orderId);
                scores = scores.Take(maxResults).ToList();
            }

            return scores;
        }

        public async Task<WriterMatchScore?> GetTopMatchAsync(int orderId)
        {
            var scores = await GetTopRecommendationsAsync(orderId, 1);
            return scores.FirstOrDefault();
        }

        public async Task<bool> IsWriterEligibleAsync(int orderId, string writerId)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null) return false;

            var writer = await _context.Users.FindAsync(writerId);
            if (writer == null || writer.UserType != "Writer" || !writer.IsActive || !writer.IsAcceptingOrders)
                return false;

            if (!await _capacityService.CanAcceptOrderAsync(writerId))
                return false;

            var reliability = await _reliabilityService.GetOrCreateAsync(writerId);

            if (reliability.ReliabilityScore < 50)
                return false;

            // For premium orders (Urgent or >= $500), stricter requirements
            bool isPremiumOrder = order.Priority == PriorityLevel.Urgent || order.Budget >= 500;
            if (isPremiumOrder)
            {
                var (avgRating, totalReviews, _) = await _ratingService.GetWriterStatsAsync(writerId);
                if (reliability.ReliabilityScore < _config.MinReliabilityForPremium
                    || avgRating < _config.MinRatingForPremium)
                    return false;
            }

            return true;
        }

        public async Task<WriterMatchScore?> GetWriterMatchScoreAsync(int orderId, string writerId)
        {
            return await _context.Set<WriterMatchScore>()
                .AsNoTracking()
                .Include(s => s.Writer)
                .FirstOrDefaultAsync(s => s.OrderId == orderId && s.WriterId == writerId);
        }

        public async Task<(bool Assigned, string? WriterId, string Message)> TryAutoAssignAsync(int orderId, string adminId)
        {
            _config = LoadConfiguration();

            if (_config.AutoAssignmentMode == AutoAssignmentMode.Disabled)
                return (false, null, "Auto-assignment is disabled.");

            if (_config.AutoAssignmentMode == AutoAssignmentMode.RecommendationOnly)
                return (false, null, "Auto-assignment is in recommendation-only mode.");

            var recommendations = await GetTopRecommendationsAsync(orderId);

            if (!recommendations.Any())
                return (false, null, "No eligible writers found for auto-assignment.");

            var topMatch = recommendations.First();

            var reliability = await _reliabilityService.GetOrCreateAsync(topMatch.WriterId);
            if (reliability.ReliabilityScore < _config.MinReliabilityForAutoAssign)
                return (false, null, $"Top writer's reliability ({reliability.ReliabilityScore}%) is below minimum ({_config.MinReliabilityForAutoAssign}%).");

            if (topMatch.QualityScore < _config.MinQualityForAutoAssign)
                return (false, null, $"Top writer's quality score ({topMatch.QualityScore}) is below minimum ({_config.MinQualityForAutoAssign}).");

            if (!await _capacityService.CanAcceptOrderAsync(topMatch.WriterId))
                return (false, null, "Top writer has no available capacity.");

            var order = await _context.Orders.FindAsync(orderId);
            if (order == null)
                return (false, null, "Order not found.");

            order.AssignedWriterId = topMatch.WriterId;
            order.AssignedAt = DateTime.UtcNow;
            order.AssignedByAdminId = adminId;
            order.Status = OrderStatus.Assigned;
            order.IsMarketplaceOpen = false;
            order.UpdatedAt = DateTime.UtcNow;

            _context.Orders.Update(order);
            await _capacityService.IncrementActiveOrdersAsync(topMatch.WriterId);

            // Audit log
            _context.AuditLogs.Add(new AuditLog
            {
                Action = "Auto-Assignment",
                PerformedById = adminId,
                TargetUserId = topMatch.WriterId,
                Description = $"Auto-assigned writer to order #{order.OrderNumber} (Match: {topMatch.MatchPercentage}%)",
                CreatedDate = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            // Notify writer
            await _notificationService.CreateNotificationAsync(topMatch.WriterId,
                "Order Auto-Assigned",
                $"You have been automatically assigned to order #{order.OrderNumber}: {order.Title}",
                NotificationType.OrderAssigned, orderId.ToString(), "Order");

            var recommendedIds = recommendations.Select(r => r.WriterId).ToList();
            await RecordAssignmentHistoryAsync(orderId, recommendedIds, topMatch.WriterId, true, topMatch.MatchPercentage);

            _logger.LogInformation("Auto-assigned writer {Writer} to order {Order} with {Match}% match",
                topMatch.WriterId, orderId, topMatch.MatchPercentage);

            return (true, topMatch.WriterId, $"Auto-assigned writer with {topMatch.MatchPercentage}% match.");
        }

        public async Task RecordAssignmentHistoryAsync(int orderId, List<string> recommendedWriterIds,
            string? assignedWriterId, bool wasAutoAssigned, double assignedMatchScore)
        {
            var history = new AssignmentHistory
            {
                OrderId = orderId,
                RecommendedWriterIds = JsonSerializer.Serialize(recommendedWriterIds),
                AssignedWriterId = assignedWriterId,
                WasAutoAssigned = wasAutoAssigned,
                AssignedWriterMatchScore = assignedMatchScore,
                CreatedAt = DateTime.UtcNow
            };

            _context.Set<AssignmentHistory>().Add(history);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAssignmentOutcomeAsync(int orderId, bool completedSuccessfully,
            double? clientRating, bool wasOnTime, int revisionCount, bool hadDispute)
        {
            var history = await _context.Set<AssignmentHistory>()
                .FirstOrDefaultAsync(h => h.OrderId == orderId);

            if (history == null) return;

            history.WasCompletedSuccessfully = completedSuccessfully;
            history.ClientRating = clientRating;
            history.WasOnTime = wasOnTime;
            history.RevisionCount = revisionCount;
            history.HadDispute = hadDispute;
            history.CompletedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
        }

        public async Task<MatchingAnalytics> GetMatchingAnalyticsAsync()
        {
            var analytics = new MatchingAnalytics();

            var allScores = await _context.Set<WriterMatchScore>()
                .AsNoTracking()
                .ToListAsync();

            analytics.AverageMatchScore = allScores.Any()
                ? Math.Round(allScores.Average(s => s.MatchPercentage), 1)
                : 0;

            var assignmentHistories = await _context.Set<AssignmentHistory>()
                .AsNoTracking()
                .ToListAsync();

            analytics.TotalRecommendationsGenerated = assignmentHistories.Sum(h =>
            {
                try { return JsonSerializer.Deserialize<List<string>>(h.RecommendedWriterIds)?.Count ?? 0; }
                catch { return 0; }
            });

            analytics.TotalAutoAssignments = assignmentHistories.Count(h => h.WasAutoAssigned);
            analytics.TotalManualAssignments = assignmentHistories.Count(h => !h.WasAutoAssigned && h.AssignedWriterId != null);

            var completed = assignmentHistories.Where(h => h.WasCompletedSuccessfully.HasValue).ToList();
            analytics.AssignmentSuccessRate = completed.Any()
                ? Math.Round((double)completed.Count(h => h.WasCompletedSuccessfully == true) / completed.Count * 100.0, 1)
                : 0;

            var autoCompleted = completed.Where(h => h.WasAutoAssigned).ToList();
            analytics.AutoAssignmentSuccessRate = autoCompleted.Any()
                ? Math.Round((double)autoCompleted.Count(h => h.WasCompletedSuccessfully == true) / autoCompleted.Count * 100.0, 1)
                : 0;

            var writerSuccess = assignmentHistories
                .Where(h => h.WasCompletedSuccessfully.HasValue && h.AssignedWriterId != null)
                .GroupBy(h => h.AssignedWriterId!)
                .Select(g => new
                {
                    WriterId = g.Key,
                    Count = g.Count(),
                    SuccessCount = g.Count(h => h.WasCompletedSuccessfully == true),
                    AvgMatch = g.Average(h => h.AssignedWriterMatchScore)
                })
                .OrderByDescending(x => x.SuccessCount)
                .Take(5)
                .ToList();

            foreach (var ws in writerSuccess)
            {
                var writer = await _context.Users.FindAsync(ws.WriterId);
                analytics.TopPerformingWriters.Add(new TopWriterInfo
                {
                    WriterId = ws.WriterId,
                    WriterName = writer != null ? $"{writer.FirstName} {writer.LastName}" : "Unknown",
                    AverageMatchScore = Math.Round(ws.AvgMatch, 1),
                    AssignmentsCompleted = ws.Count,
                    SuccessRate = Math.Round((double)ws.SuccessCount / ws.Count * 100.0, 1)
                });
            }

            var assignedOrders = await _context.Orders
                .AsNoTracking()
                .Where(o => o.AssignedAt.HasValue && o.Status == OrderStatus.Completed && o.CompletedAt.HasValue)
                .ToListAsync();

            if (assignedOrders.Any())
            {
                double avgTicks = assignedOrders.Average(o => (o.CompletedAt!.Value - o.AssignedAt!.Value).Ticks);
                analytics.AverageAssignmentTime = new TimeSpan((long)avgTicks);
            }

            return analytics;
        }

        public async Task NotifyHighMatchWritersAsync(int orderId)
        {
            if (!_config.NotifyWritersOnHighMatch) return;

            var topMatches = await GetTopRecommendationsAsync(orderId, 5);
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null) return;

            foreach (var match in topMatches.Where(m => m.MatchPercentage >= 70))
            {
                await _notificationService.CreateNotificationAsync(match.WriterId,
                    "High-Match Order Available",
                    $"A new order '{order.Title}' matches your expertise ({match.MatchPercentage}% match). Apply now!",
                    NotificationType.NewOrder, orderId.ToString(), "Order");
            }

            if (_config.NotifyAdminOnRecommendations)
            {
                var admins = await _context.UserRoles
                    .AsNoTracking()
                    .Where(ur => ur.RoleId == _context.Roles
                        .Where(r => r.Name == "Administrator")
                        .Select(r => r.Id)
                        .FirstOrDefault())
                    .Select(ur => ur.UserId)
                    .ToListAsync();

                foreach (var adminId in admins)
                {
                    await _notificationService.CreateNotificationAsync(adminId,
                        "Recommendations Generated",
                        $"Top {topMatches.Count} writer recommendations generated for order #{order.OrderNumber}.",
                        NotificationType.SystemAlert, orderId.ToString(), "Order");
                }
            }
        }

        public async Task<List<WriterMatchScore>> RankApplicationsAsync(int orderId)
        {
            var applications = await _context.OrderApplications
                .AsNoTracking()
                .Where(a => a.OrderId == orderId && a.Status == OrderApplicationStatus.Pending)
                .ToListAsync();

            var order = await _context.Orders.FindAsync(orderId);
            if (order == null) return new List<WriterMatchScore>();

            var scores = new List<WriterMatchScore>();

            foreach (var app in applications)
            {
                var writer = await _context.Users.FindAsync(app.WriterId);
                if (writer == null) continue;

                var score = await CalculateSingleMatchScoreAsync(order, writer);
                if (score != null)
                    scores.Add(score);
            }

            return scores.OrderByDescending(s => s.MatchPercentage).ToList();
        }
    }
}