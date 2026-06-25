using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ScholarRescue.Models.Enums;

namespace ScholarRescue.Models
{
    /// <summary>
    /// Tracks risk profile for writers including violations and restrictions.
    /// </summary>
    public class WriterRiskProfile
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string WriterId { get; set; } = string.Empty;

        [ForeignKey(nameof(WriterId))]
        public virtual ApplicationUser Writer { get; set; } = null!;

        /// <summary>Current cumulative risk score.</summary>
        public int CurrentRiskScore { get; set; }

        /// <summary>Current risk level based on score.</summary>
        public RiskLevel RiskLevel { get; set; } = RiskLevel.Low;

        /// <summary>Number of violations recorded.</summary>
        public int ViolationCount { get; set; }

        /// <summary>Number of warnings issued.</summary>
        public int WarningCount { get; set; }

        /// <summary>Number of suspensions.</summary>
        public int SuspensionCount { get; set; }

        /// <summary>Whether messaging is restricted.</summary>
        public bool IsMessagingRestricted { get; set; }

        /// <summary>Whether the account is frozen pending review.</summary>
        public bool IsFrozen { get; set; }

        /// <summary>When the profile was last updated.</summary>
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }
}