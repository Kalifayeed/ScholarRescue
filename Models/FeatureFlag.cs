using System.ComponentModel.DataAnnotations;

namespace ScholarRescue.Models
{
    /// <summary>
    /// Feature flags for enabling/disabling platform features without redeployment.
    /// </summary>
    public class FeatureFlag
    {
        [Key]
        public int Id { get; set; }

        /// <summary>Unique feature name for programmatic lookup.</summary>
        [Required]
        [MaxLength(100)]
        public string FeatureName { get; set; } = string.Empty;

        /// <summary>Whether the feature is enabled.</summary>
        public bool Enabled { get; set; } = true;

        /// <summary>Human-readable description of the feature.</summary>
        [MaxLength(500)]
        public string? Description { get; set; }

        /// <summary>Admin who last updated this flag.</summary>
        [MaxLength(100)]
        public string? UpdatedById { get; set; }

        /// <summary>When the flag was last updated.</summary>
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}