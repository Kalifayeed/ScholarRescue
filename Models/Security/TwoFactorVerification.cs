using System.ComponentModel.DataAnnotations;

namespace ScholarRescue.Models.Security
{
    public class TwoFactorVerification
    {
        public int Id { get; set; }

        [Required, MaxLength(450)]
        public string UserId { get; set; } = string.Empty;

        [Required, MaxLength(10)]
        public string OtpCode { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddMinutes(5);

        public DateTime? VerifiedAt { get; set; }

        public int AttemptCount { get; set; }

        public bool IsUsed { get; set; }

        [MaxLength(50)]
        public string Purpose { get; set; } = "Login"; // Login, SensitiveAction
    }
}