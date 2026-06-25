using ScholarRescue.Models;
using ScholarRescue.Models.Enums;

namespace ScholarRescue.Services
{
    /// <summary>
    /// Service for managing system announcements, broadcasts, and read tracking.
    /// </summary>
    public interface IAnnouncementService
    {
        // --- Admin Operations ---

        /// <summary>Creates a new announcement and sends notifications to targeted users.</summary>
        Task<SystemAnnouncement> CreateAnnouncementAsync(string createdById, string title, string content,
            NotificationPriority priority, TargetAudience targetAudience,
            DateTime? expiresAt, string? broadcastType);

        /// <summary>Updates an existing announcement.</summary>
        Task<bool> UpdateAnnouncementAsync(int announcementId, string title, string content,
            NotificationPriority priority, TargetAudience targetAudience,
            DateTime? expiresAt, bool isActive, string? broadcastType);

        /// <summary>Deletes an announcement.</summary>
        Task<bool> DeleteAnnouncementAsync(int announcementId);

        /// <summary>Gets all announcements with pagination (admin).</summary>
        Task<(List<SystemAnnouncement> Announcements, int TotalCount)> GetAllAnnouncementsAsync(
            int page = 1, int pageSize = 25, string? search = null,
            TargetAudience? audience = null, bool? isActive = null);

        /// <summary>Gets broadcast history for admin monitoring.</summary>
        Task<List<SystemAnnouncement>> GetBroadcastHistoryAsync(int take = 50);

        // --- User Operations ---

        /// <summary>Gets active announcements for a specific user based on their role.</summary>
        Task<List<SystemAnnouncement>> GetActiveAnnouncementsForUserAsync(string userId, string userRole);

        /// <summary>Marks an announcement as read by a user.</summary>
        Task MarkAnnouncementReadAsync(int announcementId, string userId);

        /// <summary>Checks if a user has read an announcement.</summary>
        Task<bool> HasUserReadAnnouncementAsync(int announcementId, string userId);

        /// <summary>Gets the count of unread announcements for a user.</summary>
        Task<int> GetUnreadAnnouncementCountAsync(string userId, string userRole);

        /// <summary>Gets a single announcement by ID.</summary>
        Task<SystemAnnouncement?> GetAnnouncementByIdAsync(int announcementId);

        /// <summary>Broadcasts a message as a notification to all targeted users.</summary>
        Task BroadcastMessageAsync(string createdById, string title, string content,
            NotificationPriority priority, TargetAudience targetAudience, string? broadcastType);
    }
}