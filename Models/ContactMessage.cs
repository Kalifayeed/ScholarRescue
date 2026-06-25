using System.ComponentModel.DataAnnotations;

namespace ScholarRescue.Models
{
    /// <summary>
    /// Stores messages submitted through the public contact form.
    /// </summary>
    public class ContactMessage
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [MaxLength(200)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string Subject { get; set; } = string.Empty;

        [Required]
        public string Message { get; set; } = string.Empty;

        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

        public bool IsRead { get; set; } = false;

        public string? AdminResponse { get; set; }

        public DateTime? RespondedAt { get; set; }
    }
}