using Microsoft.EntityFrameworkCore;
using ScholarRescue.Data;
using ScholarRescue.Models;
using ScholarRescue.Models.Enums;

namespace ScholarRescue.Services
{
    /// <summary>
    /// Email service implementation for development.
    /// Logs email contents to application logs and stores delivery status.
    /// Replace with SMTP/SendGrid/Mailgun for production - no consumer refactoring needed.
    /// </summary>
    public class EmailService : IEmailService
    {
        private readonly ILogger<EmailService> _logger;
        private readonly ScholarRescueDbContext _context;

        public EmailService(ILogger<EmailService> logger, ScholarRescueDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        /// <summary>
        /// Sends an email. In development, logs to application logs.
        /// In production, integrate with SMTP/SendGrid/Mailgun here.
        /// </summary>
        public async Task SendEmailAsync(string email, string subject, string body)
        {
            // STUB: Replace with actual SMTP/SendGrid/Mailgun in production
            _logger.LogInformation(
                "[EMAIL STUB] To: {Email}, Subject: {Subject}, Body: {Body}",
                email, subject, body);

            await Task.CompletedTask;
        }

        /// <summary>
        /// Creates a notification record and sends email notification.
        /// Stores email delivery status in the Notification record.
        /// </summary>
        public async Task<Notification> CreateAndSendNotificationAsync(
            string userId,
            string email,
            string title,
            string message,
            NotificationType notificationType,
            string? relatedEntityId = null,
            string? relatedEntityType = null)
        {
            // Create the notification record
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

            // Send email (stub in dev, real SMTP in production)
            try
            {
                await SendEmailAsync(email, title, message);
                notification.EmailSent = true;
                notification.EmailSentAt = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send email notification to {Email} for {Title}", email, title);
                notification.EmailSent = false;
            }

            await _context.SaveChangesAsync();

            // Create audit log entry
            var auditLog = new AuditLog
            {
                Action = "Notification Created",
                PerformedById = userId,
                TargetUserId = userId,
                Description = $"Notification '{title}' ({notificationType}) created for user {userId}",
                CreatedDate = DateTime.UtcNow
            };
            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();

            return notification;
        }

        public Task<bool> TestSmtpConnectionAsync()
        {
            try
            {
                // In development mode, SMTP is not configured - return true
                // In production, this would attempt an actual SMTP connection
                _logger.LogInformation("SMTP connection test requested (development mode)");
                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SMTP connection test failed");
                return Task.FromResult(false);
            }
        }
    }
}
