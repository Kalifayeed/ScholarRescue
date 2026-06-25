using ScholarRescue.Models;
using ScholarRescue.Models.Enums;

namespace ScholarRescue.Services
{
    /// <summary>
    /// Service interface for notification creation, retrieval, and management.
    /// Designed for extensibility - future channels (SMS, WhatsApp, Push) can be added via INotificationChannel.
    /// </summary>
    public interface INotificationService
    {
        /// <summary>
        /// Creates a notification for a specific user.
        /// </summary>
        Task<Notification> CreateNotificationAsync(string userId, string title, string message,
            NotificationType notificationType, string? relatedEntityId = null, string? relatedEntityType = null);

        /// <summary>
        /// Creates a notification and sends email, WhatsApp, SMS, etc. based on user preferences.
        /// </summary>
        Task<Notification> CreateAndSendAsync(string userId, string email, string title, string message,
            NotificationType notificationType, string? relatedEntityId = null, string? relatedEntityType = null);

        /// <summary>
        /// Gets paginated notifications for a user.
        /// </summary>
        Task<(List<Notification> Notifications, int TotalCount)> GetUserNotificationsAsync(
            string userId, int page = 1, int pageSize = 20,
            string? filter = null, string? search = null, NotificationCategory? category = null);

        /// <summary>
        /// Gets the latest notifications for the navbar dropdown.
        /// </summary>
        Task<List<Notification>> GetRecentNotificationsAsync(string userId, int take = 10);

        /// <summary>
        /// Gets the count of unread notifications for a user.
        /// </summary>
        Task<int> GetUnreadCountAsync(string userId);

        /// <summary>
        /// Marks a single notification as read.
        /// </summary>
        Task MarkAsReadAsync(int notificationId, string userId);

        /// <summary>
        /// Marks all unread notifications as read for a user.
        /// </summary>
        Task MarkAllAsReadAsync(string userId);

        /// <summary>
        /// Deletes a notification.
        /// </summary>
        Task DeleteNotificationAsync(int notificationId, string userId);

        /// <summary>
        /// Gets a single notification by ID, verifying ownership.
        /// </summary>
        Task<Notification?> GetNotificationByIdAsync(int notificationId, string userId);

        /// <summary>
        /// Gets or creates notification settings for a user.
        /// </summary>
        Task<NotificationSettings> GetSettingsAsync(string userId);

        /// <summary>
        /// Updates notification settings for a user.
        /// </summary>
        Task UpdateSettingsAsync(string userId, bool notifyMessages, bool notifyOrders,
            bool notifyAssignments, bool notifyRevisions, bool notifySystemAlerts);

        // --- Admin methods ---

        /// <summary>
        /// Gets all notifications across the platform (admin only).
        /// </summary>
        Task<(List<Notification> Notifications, int TotalCount)> GetAllNotificationsAsync(
            int page = 1, int pageSize = 25, string? search = null,
            string? filter = null, NotificationCategory? category = null, string? userName = null);

        /// <summary>
        /// Sends a system alert to all users.
        /// </summary>
        Task SendSystemAlertAsync(string title, string message);

        // --- Automatic Notification Events ---

        /// <summary>Writer application approved by admin.</summary>
        Task NotifyWriterApprovedAsync(string userId, string email, string writerName);

        /// <summary>New order created (notifies admin).</summary>
        Task NotifyNewOrderCreatedAsync(int orderId, string orderNumber);

        /// <summary>Writer applies for an order (notifies admin).</summary>
        Task NotifyWriterAppliedForOrderAsync(int orderId, string writerId, string orderNumber);

        /// <summary>Order assigned to writer (notifies writer and client).</summary>
        Task NotifyOrderAssignedAsync(int orderId, string writerId, string clientId, string orderNumber);

        /// <summary>New chat message received.</summary>
        Task NotifyNewMessageAsync(int conversationId, string receiverId, string senderName);

        /// <summary>File uploaded to order (notifies other participant).</summary>
        Task NotifyFileUploadedAsync(int orderId, string receiverId, string orderNumber, string uploaderName);

        /// <summary>Revision requested on an order (notifies writer).</summary>
        Task NotifyRevisionRequestedAsync(int orderId, string writerId, string orderNumber);

        /// <summary>Dispute opened on order (notifies admin, client, writer).</summary>
        Task NotifyDisputeOpenedAsync(int orderId, string orderNumber, string adminId, string clientId, string writerId);

        /// <summary>Payout requested by writer (notifies admin).</summary>
        Task NotifyPayoutRequestedAsync(string adminId, string writerName, decimal amount);

        /// <summary>Payout approved (notifies writer).</summary>
        Task NotifyPayoutApprovedAsync(string writerId, string writerEmail, decimal amount);

        /// <summary>Payout rejected (notifies writer).</summary>
        Task NotifyPayoutRejectedAsync(string writerId, string writerEmail, decimal amount, string? reason);

        /// <summary>Deadline reminder (notifies writer and admin).</summary>
        Task NotifyDeadlineReminderAsync(int orderId, string userId, string orderNumber, int hoursRemaining);

        /// <summary>Archives a notification.</summary>
        Task ArchiveNotificationAsync(int notificationId, string userId);

        /// <summary>Unarchives a notification.</summary>
        Task UnarchiveNotificationAsync(int notificationId, string userId);

        /// <summary>Gets archived notifications for a user.</summary>
        Task<(List<Notification> Notifications, int TotalCount)> GetArchivedNotificationsAsync(
            string userId, int page = 1, int pageSize = 20);

        /// <summary>Broadcasts a notification to all users with priority.</summary>
        Task BroadcastNotificationAsync(string title, string message, NotificationPriority priority,
            string broadcastById, string? targetAudience = null);
    }
}