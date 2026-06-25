using System.ComponentModel.DataAnnotations;
using ScholarRescue.Models.Enums;

namespace ScholarRescue.Models
{
    /// <summary>
    /// A content item in the Writer Knowledge Center.
    /// Supports FAQ entries, guide articles, checklists, and citation references.
    /// </summary>
    public class WriterResource
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// For FAQ: the answer body. For guides: the article body. Supports HTML.
        /// </summary>
        [Required]
        public string Content { get; set; } = string.Empty;

        public WriterResourceCategory Category { get; set; }

        /// <summary>
        /// Sub-category label, e.g. "Platform Policies" under WriterRules,
        /// "APA 7" under CitationGuides, "Essay Writing" under WritingGuide.
        /// </summary>
        [MaxLength(100)]
        public string? SubCategory { get; set; }

        /// <summary>
        /// Comma-separated tags for search indexing.
        /// </summary>
        [MaxLength(500)]
        public string? Tags { get; set; }

        public int SortOrder { get; set; }

        public bool IsActive { get; set; } = true;

        /// <summary>
        /// For FAQ entries: stores the question text for searchability.
        /// </summary>
        [MaxLength(500)]
        public string? Question { get; set; }

        /// <summary>
        /// The ID of the admin who last created or updated this resource.
        /// </summary>
        [MaxLength(450)]
        public string? AuthorId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}