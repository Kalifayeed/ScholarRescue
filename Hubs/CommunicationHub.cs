using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using ScholarRescue.Services;

namespace ScholarRescue.Hubs
{
    /// <summary>
    /// SignalR Hub for real-time communication hub updates.
    /// Delivers live updates for messages, notifications, announcements, and support tickets.
    /// </summary>
    [Authorize]
    public class CommunicationHub : Hub
    {
        private readonly IUserPresenceService _presenceService;

        public CommunicationHub(IUserPresenceService presenceService)
        {
            _presenceService = presenceService;
        }

        public override async Task OnConnectedAsync()
        {
            var userId = Context.UserIdentifier;
            if (!string.IsNullOrEmpty(userId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"communication_{userId}");
                await _presenceService.UserConnectedAsync(userId, Context.ConnectionId);
            }
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.UserIdentifier;
            if (!string.IsNullOrEmpty(userId))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"communication_{userId}");
                await _presenceService.UserDisconnectedAsync(userId, Context.ConnectionId);
            }
            await base.OnDisconnectedAsync(exception);
        }

        /// <summary>Broadcasts a new announcement to targeted users.</summary>
        public async Task NewAnnouncement(string userId, object announcementData)
        {
            await Clients.Group($"communication_{userId}")
                .SendAsync("ReceiveAnnouncement", announcementData);
        }

        /// <summary>Sends a communication hub notification (message, ticket update, etc.).</summary>
        public async Task SendHubNotification(string userId, string type, string title, string message)
        {
            await Clients.Group($"communication_{userId}")
                .SendAsync("HubNotification", new
                {
                    Type = type,
                    Title = title,
                    Message = message,
                    Timestamp = DateTime.UtcNow
                });
        }

        /// <summary>Updates the unread count badge on the communication hub.</summary>
        public async Task UpdateBadgeCount(string userId, string badgeType, int count)
        {
            await Clients.Group($"communication_{userId}")
                .SendAsync("BadgeCountUpdated", new
                {
                    BadgeType = badgeType,
                    Count = count
                });
        }
    }
}