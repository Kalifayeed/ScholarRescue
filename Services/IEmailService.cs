using ScholarRescue.Models;

namespace ScholarRescue.Services
{
    /// <summary>
    /// Abstraction for email service supporting multiple providers.
    /// Architecture allows future SMTP/SendGrid/Mailgun integration without refactoring consumers.
    /// </summary>
    public interface IEmailService
    {
        /// <summary>
        /// Sends an email. For development, logs content to application logs.
        /// For production, replace implementation with SMTP/SendGrid.
        /// </summary>
        Task SendEmailAsync(string email, string subject, string body);

        /// <summary>
        /// Creates a notification + optionally sends an email for it.
        /// </summary>
        Task<Notification> CreateAndSendNotificationAsync(string userId, string email, string title, string message,
            Models.Enums.NotificationType notificationType, string? relatedEntityId = null, string? relatedEntityType = null);

        /// <summary>
        /// Tests the SMTP connection.
        /// </summary>
        Task<bool> TestSmtpConnectionAsync();
    }
}
