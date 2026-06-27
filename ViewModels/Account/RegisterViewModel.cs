using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace ScholarRescue.ViewModels.Account
{
    /// <summary>
    /// ViewModel for user registration. Collects required identity information
    /// and profile details to create a new account.
    /// For Writer/Tutor registration, extended professional information is collected.
    /// </summary>
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "First name is required.")]
        [MaxLength(100, ErrorMessage = "First name cannot exceed 100 characters.")]
        [Display(Name = "First Name")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Last name is required.")]
        [MaxLength(100, ErrorMessage = "Last name cannot exceed 100 characters.")]
        [Display(Name = "Last Name")]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email address.")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Account type is required.")]
        [Display(Name = "I want to register as")]
        public string UserType { get; set; } = "Client";

        [Required(ErrorMessage = "Password is required.")]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at most {1} characters long.",
            MinimumLength = 8)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please confirm your password.")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        [Display(Name = "Confirm Password")]
        public string ConfirmPassword { get; set; } = string.Empty;

        // --- Writer/Tutor extended fields ---

        [Display(Name = "Phone Number")]
        public string? PhoneNumber { get; set; }

        /// <summary>Highest education level (writer only). Select from dropdown.</summary>
        [MaxLength(100)]
        [Display(Name = "Education Level")]
        public string? EducationLevel { get; set; }

        /// <summary>Alias for EducationLevel to maintain backward compatibility with WriterApplicationService.</summary>
        [MaxLength(150)]
        public string? HighestQualification { get => EducationLevel; set => EducationLevel = value; }

        /// <summary>Subject specializations (writer only). Comma-separated, max 5.</summary>
        [MaxLength(500)]
        [Display(Name = "Subject Specializations")]
        public string? Specialization { get; set; }

        /// <summary>Comma-separated list of selected specializations for validation.</summary>
        [Display(Name = "Selected Specializations")]
        public string? SelectedSpecializations { get; set; }

        [Display(Name = "Professional Biography")]
        public string? Biography { get; set; }

        /// <summary>National ID document (replaces CV for writer registration).</summary>
        [Display(Name = "National ID (PDF/JPG/PNG)")]
        public IFormFile? NationalIdFile { get; set; }

        /// <summary>Alias for NationalIdFile to maintain backward compatibility with WriterApplicationService.</summary>
        public IFormFile? CvFile { get => NationalIdFile; set => NationalIdFile = value; }

        [Display(Name = "Degree Certificate")]
        public IFormFile? DegreeFile { get; set; }

        [Display(Name = "Writing Sample")]
        public IFormFile? WritingSampleFile { get; set; }
    }
}