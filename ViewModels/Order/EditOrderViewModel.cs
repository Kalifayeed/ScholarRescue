using System.ComponentModel.DataAnnotations;
using ScholarRescue.Models.Enums;

namespace ScholarRescue.ViewModels.Order
{
    /// <summary>
    /// ViewModel for editing an existing order.
    /// Budget is read-only (auto-calculated). WordCount is read-only (Pages × 300).
    /// </summary>
    public class EditOrderViewModel
    {
        public int Id { get; set; }

        [Display(Name = "Order Number")]
        public string OrderNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Title is required.")]
        [MaxLength(500, ErrorMessage = "Title cannot exceed 500 characters.")]
        [Display(Name = "Title")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Description is required.")]
        [Display(Name = "Description")]
        [DataType(DataType.MultilineText)]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Subject is required.")]
        [MaxLength(200, ErrorMessage = "Subject cannot exceed 200 characters.")]
        [Display(Name = "Subject")]
        public string Subject { get; set; } = string.Empty;

        [Required(ErrorMessage = "Academic level is required.")]
        [Display(Name = "Academic Level")]
        public AcademicLevel AcademicLevel { get; set; }

        [Required(ErrorMessage = "Citation format is required.")]
        [Display(Name = "Citation Format")]
        public CitationFormat CitationFormat { get; set; } = CitationFormat.APA_7th;

        [Required(ErrorMessage = "Deadline is required.")]
        [Display(Name = "Deadline")]
        [DataType(DataType.DateTime)]
        public DateTime Deadline { get; set; }

        [Required(ErrorMessage = "Number of pages is required.")]
        [Range(1, 1000, ErrorMessage = "Pages must be between 1 and 1000.")]
        [Display(Name = "Number of Pages")]
        public int Pages { get; set; }

        /// <summary>Auto-calculated as Pages × 300. Read-only display.</summary>
        [Display(Name = "Word Count (auto)")]
        public int WordCount { get; set; }

        /// <summary>Auto-calculated price. Read-only display.</summary>
        [DataType(DataType.Currency)]
        [Display(Name = "Price (auto)")]
        public decimal Budget { get; set; }

        /// <summary>Auto-calculated commission.</summary>
        [DataType(DataType.Currency)]
        [Display(Name = "Commission")]
        public decimal CommissionAmount { get; set; }

        /// <summary>Auto-calculated writer earnings.</summary>
        [DataType(DataType.Currency)]
        [Display(Name = "Writer Earnings")]
        public decimal WriterEarnings { get; set; }

        /// <summary>Number of sources (informational).</summary>
        [Range(0, 100)]
        [Display(Name = "Number of Sources")]
        public int NumberOfSources { get; set; }

        [Required]
        [Display(Name = "Status")]
        public OrderStatus Status { get; set; }
    }
}