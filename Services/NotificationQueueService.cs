using Microsoft.EntityFrameworkCore;
using ScholarRescue.Data;
using ScholarRescue.Models;

namespace ScholarRescue.Services
{
    public class NotificationQueueService : INotificationQueueService
    {
        private readonly ScholarRescueDbContext _context;
        private readonly ILogger<NotificationQueueService> _logger;

        public NotificationQueueService(
            ScholarRescueDbContext context,
            ILogger<NotificationQueueService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task EnqueueNotificationAsync(int notificationId, string userId, string deliveryMethod = "InApp")
        {
            var delivery = new NotificationDelivery
            {
                NotificationId = notificationId,
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                DeliveryMethod = deliveryMethod,
                IsDelivered = false,
                IsRead = false,
                RetryCount = 0
            };

            _context.Set<NotificationDelivery>().Add(delivery);
            await _context.SaveChangesAsync();
        }

        public async Task ProcessQueueAsync()
        {
            var pending = await _context.Set<NotificationDelivery>()
                .Where(d => !d.IsDelivered && d.RetryCount < 5)
                .OrderBy(d => d.CreatedAt)
                .Take(50)
                .ToListAsync();

            foreach (var delivery in pending)
            {
                try
                {
                    delivery.DeliveredAt = DateTime.UtcNow;
                    delivery.IsDelivered = true;
                    delivery.RetryCount++;
                }
                catch (Exception ex)
                {
                    delivery.RetryCount++;
                    delivery.ErrorMessage = ex.Message;
                    _logger.LogWarning(ex, "Notification delivery failed for ID {DeliveryId}", delivery.Id);
                }
            }

            await _context.SaveChangesAsync();
        }

        public async Task<List<NotificationDelivery>> GetPendingDeliveriesAsync()
        {
            return await _context.Set<NotificationDelivery>()
                .Where(d => !d.IsDelivered && d.RetryCount < 5)
                .OrderBy(d => d.CreatedAt)
                .Take(100)
                .ToListAsync();
        }

        public async Task<List<NotificationDelivery>> GetDeliveriesForNotificationAsync(int notificationId)
        {
            return await _context.Set<NotificationDelivery>()
                .Where(d => d.NotificationId == notificationId)
                .OrderByDescending(d => d.CreatedAt)
                .ToListAsync();
        }

        public async Task MarkAsDeliveredAsync(int deliveryId)
        {
            var delivery = await _context.Set<NotificationDelivery>().FindAsync(deliveryId);
            if (delivery != null)
            {
                delivery.IsDelivered = true;
                delivery.DeliveredAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }

        public async Task MarkAsReadAsync(int deliveryId)
        {
            var delivery = await _context.Set<NotificationDelivery>().FindAsync(deliveryId);
            if (delivery != null)
            {
                delivery.IsRead = true;
                delivery.ReadAt = DateTime.UtcNow;
                if (!delivery.IsDelivered)
                {
                    delivery.IsDelivered = true;
                    delivery.DeliveredAt = DateTime.UtcNow;
                }
                await _context.SaveChangesAsync();
            }
        }

        public async Task<int> GetPendingCountAsync()
        {
            return await _context.Set<NotificationDelivery>()
                .CountAsync(d => !d.IsDelivered && d.RetryCount < 5);
        }

        public async Task<int> GetFailedCountAsync()
        {
            return await _context.Set<NotificationDelivery>()
                .CountAsync(d => !d.IsDelivered && d.RetryCount >= 5);
        }
    }
}