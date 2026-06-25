using ScholarRescue.Models;

namespace ScholarRescue.Services
{
    public interface INotificationQueueService
    {
        Task EnqueueNotificationAsync(int notificationId, string userId, string deliveryMethod = "InApp");
        Task ProcessQueueAsync();
        Task<List<NotificationDelivery>> GetPendingDeliveriesAsync();
        Task<List<NotificationDelivery>> GetDeliveriesForNotificationAsync(int notificationId);
        Task MarkAsDeliveredAsync(int deliveryId);
        Task MarkAsReadAsync(int deliveryId);
        Task<int> GetPendingCountAsync();
        Task<int> GetFailedCountAsync();
    }
}