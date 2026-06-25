using Microsoft.EntityFrameworkCore;
using ScholarRescue.Data;
using ScholarRescue.Models;
using ScholarRescue.Models.Enums;

namespace ScholarRescue.Services
{
    /// <summary>
    /// Immutable order timeline service. Events can only be created and viewed - never edited or deleted.
    /// </summary>
    public class OrderTimelineService : IOrderTimelineService
    {
        private readonly ScholarRescueDbContext _context;

        public OrderTimelineService(ScholarRescueDbContext context)
        {
            _context = context;
        }

        public async Task AddEventAsync(int orderId, string createdByUserId, string createdByName,
            TimelineEventType eventType, string title, string? description = null,
            string? metadataJson = null, bool visibleToClient = true,
            bool visibleToWriter = true, bool visibleToAdmin = true)
        {
            var evt = new OrderTimelineEvent
            {
                OrderId = orderId,
                CreatedByUserId = createdByUserId,
                CreatedByName = createdByName,
                EventType = eventType,
                Title = title,
                Description = description,
                Timestamp = DateTime.UtcNow,
                MetadataJson = metadataJson,
                IsVisibleToClient = visibleToClient,
                IsVisibleToWriter = visibleToWriter,
                IsVisibleToAdmin = visibleToAdmin
            };

            _context.Set<OrderTimelineEvent>().Add(evt);
            await _context.SaveChangesAsync();
        }

        public async Task<List<OrderTimelineEvent>> GetEventsAsync(int orderId, string? role = null,
            TimelineEventType? typeFilter = null, int page = 1, int pageSize = 100)
        {
            IQueryable<OrderTimelineEvent> query = _context.Set<OrderTimelineEvent>()
                .Where(e => e.OrderId == orderId);

            // Role-based visibility filtering
            if (role == "Client")
                query = query.Where(e => e.IsVisibleToClient);
            else if (role == "Writer")
                query = query.Where(e => e.IsVisibleToWriter);
            else if (role == "Administrator")
                query = query.Where(e => e.IsVisibleToAdmin);

            if (typeFilter.HasValue)
                query = query.Where(e => e.EventType == typeFilter.Value);

            return await query
                .OrderByDescending(e => e.Timestamp)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public int GetProgressPercentage(OrderStatus status) => status switch
        {
            OrderStatus.PendingPayment => 5,
            OrderStatus.Draft => 5,
            OrderStatus.PendingReview => 10,
            OrderStatus.Open => 20,
            OrderStatus.Assigned => 30,
            OrderStatus.InProgress => 40,
            OrderStatus.DraftSubmitted => 55,
            OrderStatus.RevisionRequested => 60,
            OrderStatus.RevisionSubmitted => 75,
            OrderStatus.FinalSubmitted => 85,
            OrderStatus.Completed => 100,
            OrderStatus.PendingQA => 80,
            OrderStatus.Delivered => 95,
            _ => 0
        };

        public async Task<(List<OrderTimelineEvent> Events, int TotalCount)> GetAllEventsAsync(
            int page = 1, int pageSize = 50, string? search = null,
            DateTime? dateFrom = null, DateTime? dateTo = null,
            TimelineEventType? typeFilter = null)
        {
            IQueryable<OrderTimelineEvent> query = _context.Set<OrderTimelineEvent>()
                .Include(e => e.Order)
                .AsNoTracking();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.ToLowerInvariant();
                query = query.Where(e =>
                    e.Title.ToLower().Contains(term) ||
                    e.Description!.ToLower().Contains(term) ||
                    e.CreatedByName.ToLower().Contains(term));
            }

            if (dateFrom.HasValue) query = query.Where(e => e.Timestamp >= dateFrom.Value);
            if (dateTo.HasValue) query = query.Where(e => e.Timestamp <= dateTo.Value);
            if (typeFilter.HasValue) query = query.Where(e => e.EventType == typeFilter.Value);

            var total = await query.CountAsync();

            var events = await query
                .OrderByDescending(e => e.Timestamp)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (events, total);
        }
    }
}