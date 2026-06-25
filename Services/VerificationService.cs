using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ScholarRescue.Data;
using ScholarRescue.Models;

namespace ScholarRescue.Services
{
    /// <summary>
    /// Handles email verification for writers and welcome emails for clients.
    /// Uses Identity token providers for token generation.
    /// </summary>
    public class VerificationService : IVerificationService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailService _emailService;
        private readonly ILogger<VerificationService> _logger;

        public VerificationService(
            UserManager<ApplicationUser> userManager,
            IEmailService emailService,
            ILogger<VerificationService> logger)
        {
            _userManager = userManager;
            _emailService = emailService;
            _logger = logger;
        }

        public async Task SendVerificationEmailAsync(string userId, string email, string fullName)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) throw new InvalidOperationException("User not found.");

            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var encodedToken = Uri.EscapeDataString(token);
            var verifyLink = $"https://scholarrescue.com/Account/VerifyEmail?userId={userId}&token={encodedToken}";

            var subject = "ScholarRescue - Verify Your Email Address";
            var body = $@"
<!DOCTYPE html>
<html>
<head><meta charset='utf-8'></head>
<body style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px;'>
    <div style='text-align: center; padding: 30px; background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); border-radius: 10px;'>
        <h1 style='color: white; margin: 0;'>ScholarRescue</h1>
    </div>
    <div style='padding: 30px; background: #f9f9f9; border-radius: 0 0 10px 10px;'>
        <h2>Welcome, {fullName}!</h2>
        <p>Thank you for registering with ScholarRescue. Please verify your email address by clicking the button below:</p>
        <div style='text-align: center; margin: 30px 0;'>
            <a href='{verifyLink}' style='background: #667eea; color: white; padding: 15px 30px; text-decoration: none; border-radius: 5px; font-size: 16px;'>Verify Email Address</a>
        </div>
        <p><strong>Note:</strong> This link will expire in 24 hours.</p>
        <p>If you did not create an account, please ignore this email.</p>
        <hr style='border: none; border-top: 1px solid #ddd;' />
        <p style='color: #888; font-size: 12px;'>ScholarRescue - Academic Support Platform</p>
    </div>
</body>
</html>";

            await _emailService.SendEmailAsync(email, subject, body);

            _logger.LogInformation("Verification email sent to {Email} for user {UserId}", email, userId);
        }

        public async Task<(bool Success, string Message)> VerifyEmailAsync(string userId, string token)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return (false, "User not found.");

            if (user.EmailConfirmed)
                return (false, "Email is already verified.");

            var result = await _userManager.ConfirmEmailAsync(user, token);
            if (!result.Succeeded)
                return (false, "Invalid or expired verification token.");

            user.EmailVerifiedAt = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

            _logger.LogInformation("Email verified for user {UserId}", userId);
            return (true, "Email verified successfully. You can now proceed.");
        }

        public async Task ResendVerificationAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) throw new InvalidOperationException("User not found.");
            if (user.EmailConfirmed) throw new InvalidOperationException("Email already verified.");

            await SendVerificationEmailAsync(userId, user.Email!, $"{user.FirstName} {user.LastName}");
        }

        public bool IsTokenValid(string token)
        {
            // Identity token validation is done by ConfirmEmailAsync
            return !string.IsNullOrWhiteSpace(token);
        }

        public async Task SendClientWelcomeEmailAsync(string email, string fullName)
        {
            var subject = "Welcome to ScholarRescue!";
            var body = $@"
<!DOCTYPE html>
<html>
<head><meta charset='utf-8'></head>
<body style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px;'>
    <div style='text-align: center; padding: 30px; background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); border-radius: 10px;'>
        <h1 style='color: white; margin: 0;'>ScholarRescue</h1>
    </div>
    <div style='padding: 30px; background: #f9f9f9; border-radius: 0 0 10px 10px;'>
        <h2>Welcome, {fullName}!</h2>
        <p>Your account has been created successfully. You can now:</p>
        <ul>
            <li><a href='https://scholarrescue.com/Orders/Dashboard'>View your orders</a></li>
            <li><a href='https://scholarrescue.com/Messages'>Access messages</a></li>
            <li><a href='https://scholarrescue.com/Notifications'>Check notifications</a></li>
        </ul>
        <p>Need help? Contact our support team.</p>
        <hr style='border: none; border-top: 1px solid #ddd;' />
        <p style='color: #888; font-size: 12px;'>ScholarRescue - Academic Support Platform</p>
    </div>
</body>
</html>";

            await _emailService.SendEmailAsync(email, subject, body);
            _logger.LogInformation("Welcome email sent to {Email}", email);
        }
    }
}