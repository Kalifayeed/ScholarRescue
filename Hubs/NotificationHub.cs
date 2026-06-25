using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using ScholarRescue.Services;

namespace ScholarRescue.Hubs
{
    /// <summary>
    /// SignalR Hub for real-time notification delivery.
    /// Clients receive instant push notifications, unread count updates, and toast alerts.
    /// </summary>
    [Authorize]
    public class NotificationHub : Hub
    {
        private readonly IUserPresenceService _presenceService;

        public NotificationHub(IUserPresenceService presenceService)
        {
            _presenceService = presenceService;
        }

        /// <summary>
        /// Registers the connection with the user's notification group.
        /// </summary>
        public override async Task OnConnectedAsync()
        {
            var userId = Context.UserIdentifier;
            if (!string.IsNullOrEmpty(userId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"notifications_{userId}");
            }

            await base.OnConnectedAsync();
        }

        /// <summary>
        /// Removes the connection from the user's notification group.
        /// </summary>
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.UserIdentifier;
            if (!string.IsNullOrEmpty(userId))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"notifications_{userId}");
            }

            await base.OnDisconnectedAsync(exception);
        }

        /// <summary>
        /// Sends a notification in real-time to a specific user.
        /// Called by the server-side notification service after creating a notification.
        /// </summary>
        public async Task SendNotification(string userId, object notificationData)
        {
            await Clients.Group($"notifications_{userId}")
                .SendAsync("ReceiveNotification", notificationData);
        }

        /// <summary>
        /// Sends a toast alert to a specific user.
        /// </summary>
        public async Task SendToast(string userId, string type, string title, string message)
        {
            await Clients.Group($"notifications_{userId}")
                .SendAsync("ReceiveToast", new
                {
                    Type = type,
                    Title = title,
                    Message = message,
                    Timestamp = DateTime.UtcNow
                });
        }

        /// <summary>
        /// Broadcasts an updated unread count to a specific user.
        /// </summary>
        public async Task UpdateUnreadCount(string userId, int count)
        {
            await Clients.Group($"notifications_{userId}")
                .SendAsync("UnreadCountUpdated", count);
        }
    }
}