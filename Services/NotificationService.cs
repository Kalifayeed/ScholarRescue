using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using ScholarRescue.Data;
using ScholarRescue.Models;
using ScholarRescue.Models.Enums;

namespace ScholarRescue.Services
{
    /// <summary>
    /// Implementation of the notification service with full CRUD, filtering,
    /// automatic notification events, audit logging, and multi-channel support.
    /// </summary>
    public class NotificationService : INotificationService
    {
        private readonly ScholarRescueDbContext _context;
        private readonly IUserPresenceService _presenceService;
        private readonly IEmailService _emailService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(
            ScholarRescueDbContext context,
            IUserPresenceService presenceService,
            IEmailService emailService,
            UserManager<ApplicationUser> userManager,
            ILogger<NotificationService> logger)
        {
            _context = context;
            _presenceService = presenceService;
            _emailService = emailService;
            _userManager = userManager;
            _logger = logger;
        }

        // ----------------------------------------------------------------
        // CORE OPERATIONS
        // ----------------------------------------------------------------

        public async Task<Notification> CreateNotificationAsync(string userId, string title, string message,
            NotificationType notificationType, string? relatedEntityId = null, string? relatedEntityType = null)
        {
            var notification = new Notification
            {
                UserId = userId,
                Title = title,
                Message = message,
                NotificationType = notificationType,
                IsRead = false,
                CreatedAt = DateTime.UtcNow,
                RelatedEntityId = relatedEntityId,
                RelatedEntityType = relatedEntityType
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            // Audit log
            _context.AuditLogs.Add(new AuditLog
            {
                Action = "Notification Created",
                PerformedById = userId,
                TargetUserId = userId,
                Description = $"Notification '{title}' ({notificationType}) for user {userId}" +
                    (relatedEntityId != null ? $" | Entity: {relatedEntityType}#{relatedEntityId}" : ""),
                CreatedDate = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();

            return notification;
        }

        public async Task<Notification> CreateAndSendAsync(string userId, string email, string title, string message,
            NotificationType notificationType, string? relatedEntityId = null, string? relatedEntityType = null)
        {
            // Delegate to EmailService which handles both notification creation and email sending
            return await _emailService.CreateAndSendNotificationAsync(
                userId, email, title, message, notificationType, relatedEntityId, relatedEntityType);
        }

        public async Task<(List<Notification> Notifications, int TotalCount)> GetUserNotificationsAsync(
            string userId, int page = 1, int pageSize = 20,
            string? filter = null, string? search = null, NotificationCategory? category = null)
        {
            IQueryable<Notification> query = _context.Notifications
                .Where(n => n.UserId == userId);

            // Filter by read/unread
            if (filter == "unread")
                query = query.Where(n => !n.IsRead);
            else if (filter == "read")
                query = query.Where(n => n.IsRead);

            // Search in title/message
            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLowerInvariant();
                query = query.Where(n =>
                    n.Title.ToLower().Contains(term) ||
                    n.Message.ToLower().Contains(term));
            }

            // Category filter
            if (category.HasValue)
            {
                var cat = category.Value;
                query = query.Where(n => GetNotificationCategory(n.NotificationType) == cat);
            }

            var totalCount = await query.CountAsync();

            var notifications = await query
                .OrderByDescending(n => n.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (notifications, totalCount);
        }

        public async Task<List<Notification>> GetRecentNotificationsAsync(string userId, int take = 10)
        {
            return await _context.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Take(take)
                .ToListAsync();
        }

        public async Task<int> GetUnreadCountAsync(string userId)
        {
            return await _context.Notifications
                .CountAsync(n => n.UserId == userId && !n.IsRead);
        }

        public async Task MarkAsReadAsync(int notificationId, string userId)
        {
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);

            if (notification != null && !notification.IsRead)
            {
                notification.IsRead = true;
                notification.ReadAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }

        public async Task MarkAllAsReadAsync(string userId)
        {
            var now = DateTime.UtcNow;
            var unread = await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync();

            foreach (var n in unread)
            {
                n.IsRead = true;
                n.ReadAt = now;
            }

            await _context.SaveChangesAsync();
        }

        public async Task DeleteNotificationAsync(int notificationId, string userId)
        {
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);

            if (notification != null)
            {
                _context.Notifications.Remove(notification);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<Notification?> GetNotificationByIdAsync(int notificationId, string userId)
        {
            return await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);
        }

        // ----------------------------------------------------------------
        // SETTINGS
        // ----------------------------------------------------------------

        public async Task<NotificationSettings> GetSettingsAsync(string userId)
        {
            var settings = await _context.Set<NotificationSettings>()
                .FirstOrDefaultAsync(s => s.UserId == userId);

            if (settings == null)
            {
                settings = new NotificationSettings { UserId = userId };
                _context.Set<NotificationSettings>().Add(settings);
                await _context.SaveChangesAsync();
            }

            return settings;
        }

        public async Task UpdateSettingsAsync(string userId, bool notifyMessages, bool notifyOrders,
            bool notifyAssignments, bool notifyRevisions, bool notifySystemAlerts)
        {
            var settings = await GetSettingsAsync(userId);

            settings.NotifyMessages = notifyMessages;
            settings.NotifyOrders = notifyOrders;
            settings.NotifyAssignments = notifyAssignments;
            settings.NotifyRevisions = notifyRevisions;
            settings.NotifySystemAlerts = notifySystemAlerts;

            await _context.SaveChangesAsync();
        }

        // ----------------------------------------------------------------
        // ADMIN METHODS
        // ----------------------------------------------------------------

        public async Task<(List<Notification> Notifications, int TotalCount)> GetAllNotificationsAsync(
            int page = 1, int pageSize = 25, string? search = null,
            string? filter = null, NotificationCategory? category = null, string? userName = null)
        {
            IQueryable<Notification> query = _context.Notifications
                .Include(n => n.User);

            // Filter
            if (filter == "unread")
                query = query.Where(n => !n.IsRead);
            else if (filter == "read")
                query = query.Where(n => n.IsRead);

            // Search in title/message
            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLowerInvariant();
                query = query.Where(n =>
                    n.Title.ToLower().Contains(term) ||
                    n.Message.ToLower().Contains(term));
            }

            // User name filter
            if (!string.IsNullOrWhiteSpace(userName))
            {
                var term = userName.Trim().ToLowerInvariant();
                query = query.Where(n =>
                    (n.User.FirstName + " " + n.User.LastName).ToLower().Contains(term) ||
                    n.User.Email!.ToLower().Contains(term));
            }

            // Category filter
            if (category.HasValue)
            {
                var cat = category.Value;
                query = query.Where(n => GetNotificationCategory(n.NotificationType) == cat);
            }

            var totalCount = await query.CountAsync();

            var notifications = await query
                .OrderByDescending(n => n.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (notifications, totalCount);
        }

        public async Task SendSystemAlertAsync(string title, string message)
        {
            var users = await _context.Users
                .Where(u => u.IsActive && !u.IsDeleted)
                .ToListAsync();

            foreach (var user in users)
            {
                var notification = new Notification
                {
                    UserId = user.Id,
                    Title = title,
                    Message = message,
                    NotificationType = NotificationType.SystemAlert,
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow,
                    RelatedEntityType = "System"
                };

                _context.Notifications.Add(notification);
            }

            await _context.SaveChangesAsync();
        }

        // ----------------------------------------------------------------
        // AUTOMATIC NOTIFICATION EVENTS
        // ----------------------------------------------------------------

        /// <summary>Writer application approved - notifies the writer.</summary>
        public async Task NotifyWriterApprovedAsync(string userId, string email, string writerName)
        {
            var title = "Application Approved";
            var message = "Congratulations! Your writer application has been approved and you may now apply for available orders.";

            await CreateAndSendAsync(userId, email, title, message,
                NotificationType.WriterApproved, userId, "User");
        }

        /// <summary>New order created - notifies admin.</summary>
        public async Task NotifyNewOrderCreatedAsync(int orderId, string orderNumber)
        {
            var admins = await _userManager.GetUsersInRoleAsync("Administrator");
            var title = "New Order Posted";
            var message = $"A new client order ({orderNumber}) has been submitted and requires review.";

            foreach (var admin in admins)
            {
                await CreateAndSendAsync(admin.Id, admin.Email!, title, message,
                    NotificationType.NewOrder, orderId.ToString(), "Order");
            }
        }

        /// <summary>Writer applies for an order - notifies admin.</summary>
        public async Task NotifyWriterAppliedForOrderAsync(int orderId, string writerId, string orderNumber)
        {
            var admins = await _userManager.GetUsersInRoleAsync("Administrator");

            // Get writer name
            var writer = await _userManager.FindByIdAsync(writerId);
            var writerName = writer != null ? $"{writer.FirstName} {writer.LastName}" : "A writer";

            var title = "Writer Applied";
            var message = $"{writerName} has applied for order {orderNumber}.";

            foreach (var admin in admins)
            {
                await CreateAndSendAsync(admin.Id, admin.Email!, title, message,
                    NotificationType.WriterApplied, orderId.ToString(), "Order");
            }
        }

        /// <summary>Order assigned - notifies both writer and client.</summary>
        public async Task NotifyOrderAssignedAsync(int orderId, string writerId, string clientId, string orderNumber)
        {
            // Notify writer
            var writer = await _userManager.FindByIdAsync(writerId);
            if (writer != null)
            {
                await CreateAndSendAsync(writerId, writer.Email!, "Order Assigned",
                    $"You have been assigned a new order ({orderNumber}).",
                    NotificationType.OrderAssigned, orderId.ToString(), "Order");
            }

            // Notify client
            var client = await _userManager.FindByIdAsync(clientId);
            if (client != null)
            {
                await CreateAndSendAsync(clientId, client.Email!, "Writer Assigned",
                    $"A writer has been assigned to your order ({orderNumber}).",
                    NotificationType.WriterAssigned, orderId.ToString(), "Order");
            }
        }

        /// <summary>New message - notifies the receiver.</summary>
        public async Task NotifyNewMessageAsync(int conversationId, string receiverId, string senderName)
        {
            var receiver = await _userManager.FindByIdAsync(receiverId);
            if (receiver == null) return;

            await CreateAndSendAsync(receiverId, receiver.Email!, "New Message",
                $"You have received a new message from {senderName} regarding your order.",
                NotificationType.NewMessage, conversationId.ToString(), "Conversation");
        }

        /// <summary>File uploaded - notifies the other participant.</summary>
        public async Task NotifyFileUploadedAsync(int orderId, string receiverId, string orderNumber, string uploaderName)
        {
            var receiver = await _userManager.FindByIdAsync(receiverId);
            if (receiver == null) return;

            await CreateAndSendAsync(receiverId, receiver.Email!, "New File Uploaded",
                $"{uploaderName} has uploaded a new file to order {orderNumber}.",
                NotificationType.FileUploaded, orderId.ToString(), "Order");
        }

        /// <summary>Revision requested - notifies writer.</summary>
        public async Task NotifyRevisionRequestedAsync(int orderId, string writerId, string orderNumber)
        {
            var writer = await _userManager.FindByIdAsync(writerId);
            if (writer == null) return;

            await CreateAndSendAsync(writerId, writer.Email!, "Revision Requested",
                $"Client has requested revisions on your submission for order {orderNumber}.",
                NotificationType.RevisionRequested, orderId.ToString(), "Order");
        }

        /// <summary>Dispute opened - notifies admin, client, and writer.</summary>
        public async Task NotifyDisputeOpenedAsync(int orderId, string orderNumber, string adminId, string clientId, string writerId)
        {
            var title = "Dispute Opened";
            var message = $"A dispute has been opened on order {orderNumber} requiring review.";

            // Notify admin
            var admin = await _userManager.FindByIdAsync(adminId);
            if (admin != null)
            {
                await CreateAndSendAsync(adminId, admin.Email!, title, message,
                    NotificationType.DisputeOpened, orderId.ToString(), "Order");
            }

            // Notify client
            var client = await _userManager.FindByIdAsync(clientId);
            if (client != null)
            {
                await CreateAndSendAsync(clientId, client.Email!, title, message,
                    NotificationType.DisputeOpened, orderId.ToString(), "Order");
            }

            // Notify writer
            var writer = await _userManager.FindByIdAsync(writerId);
            if (writer != null)
            {
                await CreateAndSendAsync(writerId, writer.Email!, title, message,
                    NotificationType.DisputeOpened, orderId.ToString(), "Order");
            }
        }

        /// <summary>Payout requested - notifies admin.</summary>
        public async Task NotifyPayoutRequestedAsync(string adminId, string writerName, decimal amount)
        {
            var admin = await _userManager.FindByIdAsync(adminId);
            if (admin == null) return;

            await CreateAndSendAsync(adminId, admin.Email!, "Payout Request Submitted",
                $"{writerName} has submitted a payout request for ${amount:F2}.",
                NotificationType.PayoutRequested, adminId, "Payout");
        }

        /// <summary>Payout approved - notifies writer.</summary>
        public async Task NotifyPayoutApprovedAsync(string writerId, string writerEmail, decimal amount)
        {
            await CreateAndSendAsync(writerId, writerEmail, "Payout Approved",
                $"Your payout request for ${amount:F2} has been approved.",
                NotificationType.PayoutApproved, writerId, "Payout");
        }

        /// <summary>Payout rejected - notifies writer.</summary>
        public async Task NotifyPayoutRejectedAsync(string writerId, string writerEmail, decimal amount, string? reason)
        {
            var msg = $"Your payout request for ${amount:F2} has been rejected.";
            if (!string.IsNullOrWhiteSpace(reason))
                msg += $" Reason: {reason}";

            await CreateAndSendAsync(writerId, writerEmail, "Payout Rejected",
                msg, NotificationType.PayoutRejected, writerId, "Payout");
        }

        /// <summary>Deadline reminder - notifies writer/admin.</summary>
        public async Task NotifyDeadlineReminderAsync(int orderId, string userId, string orderNumber, int hoursRemaining)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return;

            var timeLabel = hoursRemaining switch
            {
                24 => "24 hours",
                12 => "12 hours",
                6 => "6 hours",
                2 => "2 hours",
                _ => $"{hoursRemaining} hours"
            };

            await CreateAndSendAsync(userId, user.Email!, "Deadline Reminder",
                $"Order #{orderNumber} is due in {timeLabel}.",
                NotificationType.DeadlineReminder, orderId.ToString(), "Order");
        }

        // ----------------------------------------------------------------
        // HELPERS
        // ----------------------------------------------------------------

        /// <summary>
        /// Maps NotificationType to a NotificationCategory for filtering.
        /// </summary>
        public static NotificationCategory GetNotificationCategory(NotificationType type)
        {
            return type switch
            {
                NotificationType.NewMessage => NotificationCategory.Messages,
                NotificationType.OrderAssigned => NotificationCategory.Assignments,
                NotificationType.WriterAssigned => NotificationCategory.Assignments,
                NotificationType.RevisionRequested => NotificationCategory.Revisions,
                NotificationType.OrderSubmitted => NotificationCategory.Orders,
                NotificationType.OrderCompleted => NotificationCategory.Orders,
                NotificationType.NewOrder => NotificationCategory.Orders,
                NotificationType.FileUploaded => NotificationCategory.Orders,
                NotificationType.WriterApproved => NotificationCategory.Applications,
                NotificationType.WriterApplicationRejected => NotificationCategory.Applications,
                NotificationType.WriterApplied => NotificationCategory.Applications,
                NotificationType.WriterRejected => NotificationCategory.Applications,
                NotificationType.OrderReassigned => NotificationCategory.Assignments,
                NotificationType.DisputeOpened => NotificationCategory.System,
                NotificationType.DisputeResolved => NotificationCategory.System,
                NotificationType.PayoutRequested => NotificationCategory.System,
                NotificationType.PayoutApproved => NotificationCategory.System,
                NotificationType.PayoutRejected => NotificationCategory.System,
                NotificationType.DeadlineReminder => NotificationCategory.System,
                NotificationType.SystemAlert => NotificationCategory.System,
                NotificationType.System => NotificationCategory.System,
                _ => NotificationCategory.System
            };
        }

        /// <summary>
        /// Gets admin user IDs. Cached and reused across notification events.
        /// </summary>
        public async Task<List<string>> GetAdminUserIdsAsync()
        {
            var admins = await _userManager.GetUsersInRoleAsync("Administrator");
            return admins.Select(a => a.Id).ToList();
        }

        // ----------------------------------------------------------------
        // ARCHIVE OPERATIONS
        // ----------------------------------------------------------------

        public async Task ArchiveNotificationAsync(int notificationId, string userId)
        {
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);

            if (notification != null)
            {
                notification.IsArchived = true;
                await _context.SaveChangesAsync();
            }
        }

        public async Task UnarchiveNotificationAsync(int notificationId, string userId)
        {
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);

            if (notification != null)
            {
                notification.IsArchived = false;
                await _context.SaveChangesAsync();
            }
        }

        public async Task<(List<Notification> Notifications, int TotalCount)> GetArchivedNotificationsAsync(
            string userId, int page = 1, int pageSize = 20)
        {
            var query = _context.Notifications
                .Where(n => n.UserId == userId && n.IsArchived);

            var totalCount = await query.CountAsync();

            var notifications = await query
                .OrderByDescending(n => n.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (notifications, totalCount);
        }

        public async Task BroadcastNotificationAsync(string title, string message, NotificationPriority priority,
            string broadcastById, string? targetAudience = null)
        {
            var users = targetAudience switch
            {
                "Clients" => await _userManager.GetUsersInRoleAsync("Client"),
                "Writers" => await _userManager.GetUsersInRoleAsync("Writer"),
                "Admins" => await _userManager.GetUsersInRoleAsync("Administrator"),
                _ => await _userManager.Users.Where(u => u.IsActive && !u.IsDeleted).ToListAsync()
            };

            foreach (var user in users)
            {
                var notification = new Notification
                {
                    UserId = user.Id,
                    Title = title,
                    Message = message,
                    NotificationType = NotificationType.SystemAlert,
                    Priority = priority,
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow,
                    RelatedEntityType = "Broadcast"
                };

                _context.Notifications.Add(notification);
            }

            // Audit log
            _context.AuditLogs.Add(new AuditLog
            {
                Action = "Notification Broadcast",
                PerformedById = broadcastById,
                Description = $"Broadcast '{title}' sent to {users.Count} users (Priority: {priority})",
                CreatedDate = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
        }
    }
}
