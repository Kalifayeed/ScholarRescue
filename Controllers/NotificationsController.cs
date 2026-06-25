using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using ScholarRescue.Hubs;
using ScholarRescue.Models;
using ScholarRescue.Services;
using ScholarRescue.ViewModels.Notification;

namespace ScholarRescue.Controllers
{
    /// <summary>
    /// Controller for notification center UI, real-time alerts, and preferences.
    /// </summary>
    [Authorize]
    [Route("[controller]")]
    public class NotificationsController : Controller
    {
        private readonly INotificationService _notificationService;
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<NotificationsController> _logger;

        public NotificationsController(
            INotificationService notificationService,
            IHubContext<NotificationHub> hubContext,
            UserManager<ApplicationUser> userManager,
            ILogger<NotificationsController> logger)
        {
            _notificationService = notificationService;
            _hubContext = hubContext;
            _userManager = userManager;
            _logger = logger;
        }

        // ----------------------------------------------------------------
        // NOTIFICATION CENTER INDEX
        // ----------------------------------------------------------------

        /// <summary>
        /// GET: /Notifications
        /// Displays the paginated notification center with filtering and search.
        /// </summary>
        [HttpGet("")]
        [HttpGet("Index")]
        public async Task<IActionResult> Index(string? filter, string? search, Models.Enums.NotificationCategory? category, int page = 1)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null) return Challenge();

                page = Math.Max(1, page);
                var pageSize = 20;

                var (notifications, totalCount) = await _notificationService.GetUserNotificationsAsync(
                    currentUser.Id, page, pageSize, filter, search, category);

                var unreadCount = await _notificationService.GetUnreadCountAsync(currentUser.Id);

                var viewModel = new NotificationIndexViewModel
                {
                    Notifications = notifications.Select(n => BuildViewModel(n, currentUser.Id)).ToList(),
                    TotalCount = totalCount,
                    UnreadCount = unreadCount,
                    CurrentPage = page,
                    PageSize = pageSize,
                    SearchTerm = search,
                    Filter = filter,
                    CategoryFilter = category
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading notifications.");
                TempData["ErrorMessage"] = "An error occurred while loading notifications.";
                return View(new NotificationIndexViewModel());
            }
        }

        // ----------------------------------------------------------------
        // NOTIFICATION DETAILS
        // ----------------------------------------------------------------

        /// <summary>
        /// GET: /Notifications/Details/{id}
        /// Shows a single notification and marks it as read.
        /// </summary>
        [HttpGet("Details/{id:int}")]
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null) return Challenge();

                var notification = await _notificationService.GetNotificationByIdAsync(id, currentUser.Id);
                if (notification == null) return NotFound();

                // Mark as read
                await _notificationService.MarkAsReadAsync(id, currentUser.Id);

                var viewModel = BuildViewModel(notification, currentUser.Id);

                // Broadcast updated unread count
                var unreadCount = await _notificationService.GetUnreadCountAsync(currentUser.Id);
                await _hubContext.Clients.Group($"notifications_{currentUser.Id}")
                    .SendAsync("UnreadCountUpdated", unreadCount);

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading notification {Id}.", id);
                TempData["ErrorMessage"] = "An error occurred.";
                return RedirectToAction(nameof(Index));
            }
        }

        // ----------------------------------------------------------------
        // API ENDPOINTS (AJAX)
        // ----------------------------------------------------------------

        /// <summary>
        /// GET: /Notifications/Recent
        /// Returns the latest notifications for the navbar dropdown.
        /// </summary>
        [HttpGet("Recent")]
        public async Task<IActionResult> Recent(int take = 10)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null) return Unauthorized();

                take = Math.Clamp(take, 1, 25);

                var notifications = await _notificationService.GetRecentNotificationsAsync(currentUser.Id, take);
                var unreadCount = await _notificationService.GetUnreadCountAsync(currentUser.Id);

                var result = notifications.Select(n => new
                {
                    id = n.Id,
                    title = n.Title,
                    message = n.Message.Length > 80 ? n.Message[..80] + "..." : n.Message,
                    type = n.NotificationType.ToString(),
                    category = NotificationService.GetNotificationCategory(n.NotificationType).ToString(),
                    isRead = n.IsRead,
                    createdDate = n.CreatedAt,
                    timeAgo = GetTimeAgo(n.CreatedAt),
                    relatedUrl = GetRelatedUrl(n)
                });

                return Ok(new { success = true, notifications = result, unreadCount });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading recent notifications.");
                return Ok(new { success = false, notifications = Array.Empty<object>(), unreadCount = 0 });
            }
        }

        /// <summary>
        /// GET: /Notifications/UnreadCount
        /// Returns the count of unread notifications for the navbar badge.
        /// </summary>
        [HttpGet("UnreadCount")]
        public async Task<IActionResult> UnreadCount()
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null) return Unauthorized();

                var count = await _notificationService.GetUnreadCountAsync(currentUser.Id);
                return Ok(new { success = true, count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting unread notification count.");
                return Ok(new { success = false, count = 0 });
            }
        }

        /// <summary>
        /// POST: /Notifications/MarkRead/{id}
        /// Marks a single notification as read.
        /// </summary>
        [HttpPost("MarkRead/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkRead(int id)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null) return Unauthorized();

                await _notificationService.MarkAsReadAsync(id, currentUser.Id);

                var unreadCount = await _notificationService.GetUnreadCountAsync(currentUser.Id);
                await _hubContext.Clients.Group($"notifications_{currentUser.Id}")
                    .SendAsync("UnreadCountUpdated", unreadCount);

                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking notification {Id} as read.", id);
                return StatusCode(500, new { success = false });
            }
        }

        /// <summary>
        /// POST: /Notifications/MarkAllRead
        /// Marks all unread notifications as read.
        /// </summary>
        [HttpPost("MarkAllRead")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAllRead()
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null) return Unauthorized();

                await _notificationService.MarkAllAsReadAsync(currentUser.Id);

                // Broadcast updated count
                await _hubContext.Clients.Group($"notifications_{currentUser.Id}")
                    .SendAsync("UnreadCountUpdated", 0);

                TempData["SuccessMessage"] = "All notifications marked as read.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking all notifications as read.");
                TempData["ErrorMessage"] = "An error occurred.";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// POST: /Notifications/Delete/{id}
        /// Deletes a notification.
        /// </summary>
        [HttpPost("Delete/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null) return Unauthorized();

                await _notificationService.DeleteNotificationAsync(id, currentUser.Id);

                var unreadCount = await _notificationService.GetUnreadCountAsync(currentUser.Id);
                await _hubContext.Clients.Group($"notifications_{currentUser.Id}")
                    .SendAsync("UnreadCountUpdated", unreadCount);

                TempData["SuccessMessage"] = "Notification deleted.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting notification {Id}.", id);
                TempData["ErrorMessage"] = "An error occurred.";
                return RedirectToAction(nameof(Index));
            }
        }

        // ----------------------------------------------------------------
        // SETTINGS
        // ----------------------------------------------------------------

        /// <summary>
        /// GET: /Notifications/Settings
        /// Displays the notification preferences page.
        /// </summary>
        [HttpGet("Settings")]
        public async Task<IActionResult> Settings()
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null) return Challenge();

                var settings = await _notificationService.GetSettingsAsync(currentUser.Id);

                var viewModel = new NotificationSettingsViewModel
                {
                    NotifyMessages = settings.NotifyMessages,
                    NotifyOrders = settings.NotifyOrders,
                    NotifyAssignments = settings.NotifyAssignments,
                    NotifyRevisions = settings.NotifyRevisions,
                    NotifySystemAlerts = settings.NotifySystemAlerts
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading notification settings.");
                TempData["ErrorMessage"] = "An error occurred.";
                return View(new NotificationSettingsViewModel());
            }
        }

        /// <summary>
        /// POST: /Notifications/Settings
        /// Saves notification preferences.
        /// </summary>
        [HttpPost("Settings")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Settings(NotificationSettingsViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null) return Challenge();

                await _notificationService.UpdateSettingsAsync(
                    currentUser.Id,
                    model.NotifyMessages,
                    model.NotifyOrders,
                    model.NotifyAssignments,
                    model.NotifyRevisions,
                    model.NotifySystemAlerts);

                TempData["SuccessMessage"] = "Notification preferences saved.";
                return RedirectToAction(nameof(Settings));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving notification settings.");
                TempData["ErrorMessage"] = "An error occurred while saving settings.";
                return View(model);
            }
        }

        // ----------------------------------------------------------------
        // HELPERS
        // ----------------------------------------------------------------

        private NotificationViewModel BuildViewModel(Models.Notification notification, string currentUserId)
        {
            return new NotificationViewModel
            {
                Id = notification.Id,
                UserId = notification.UserId,
                Title = notification.Title,
                Message = notification.Message,
                NotificationType = notification.NotificationType,
                Category = NotificationService.GetNotificationCategory(notification.NotificationType),
                IsRead = notification.IsRead,
                CreatedDate = notification.CreatedAt,
                RelatedEntityId = notification.RelatedEntityId,
                RelatedUrl = GetRelatedUrl(notification),
                TimeAgo = GetTimeAgo(notification.CreatedAt)
            };
        }

        private string? GetRelatedUrl(Models.Notification notification)
        {
            if (string.IsNullOrEmpty(notification.RelatedEntityId))
                return null;

            return notification.NotificationType switch
            {
                Models.Enums.NotificationType.NewMessage =>
                    Url.Action("Conversation", "Messages", new { id = int.Parse(notification.RelatedEntityId) }),
                Models.Enums.NotificationType.OrderAssigned or
                Models.Enums.NotificationType.RevisionRequested or
                Models.Enums.NotificationType.OrderSubmitted or
                Models.Enums.NotificationType.OrderCompleted =>
                    Url.Action("Details", "Orders", new { id = int.Parse(notification.RelatedEntityId) }),
                Models.Enums.NotificationType.WriterApproved =>
                    Url.Action("Dashboard", "Writers"),
                Models.Enums.NotificationType.SystemAlert => null,
                _ => null
            };
        }

        private static string GetTimeAgo(DateTime utc)
        {
            var diff = DateTime.UtcNow - utc;
            if (diff.TotalMinutes < 1) return "Just now";
            if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes}m ago";
            if (diff.TotalHours < 24) return $"{(int)diff.TotalHours}h ago";
            if (diff.TotalDays < 7) return $"{(int)diff.TotalDays}d ago";
            return utc.ToLocalTime().ToString("MMM dd");
        }
    }
}