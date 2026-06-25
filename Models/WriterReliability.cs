using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ScholarRescue.Models
{
    /// <summary>
    /// Tracks a writer's reliability score, penalties, and performance history.
    /// Initial score: 100. Deductions/bonuses applied via the reliability engine.
    /// </summary>
    public class WriterReliability
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string WriterId { get; set; } = string.Empty;
        [ForeignKey(nameof(WriterId))]
        public virtual ApplicationUser Writer { get; set; } = null!;

        /// <summary>Current reliability score (0-100). Initial: 100.</summary>
        public int ReliabilityScore { get; set; } = 100;

        public int CompletedOrders { get; set; }
        public int CancelledOrders { get; set; }
        public int LateOrders { get; set; }
        public int RevisionRequests { get; set; }
        public int ClientComplaints { get; set; }
        public int Warnings { get; set; }
        public int Suspensions { get; set; }
        public int ConsecutiveOnTimeOrders { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}