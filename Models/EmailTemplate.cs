using System.ComponentModel.DataAnnotations;

namespace ScholarRescue.Models
{
    /// <summary>
    /// Stores email templates with subject and HTML content.
    /// Used by EmailTemplateService to render transactional emails.
    /// </summary>
    public class EmailTemplate
    {
        [Key]
        public int Id { get; set; }

        /// <summary>Unique key for programmatic lookup (e.g., "welcome_client", "order_assigned").</summary>
        [Required]
        [MaxLength(100)]
        public string TemplateKey { get; set; } = string.Empty;

        /// <summary>Email subject line (supports {placeholders}).</summary>
        [Required]
        [MaxLength(500)]
        public string Subject { get; set; } = string.Empty;

        /// <summary>HTML body content (supports {placeholders}).</summary>
        [Required]
        public string HtmlContent { get; set; } = string.Empty;

        /// <summary>When the template was created.</summary>
        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>When the template was last updated.</summary>
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}