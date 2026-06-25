using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ScholarRescue.Models.Enums;

namespace ScholarRescue.Models
{
    /// <summary>
    /// Represents a writer's application to join the platform.
    /// Contains qualifications, documents, and approval workflow state.
    /// </summary>
    public class WriterApplication
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "Applicant")]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey(nameof(UserId))]
        public virtual ApplicationUser User { get; set; } = null!;

        /// <summary>
        /// Applicant phone number (collected during application).
        /// </summary>
        [Required]
        [MaxLength(30)]
        [Phone]
        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; } = string.Empty;

        /// <summary>
        /// Highest academic qualification (e.g. PhD, Masters, Bachelors).
        /// </summary>
        [Required]
        [MaxLength(150)]
        [Display(Name = "Highest Academic Qualification")]
        public string HighestQualification { get; set; } = string.Empty;

        /// <summary>
        /// Specialization / subject areas the writer can handle.
        /// </summary>
        [Required]
        [MaxLength(500)]
        [Display(Name = "Specialization / Subject Areas")]
        public string Specialization { get; set; } = string.Empty;

        /// <summary>
        /// Short professional biography (150 - 500 words).
        /// </summary>
        [Required]
        [MaxLength(4000)]
        [Display(Name = "Professional Biography")]
        public string Biography { get; set; } = string.Empty;

        /// <summary>
        /// Education level (legacy field kept for backwards compatibility).
        /// </summary>
        [MaxLength(100)]
        [Display(Name = "Education Level")]
        public string? EducationLevel { get; set; }

        /// <summary>
        /// Institution name (legacy field).
        /// </summary>
        [MaxLength(300)]
        [Display(Name = "Institution")]
        public string? Institution { get; set; }

        /// <summary>
        /// Years of experience (legacy field).
        /// </summary>
        [Range(0, 100)]
        [Display(Name = "Years of Experience")]
        public int ExperienceYears { get; set; }

        /// <summary>
        /// Stored path of the uploaded CV.
        /// </summary>
        [MaxLength(1000)]
        [Display(Name = "CV")]
        public string? CvFilePath { get; set; }

        /// <summary>
        /// Stored path of the uploaded degree certificate.
        /// </summary>
        [MaxLength(1000)]
        [Display(Name = "Degree Certificate")]
        public string? DegreeFilePath { get; set; }

        /// <summary>
        /// Stored path of the uploaded writing sample.
        /// </summary>
        [MaxLength(1000)]
        [Display(Name = "Writing Sample")]
        public string? WritingSampleFilePath { get; set; }

        /// <summary>
        /// Legacy resume field (kept for backwards compatibility).
        /// </summary>
        [MaxLength(1000)]
        [Display(Name = "Resume")]
        public string? ResumePath { get; set; }

        /// <summary>
        /// Legacy certificate field (kept for backwards compatibility).
        /// </summary>
        [MaxLength(1000)]
        [Display(Name = "Certificate")]
        public string? CertificatePath { get; set; }

        [Required]
        public WriterApplicationStatus Status { get; set; } = WriterApplicationStatus.Pending;

        [Required]
        [Display(Name = "Submitted Date")]
        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Legacy SubmittedDate alias.
        /// </summary>
        public DateTime SubmittedDate
        {
            get => SubmittedAt;
            set => SubmittedAt = value;
        }

        [Display(Name = "Reviewed Date")]
        public DateTime? ReviewedAt { get; set; }

        /// <summary>
        /// Legacy ReviewedDate alias.
        /// </summary>
        public DateTime? ReviewedDate
        {
            get => ReviewedAt;
            set => ReviewedAt = value;
        }

        [Display(Name = "Reviewed By")]
        public string? ReviewedByAdminId { get; set; }

        /// <summary>
        /// Legacy ReviewedById alias.
        /// </summary>
        public string? ReviewedById
        {
            get => ReviewedByAdminId;
            set => ReviewedByAdminId = value;
        }

        [ForeignKey(nameof(ReviewedByAdminId))]
        public virtual ApplicationUser? ReviewedBy { get; set; }

        /// <summary>
        /// Admin feedback / internal notes.
        /// </summary>
        [MaxLength(2000)]
        [Display(Name = "Admin Comments")]
        public string? AdminComments { get; set; }

        /// <summary>
        /// Legacy ReviewNotes alias.
        /// </summary>
        public string? ReviewNotes
        {
            get => AdminComments;
            set => AdminComments = value;
        }

        /// <summary>
        /// Legacy Specializations alias (maps to Specialization for backwards compatibility).
        /// </summary>
        [NotMapped]
        public string? Specializations
        {
            get => Specialization;
            set => Specialization = value ?? string.Empty;
        }
    }
}
