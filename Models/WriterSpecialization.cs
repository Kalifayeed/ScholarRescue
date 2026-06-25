using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ScholarRescue.Models
{
    /// <summary>
    /// Writer's subject specialization with expertise level and experience.
    /// Each writer can have up to 5 specializations.
    /// </summary>
    public class WriterSpecialization
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string WriterId { get; set; } = string.Empty;
        [ForeignKey(nameof(WriterId))]
        public virtual ApplicationUser Writer { get; set; } = null!;

        /// <summary>Subject from the master subject list.</summary>
        [Required][MaxLength(100)]
        public string Subject { get; set; } = string.Empty;

        /// <summary>Beginner, Intermediate, Advanced, or Expert.</summary>
        [Required][MaxLength(50)]
        public string ExpertiseLevel { get; set; } = "Intermediate";

        /// <summary>Years of experience in this subject.</summary>
        [Range(0, 100)]
        public int YearsExperience { get; set; }
    }
}