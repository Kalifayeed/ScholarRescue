using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using ScholarRescue.Models.Enums;

namespace ScholarRescue.ViewModels.Order
{
    /// <summary>
    /// Combined registration + order creation form for guest (anonymous) users.
    /// The account section is fixed to Client — no account-type selector is exposed.
    /// Request fields mirror <see cref="CreateOrderViewModel"/>; the account fields
    /// mirror the required fields of the Client registration flow.
    /// </summary>
    public class GuestOrderViewModel
    {
        // ═══════════════════════════════════════════
        // ACCOUNT FIELDS (Client only — fixed)
        // ═══════════════════════════════════════════
        [Required(ErrorMessage = "First name is required.")]
        [MaxLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Last name is required.")]
        [MaxLength(100)]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email address is required.")]
        [EmailAddress(ErrorMessage = "Invalid email address.")]
        [MaxLength(256)]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required.")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please confirm your password.")]
        [DataType(DataType.Password)]
        [Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "You must accept the Terms of Service and Privacy Policy to continue.")]
        [Display(Name = "I agree to the Terms of Service and Privacy Policy")]
        public bool AcceptTerms { get; set; }

        // ═══════════════════════════════════════════
        // ORDER FIELDS (mirror CreateOrderViewModel)
        // ═══════════════════════════════════════════
        [Required(ErrorMessage = "Please select a request type.")]
        [Display(Name = "Request Type")]
        public RequestType RequestType { get; set; }

        [Required(ErrorMessage = "Assignment title is required.")]
        [MaxLength(500)]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Description is required.")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Subject is required.")]
        [MaxLength(200)]
        public string Subject { get; set; } = string.Empty;

        [Display(Name = "Specify Subject")]
        [MaxLength(200)]
        public string? OtherSubject { get; set; }

        [Required(ErrorMessage = "Academic level is required.")]
        public AcademicLevel AcademicLevel { get; set; } = AcademicLevel.Undergraduate;

        [Required(ErrorMessage = "Citation format is required.")]
        public CitationFormat CitationFormat { get; set; } = CitationFormat.APA_7th;

        [Range(1, 1000, ErrorMessage = "Pages must be between 1 and 1000.")]
        public int? Pages { get; set; }

        [Range(0, 100, ErrorMessage = "Sources must be between 0 and 100.")]
        public int NumberOfSources { get; set; }

        [Required(ErrorMessage = "Deadline is required.")]
        [DataType(DataType.DateTime)]
        public DateTime Deadline { get; set; } = DateTime.UtcNow.AddDays(7);

        /// <summary>
        /// Actual file data uploaded by the client. Bound by MVC model binding from the file input.
        /// </summary>
        public List<IFormFile>? UploadedFileData { get; set; }

        /// <summary>
        /// Whether payment is deferred. When true, the order is posted immediately
        /// to the marketplace without payment. Default false (Pay Now).
        /// </summary>
        [Display(Name = "Payment Timing")]
        public bool PayLater { get; set; }
    }
}