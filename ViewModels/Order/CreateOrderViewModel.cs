using System.ComponentModel.DataAnnotations;
using ScholarRescue.Models.Enums;

namespace ScholarRescue.ViewModels.Order
{
    /// <summary>
    /// ViewModel for creating a new order.
    /// Pricing calculated automatically. Files uploaded via drag-and-drop.
    /// Subject selected from predefined dropdown with "Other" option.
    /// </summary>
    public class CreateOrderViewModel
    {
        [Required(ErrorMessage = "Title is required.")]
        [MaxLength(500, ErrorMessage = "Title cannot exceed 500 characters.")]
        [Display(Name = "Title")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Description is required.")]
        [Display(Name = "Description")]
        [DataType(DataType.MultilineText)]
        public string Description { get; set; } = string.Empty;

        /// <summary>Selected subject from dropdown. If "Other", use OtherSubject.</summary>
        [Required(ErrorMessage = "Subject is required.")]
        [Display(Name = "Subject")]
        public string Subject { get; set; } = string.Empty;

        /// <summary>Custom subject when "Other" is selected.</summary>
        [Display(Name = "Specify Subject")]
        [MaxLength(200)]
        public string? OtherSubject { get; set; }

        [Required(ErrorMessage = "Academic level is required.")]
        [Display(Name = "Academic Level")]
        public AcademicLevel AcademicLevel { get; set; }

        [Required(ErrorMessage = "Citation format is required.")]
        [Display(Name = "Citation Format")]
        public CitationFormat CitationFormat { get; set; } = CitationFormat.APA_7th;

        [Required(ErrorMessage = "Deadline is required.")]
        [Display(Name = "Deadline")]
        [DataType(DataType.DateTime)]
        [FutureDate(ErrorMessage = "Deadline must be a future date.")]
        public DateTime Deadline { get; set; } = DateTime.UtcNow.AddDays(3);

        [Required(ErrorMessage = "Number of pages is required.")]
        [Range(1, 1000, ErrorMessage = "Pages must be between 1 and 1000.")]
        [Display(Name = "Number of Pages")]
        public int Pages { get; set; } = 1;

        [Range(0, 100)]
        [Display(Name = "Number of Sources")]
        public int NumberOfSources { get; set; }

        /// <summary>Comma-separated list of uploaded file identifiers.</summary>
        public string? UploadedFiles { get; set; }
    }

    /// <summary>
    /// Custom validation attribute to ensure the deadline is a future date.
    /// </summary>
    public class FutureDateAttribute : ValidationAttribute
    {
        public override bool IsValid(object? value)
        {
            if (value is DateTime dateTime)
            {
                return dateTime > DateTime.UtcNow;
            }
            return false;
        }
    }

    /// <summary>
    /// Predefined academic subjects for the dropdown.
    /// </summary>
    public static class AcademicSubjects
    {
        public static readonly string[] Subjects = new[]
        {
            "Accounting", "Architecture", "Biology", "Business", "Chemistry",
            "Computer Science", "Criminology", "Economics", "Education", "Engineering",
            "English", "Environmental Science", "Finance", "Geography", "Health Sciences",
            "History", "Information Technology", "Law", "Literature", "Management",
            "Marketing", "Mathematics", "Medicine", "Nursing", "Philosophy",
            "Physics", "Political Science", "Psychology", "Public Health", "Religion",
            "Sociology", "Statistics", "Other"
        };
    }
}