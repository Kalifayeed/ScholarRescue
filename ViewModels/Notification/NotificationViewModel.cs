using ScholarRescue.Models.Enums;

namespace ScholarRescue.ViewModels.Notification
{
    /// <summary>
    /// View model for a single notification item.
    /// </summary>
    public class NotificationViewModel
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public NotificationType NotificationType { get; set; }
        public NotificationCategory Category { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreatedDate { get; set; }
        public string? RelatedEntityId { get; set; }
        public string? RelatedUrl { get; set; }
        public string TimeAgo { get; set; } = string.Empty;
    }

    /// <summary>
    /// View model for the notification center index page.
    /// </summary>
    public class NotificationIndexViewModel
    {
        public List<NotificationViewModel> Notifications { get; set; } = new();
        public int TotalCount { get; set; }
        public int UnreadCount { get; set; }
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
        public string? SearchTerm { get; set; }
        public string? Filter { get; set; } // "all", "unread", "read"
        public NotificationCategory? CategoryFilter { get; set; }
    }

    /// <summary>
    /// View model for the notification preference page.
    /// </summary>
    public class NotificationSettingsViewModel
    {
        public bool NotifyMessages { get; set; } = true;
        public bool NotifyOrders { get; set; } = true;
        public bool NotifyAssignments { get; set; } = true;
        public bool NotifyRevisions { get; set; } = true;
        public bool NotifySystemAlerts { get; set; } = true;
    }

    /// <summary>
    /// Lightweight DTO for navbar dropdown notifications.
    /// </summary>
    public class NotificationDropdownViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public NotificationType NotificationType { get; set; }
        public NotificationCategory Category { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreatedDate { get; set; }
        public string? RelatedUrl { get; set; }
        public string TimeAgo { get; set; } = string.Empty;
    }

    /// <summary>
    /// View model for the admin notification management page.
    /// </summary>
    public class AdminNotificationIndexViewModel
    {
        public List<NotificationViewModel> Notifications { get; set; } = new();
        public int TotalCount { get; set; }
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 25;
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
        public string? SearchTerm { get; set; }
        public string? Filter { get; set; }
        public NotificationCategory? CategoryFilter { get; set; }
        public string? UserNameFilter { get; set; }
    }
}