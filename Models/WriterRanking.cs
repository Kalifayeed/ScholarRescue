using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ScholarRescue.Models.Enums;

namespace ScholarRescue.Models
{
    /// <summary>
    /// Tracks writer performance metrics and rank progression. One record per writer.
    /// </summary>
    public class WriterRanking
    {
        public int Id { get; set; }

        [Required]
        public string WriterId { get; set; } = string.Empty;

        [ForeignKey(nameof(WriterId))]
        public virtual ApplicationUser? Writer { get; set; }

        /// <summary>Current rank (Beginner, Intermediate, Advanced, Expert, Elite).</summary>
        public WriterRank CurrentRank { get; set; } = WriterRank.Beginner;

        public bool IsOverridden { get; set; } = false;
        public string? OverrideAdminId { get; set; }
        public DateTime? OverriddenAt { get; set; }
        public string? OverrideNotes { get; set; }

        public int CompletedOrders { get; set; }
        public int TotalRating { get; set; }
        public int TotalRatings { get; set; }
        public int OrdersWithRevisions { get; set; }
        public int DisputedOrders { get; set; }
        public int OnTimeDeliveries { get; set; }

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [NotMapped] public double AverageRating => TotalRatings == 0 ? 0d : (double)TotalRating / TotalRatings;
        [NotMapped] public double RevisionRate => CompletedOrders == 0 ? 0d : (double)OrdersWithRevisions / CompletedOrders;
        [NotMapped] public double DisputeRate => CompletedOrders == 0 ? 0d : (double)DisputedOrders / CompletedOrders;
        [NotMapped] public double OnTimeDeliveryRate => CompletedOrders == 0 ? 0d : (double)OnTimeDeliveries / CompletedOrders;
    }
}