using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using ScholarRescue.Data;
using ScholarRescue.Models;
using ScholarRescue.Models.Enums;
using ScholarRescue.Models.Security;

namespace ScholarRescue.Services
{
    /// <summary>
    /// Implementation of the announcement service with CRUD, broadcasting, and read tracking.
    /// </summary>
    public class AnnouncementService : IAnnouncementService
    {
        private readonly ScholarRescueDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<AnnouncementService> _logger;

        public AnnouncementService(
            ScholarRescueDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<AnnouncementService> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<SystemAnnouncement> CreateAnnouncementAsync(string createdById, string title, string content,
            NotificationPriority priority, TargetAudience targetAudience,
            DateTime? expiresAt, string? broadcastType)
        {
            var announcement = new SystemAnnouncement
            {
                Title = title,
                Content = content,
                CreatedById = createdById,
                Priority = priority,
                TargetAudience = targetAudience,
                ExpiresAt = expiresAt,
                BroadcastType = broadcastType,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.SystemAnnouncements.Add(announcement);
            await _context.SaveChangesAsync();

            // Audit log
            _context.AuditLogs.Add(new AuditLog
            {
                Action = "Announcement Published",
                PerformedById = createdById,
                Description = $"Announcement '{title}' published to {targetAudience} (Priority: {priority})",
                CreatedDate = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();

            return announcement;
        }

        public async Task<bool> UpdateAnnouncementAsync(int announcementId, string title, string content,
            NotificationPriority priority, TargetAudience targetAudience,
            DateTime? expiresAt, bool isActive, string? broadcastType)
        {
            var announcement = await _context.SystemAnnouncements.FindAsync(announcementId);
            if (announcement == null) return false;

            announcement.Title = title;
            announcement.Content = content;
            announcement.Priority = priority;
            announcement.TargetAudience = targetAudience;
            announcement.ExpiresAt = expiresAt;
            announcement.IsActive = isActive;
            announcement.BroadcastType = broadcastType;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAnnouncementAsync(int announcementId)
        {
            var announcement = await _context.SystemAnnouncements.FindAsync(announcementId);
            if (announcement == null) return false;

            _context.SystemAnnouncements.Remove(announcement);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<(List<SystemAnnouncement> Announcements, int TotalCount)> GetAllAnnouncementsAsync(
            int page = 1, int pageSize = 25, string? search = null,
            TargetAudience? audience = null, bool? isActive = null)
        {
            IQueryable<SystemAnnouncement> query = _context.SystemAnnouncements
                .Include(a => a.CreatedBy);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLowerInvariant();
                query = query.Where(a =>
                    a.Title.ToLower().Contains(term) ||
                    a.Content.ToLower().Contains(term));
            }

            if (audience.HasValue)
                query = query.Where(a => a.TargetAudience == audience.Value);

            if (isActive.HasValue)
                query = query.Where(a => a.IsActive == isActive.Value);

            var totalCount = await query.CountAsync();

            var announcements = await query
                .OrderByDescending(a => a.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (announcements, totalCount);
        }

        public async Task<List<SystemAnnouncement>> GetBroadcastHistoryAsync(int take = 50)
        {
            return await _context.SystemAnnouncements
                .Include(a => a.CreatedBy)
                .OrderByDescending(a => a.CreatedAt)
                .Take(take)
                .ToListAsync();
        }

        public async Task<List<SystemAnnouncement>> GetActiveAnnouncementsForUserAsync(string userId, string userRole)
        {
            var now = DateTime.UtcNow;
            IQueryable<SystemAnnouncement> query = _context.SystemAnnouncements
                .Where(a => a.IsActive && (!a.ExpiresAt.HasValue || a.ExpiresAt > now));

            // Filter by target audience
            query = userRole switch
            {
                "Administrator" => query.Where(a => a.TargetAudience == TargetAudience.AllUsers || a.TargetAudience == TargetAudience.Admins),
                "Writer" => query.Where(a => a.TargetAudience == TargetAudience.AllUsers || a.TargetAudience == TargetAudience.Writers),
                "Client" => query.Where(a => a.TargetAudience == TargetAudience.AllUsers || a.TargetAudience == TargetAudience.Clients),
                _ => query.Where(a => a.TargetAudience == TargetAudience.AllUsers)
            };

            return await query.OrderByDescending(a => a.CreatedAt).ToListAsync();
        }

        public async Task MarkAnnouncementReadAsync(int announcementId, string userId)
        {
            var exists = await _context.AnnouncementReads
                .AnyAsync(r => r.AnnouncementId == announcementId && r.UserId == userId);

            if (!exists)
            {
                _context.AnnouncementReads.Add(new AnnouncementRead
                {
                    AnnouncementId = announcementId,
                    UserId = userId,
                    ReadAt = DateTime.UtcNow
                });
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> HasUserReadAnnouncementAsync(int announcementId, string userId)
        {
            return await _context.AnnouncementReads
                .AnyAsync(r => r.AnnouncementId == announcementId && r.UserId == userId);
        }

        public async Task<int> GetUnreadAnnouncementCountAsync(string userId, string userRole)
        {
            var activeQuery = _context.SystemAnnouncements
                .Where(a => a.IsActive && !a.IsExpired);

            activeQuery = userRole switch
            {
                "Administrator" => activeQuery.Where(a => a.TargetAudience == TargetAudience.AllUsers || a.TargetAudience == TargetAudience.Admins),
                "Writer" => activeQuery.Where(a => a.TargetAudience == TargetAudience.AllUsers || a.TargetAudience == TargetAudience.Writers),
                "Client" => activeQuery.Where(a => a.TargetAudience == TargetAudience.AllUsers || a.TargetAudience == TargetAudience.Clients),
                _ => activeQuery.Where(a => a.TargetAudience == TargetAudience.AllUsers)
            };

            var activeIds = await activeQuery.Select(a => a.Id).ToListAsync();
            var readIds = await _context.AnnouncementReads
                .Where(r => r.UserId == userId && activeIds.Contains(r.AnnouncementId))
                .Select(r => r.AnnouncementId)
                .ToListAsync();

            return activeIds.Count(id => !readIds.Contains(id));
        }

        public async Task<SystemAnnouncement?> GetAnnouncementByIdAsync(int announcementId)
        {
            return await _context.SystemAnnouncements
                .Include(a => a.CreatedBy)
                .FirstOrDefaultAsync(a => a.Id == announcementId);
        }

        public async Task BroadcastMessageAsync(string createdById, string title, string content,
            NotificationPriority priority, TargetAudience targetAudience, string? broadcastType)
        {
            // Create the announcement
            var announcement = await CreateAnnouncementAsync(createdById, title, content,
                priority, targetAudience, null, broadcastType);

            // Get target users
            var users = await GetTargetUsersAsync(targetAudience);

            // Create notifications for each target user
            foreach (var user in users)
            {
                var notification = new Notification
                {
                    UserId = user.Id,
                    Title = $"📢 {title}",
                    Message = content,
                    NotificationType = NotificationType.SystemAlert,
                    Priority = priority,
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow,
                    RelatedEntityId = announcement.Id.ToString(),
                    RelatedEntityType = "Announcement"
                };

                _context.Notifications.Add(notification);
            }

            // Audit log
            _context.AuditLogs.Add(new AuditLog
            {
                Action = "Broadcast Sent",
                PerformedById = createdById,
                Description = $"Broadcast '{title}' sent to {users.Count} users ({targetAudience})",
                CreatedDate = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
        }

        private async Task<List<ApplicationUser>> GetTargetUsersAsync(TargetAudience targetAudience)
        {
            return targetAudience switch
            {
                TargetAudience.AllUsers => await _userManager.Users
                    .Where(u => u.IsActive && !u.IsDeleted).ToListAsync(),
                TargetAudience.Clients => (await _userManager.GetUsersInRoleAsync(RoleNames.Client)).ToList(),
                TargetAudience.Writers => (await _userManager.GetUsersInRoleAsync(RoleNames.Writer)).ToList(),
                TargetAudience.Admins => (await _userManager.GetUsersInRoleAsync(RoleNames.Administrator)).ToList(),
                _ => await _userManager.Users.Where(u => u.IsActive && !u.IsDeleted).ToListAsync()
            };
        }
    }
}
