using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using ScholarRescue.Hubs;
using ScholarRescue.Models;
using ScholarRescue.Models.Enums;
using ScholarRescue.Models.Security;
using ScholarRescue.Services;
using ScholarRescue.ViewModels.Communication;
using ScholarRescue.ViewModels.Notification;

namespace ScholarRescue.Controllers
{
    /// <summary>
    /// Centralized Communication Hub with tabs for Messages, Notifications, Support Tickets, and Announcements.
    /// </summary>
    [Authorize]
    [Route("[controller]")]
    public class CommunicationController : Controller
    {
        private readonly INotificationService _notificationService;
        private readonly IAnnouncementService _announcementService;
        private readonly IMessageService _messageService;
        private readonly ISupportTicketService _supportTicketService;
        private readonly IHubContext<CommunicationHub> _hubContext;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<CommunicationController> _logger;

        public CommunicationController(
            INotificationService notificationService,
            IAnnouncementService announcementService,
            IMessageService messageService,
            ISupportTicketService supportTicketService,
            IHubContext<CommunicationHub> hubContext,
            UserManager<ApplicationUser> userManager,
            ILogger<CommunicationController> logger)
        {
            _notificationService = notificationService;
            _announcementService = announcementService;
            _messageService = messageService;
            _supportTicketService = supportTicketService;
            _hubContext = hubContext;
            _userManager = userManager;
            _logger = logger;
        }

        /// <summary>
        /// GET: /Communication
        /// Main communication hub with tabbed interface.
        /// </summary>
        [HttpGet("")]
        public async Task<IActionResult> Index(string? tab = "messages")
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null) return Challenge();

                var roles = await _userManager.GetRolesAsync(currentUser);
                var role = roles.FirstOrDefault() ?? "Client";

                var unreadMessages = await _messageService.GetUnreadConversationCountAsync(currentUser.Id);
                var unreadNotifications = await _notificationService.GetUnreadCountAsync(currentUser.Id);
                var unreadAnnouncements = await _announcementService.GetUnreadAnnouncementCountAsync(currentUser.Id, role);
                var openTickets = await _supportTicketService.GetOpenTicketCountAsync(currentUser.Id);

                var viewModel = new CommunicationHubViewModel
                {
                    ActiveTab = tab ?? "messages",
                    UnreadMessageCount = unreadMessages,
                    UnreadNotificationCount = unreadNotifications,
                    UnreadAnnouncementCount = unreadAnnouncements,
                    OpenTicketCount = openTickets
                };

                ViewBag.UserRole = role;
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading communication hub.");
                TempData["ErrorMessage"] = "An error occurred.";
                return View(new CommunicationHubViewModel());
            }
        }

        /// <summary>
        /// GET: /Communication/Announcements
        /// Returns announcements for the current user (AJAX).
        /// </summary>
        [HttpGet("Announcements")]
        public async Task<IActionResult> GetAnnouncements()
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null) return Unauthorized();

                var roles = await _userManager.GetRolesAsync(currentUser);
                var role = roles.FirstOrDefault() ?? RoleNames.Client;

                var announcements = await _announcementService.GetActiveAnnouncementsForUserAsync(currentUser.Id, role);

                var results = announcements.Select(a => new
                {
                    id = a.Id,
                    title = a.Title,
                    content = a.Content,
                    priority = a.Priority.ToString(),
                    priorityColor = GetPriorityColor(a.Priority),
                    broadcastType = a.BroadcastType,
                    createdBy = a.CreatedBy != null ? $"{a.CreatedBy.FirstName} {a.CreatedBy.LastName}" : "System",
                    createdDate = a.CreatedAt.ToString("MMM dd, yyyy HH:mm"),
                    timeAgo = GetTimeAgo(a.CreatedAt)
                });

                return Ok(new { success = true, announcements = results });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading announcements.");
                return Ok(new { success = false, announcements = Array.Empty<object>() });
            }
        }

        /// <summary>
        /// POST: /Communication/MarkAnnouncementRead/{id}
        /// Marks an announcement as read.
        /// </summary>
        [HttpPost("MarkAnnouncementRead/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAnnouncementRead(int id)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null) return Unauthorized();

                await _announcementService.MarkAnnouncementReadAsync(id, currentUser.Id);
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking announcement as read.");
                return StatusCode(500, new { success = false });
            }
        }

        /// <summary>
        /// GET: /Communication/BadgeCounts
        /// Returns badge counts for the communication hub.
        /// </summary>
        [HttpGet("BadgeCounts")]
        public async Task<IActionResult> BadgeCounts()
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null) return Unauthorized();

                var roles = await _userManager.GetRolesAsync(currentUser);
                var role = roles.FirstOrDefault() ?? RoleNames.Client;

                var unreadMessages = await _messageService.GetUnreadConversationCountAsync(currentUser.Id);
                var unreadNotifications = await _notificationService.GetUnreadCountAsync(currentUser.Id);
                var unreadAnnouncements = await _announcementService.GetUnreadAnnouncementCountAsync(currentUser.Id, role);
                var openTickets = await _supportTicketService.GetOpenTicketCountAsync(currentUser.Id);

                return Ok(new
                {
                    success = true,
                    unreadMessages,
                    unreadNotifications,
                    unreadAnnouncements,
                    openTickets
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting badge counts.");
                return Ok(new { success = false });
            }
        }

        #region Helpers

        private static string GetPriorityColor(NotificationPriority priority) => priority switch
        {
            NotificationPriority.Low => "secondary",
            NotificationPriority.Normal => "primary",
            NotificationPriority.High => "warning",
            NotificationPriority.Critical => "danger",
            _ => "primary"
        };

        private static string GetTimeAgo(DateTime utc)
        {
            var diff = DateTime.UtcNow - utc;
            if (diff.TotalMinutes < 1) return "Just now";
            if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes}m ago";
            if (diff.TotalHours < 24) return $"{(int)diff.TotalHours}h ago";
            if (diff.TotalDays < 7) return $"{(int)diff.TotalDays}d ago";
            return utc.ToLocalTime().ToString("MMM dd");
        }

        #endregion
    }

}
