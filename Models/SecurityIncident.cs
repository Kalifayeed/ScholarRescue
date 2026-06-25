using System.ComponentModel.DataAnnotations;

namespace ScholarRescue.Models
{
    /// <summary>
    /// Records security incidents for tracking and management.
    /// </summary>
    public class SecurityIncident
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [MaxLength(2000)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string Severity { get; set; } = "Medium"; // Critical, High, Medium, Low

        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = "Open"; // Open, Investigating, Resolved, Closed

        [MaxLength(100)]
        public string? AssignedToId { get; set; }

        [MaxLength(500)]
        public string? Resolution { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? ResolvedAt { get; set; }

        [MaxLength(100)]
        public string? Category { get; set; } // BruteForce, SuspiciousLogin, DataBreach, PolicyViolation
    }
}