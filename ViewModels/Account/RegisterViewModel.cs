using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace ScholarRescue.ViewModels.Account
{
    /// <summary>
    /// ViewModel for user registration. Collects required identity information
    /// and profile details to create a new account.
    /// For Writer/Tutor registration, additional professional information is collected.
    /// </summary>
    public class RegisterViewModel
    {
        /// <summary>
        /// The user's first name.
        /// </summary>
        [Required(ErrorMessage = "First name is required.")]
        [MaxLength(100, ErrorMessage = "First name cannot exceed 100 characters.")]
        [Display(Name = "First Name")]
        public string FirstName { get; set; } = string.Empty;

        /// <summary>
        /// The user's last name.
        /// </summary>
        [Required(ErrorMessage = "Last name is required.")]
        [MaxLength(100, ErrorMessage = "Last name cannot exceed 100 characters.")]
        [Display(Name = "Last Name")]
        public string LastName { get; set; } = string.Empty;

        /// <summary>
        /// The email address used as the username for login.
        /// </summary>
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email address.")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// The user's role in the system (Client or Writer).
        /// </summary>
        [Required(ErrorMessage = "Account type is required.")]
        [Display(Name = "I want to register as")]
        public string UserType { get; set; } = "Client";

        /// <summary>
        /// The password for the account.
        /// </summary>
        [Required(ErrorMessage = "Password is required.")]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at most {1} characters long.",
            MinimumLength = 8)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// Confirmation of the password.
        /// </summary>
        [Required(ErrorMessage = "Please confirm your password.")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        [Display(Name = "Confirm Password")]
        public string ConfirmPassword { get; set; } = string.Empty;

        // --- Writer/Tutor extended fields (required only when UserType = "Writer") ---

        /// <summary>
        /// Phone number (writer only).
        /// </summary>
        [Display(Name = "Phone Number")]
        public string? PhoneNumber { get; set; }

        /// <summary>
        /// Highest academic qualification (writer only).
        /// </summary>
        [MaxLength(150)]
        [Display(Name = "Highest Academic Qualification")]
        public string? HighestQualification { get; set; }

        /// <summary>
        /// Specialization / subject areas (writer only).
        /// </summary>
        [MaxLength(500)]
        [Display(Name = "Specialization / Subject Areas")]
        public string? Specialization { get; set; }

        /// <summary>
        /// Professional biography (writer only). 150-500 words.
        /// </summary>
        [Display(Name = "Professional Biography")]
        public string? Biography { get; set; }

        /// <summary>
        /// Curriculum vitae (writer only).
        /// </summary>
        [Display(Name = "Curriculum Vitae (PDF/DOCX)")]
        public IFormFile? CvFile { get; set; }

        /// <summary>
        /// Degree certificate / qualification document (writer only).
        /// </summary>
        [Display(Name = "Degree Certificate")]
        public IFormFile? DegreeFile { get; set; }

        /// <summary>
        /// Writing sample (writer only).
        /// </summary>
        [Display(Name = "Writing Sample")]
        public IFormFile? WritingSampleFile { get; set; }
    }
}
