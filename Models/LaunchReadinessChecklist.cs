using System.ComponentModel.DataAnnotations;

namespace ScholarRescue.Models
{
    public class LaunchReadinessChecklist
    {
        public int Id { get; set; }

        [Required, MaxLength(200)]
        public string ItemName { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        public bool IsReady { get; set; }

        [MaxLength(100)]
        public string Category { get; set; } = string.Empty; // Database, Stripe, SMTP, SSL, Domain, Backup, Monitoring, Security, Audit, Escrow, Payout

        public DateTime? LastVerifiedAt { get; set; }

        [MaxLength(450)]
        public string? VerifiedById { get; set; }

        [MaxLength(1000)]
        public string? Notes { get; set; }

        public int SortOrder { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}