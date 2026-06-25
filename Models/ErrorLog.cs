using System.ComponentModel.DataAnnotations;

namespace ScholarRescue.Models
{
    public class ErrorLog
    {
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string Category { get; set; } = string.Empty; // Exception, FailedPayment, FailedEmail, FailedAssignment, FailedUpload

        [Required, MaxLength(500)]
        public string ErrorMessage { get; set; } = string.Empty;

        [MaxLength(8000)]
        public string? StackTrace { get; set; }

        [MaxLength(450)]
        public string? UserId { get; set; }

        [MaxLength(500)]
        public string? Url { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public bool IsResolved { get; set; }

        [MaxLength(450)]
        public string? ResolvedById { get; set; }

        public DateTime? ResolvedAt { get; set; }

        [MaxLength(1000)]
        public string? ResolutionNotes { get; set; }
    }
}