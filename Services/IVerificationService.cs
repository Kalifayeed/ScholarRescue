namespace ScholarRescue.Services
{
    /// <summary>
    /// Service for email verification, used primarily for writer verification workflow.
    /// </summary>
    public interface IVerificationService
    {
        /// <summary>Generate a verification token and send verification email.</summary>
        Task SendVerificationEmailAsync(string userId, string email, string fullName);

        /// <summary>Verify a user's email using a token.</summary>
        Task<(bool Success, string Message)> VerifyEmailAsync(string userId, string token);

        /// <summary>Generate a new token and resend verification email.</summary>
        Task ResendVerificationAsync(string userId);

        /// <summary>Check if a verification token is still valid (not expired).</summary>
        bool IsTokenValid(string token);

        /// <summary>Generate a welcome email for new clients registered through order flow.</summary>
        Task SendClientWelcomeEmailAsync(string email, string fullName);
    }
}