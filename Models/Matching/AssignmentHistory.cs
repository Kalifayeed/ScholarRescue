using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ScholarRescue.Models.Matching
{
    /// <summary>
    /// Tracks assignment outcomes for machine learning preparation (Section 18).
    /// Stores recommended writers, assigned writer, and outcome data for future analysis.
    /// </summary>
    public class AssignmentHistory
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int OrderId { get; set; }
        [ForeignKey(nameof(OrderId))]
        public virtual TutoringRequest Order { get; set; } = null!;

        /// <summary>JSON-serialized list of writer IDs that were recommended.</summary>
        [Required, MaxLength(2000)]
        public string RecommendedWriterIds { get; set; } = "[]";

        /// <summary>The writer who was ultimately assigned.</summary>
        [MaxLength(450)]
        public string? AssignedWriterId { get; set; }

        /// <summary>Whether the assignment was automatic or manual.</summary>
        public bool WasAutoAssigned { get; set; }

        /// <summary>Match percentage of the assigned writer at time of assignment.</summary>
        public double AssignedWriterMatchScore { get; set; }

        /// <summary>Whether the order was completed successfully.</summary>
        public bool? WasCompletedSuccessfully { get; set; }

        /// <summary>Final client rating for the writer (1-5).</summary>
        public double? ClientRating { get; set; }

        /// <summary>Whether the order was delivered on time.</summary>
        public bool? WasOnTime { get; set; }

        /// <summary>Number of revision requests for this order.</summary>
        public int RevisionCount { get; set; }

        /// <summary>Whether a dispute was raised.</summary>
        public bool HadDispute { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }
    }
}