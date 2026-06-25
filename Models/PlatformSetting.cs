using System.ComponentModel.DataAnnotations;

namespace ScholarRescue.Models
{
    /// <summary>
    /// Centralized platform configuration setting.
    /// All business rules, thresholds, and operational settings stored here.
    /// No hardcoded values needed after implementation.
    /// </summary>
    public class PlatformSetting
    {
        [Key]
        public int Id { get; set; }

        /// <summary>Setting category (General, Financial, Marketplace, Writer, Client, Escrow, Risk, Moderation, Communication, Security, Email, System).</summary>
        [Required]
        [MaxLength(50)]
        public string Category { get; set; } = string.Empty;

        /// <summary>Unique setting key for programmatic lookup.</summary>
        [Required]
        [MaxLength(100)]
        public string Key { get; set; } = string.Empty;

        /// <summary>Setting value stored as string.</summary>
        [Required]
        public string Value { get; set; } = string.Empty;

        /// <summary>Data type: String, Integer, Decimal, Boolean, JSON, DateTime.</summary>
        [Required]
        [MaxLength(20)]
        public string DataType { get; set; } = "String";

        /// <summary>Human-readable description of what this setting controls.</summary>
        [MaxLength(500)]
        public string? Description { get; set; }

        /// <summary>Whether this setting can be edited from the admin panel.</summary>
        public bool IsEditable { get; set; } = true;

        /// <summary>Admin who last updated this setting.</summary>
        [MaxLength(100)]
        public string? UpdatedById { get; set; }

        /// <summary>When the setting was last updated.</summary>
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>Validation rules as JSON (optional).</summary>
        public string? ValidationRules { get; set; }
    }
}