using System.ComponentModel.DataAnnotations;

namespace ScholarRescue.ViewModels.Account
{
    /// <summary>
    /// ViewModel for user login. Collects credentials to authenticate a user.
    /// </summary>
    public class LoginViewModel
    {
        /// <summary>
        /// The email address used as the username for login.
        /// </summary>
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email address.")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// The password for the account.
        /// </summary>
        [Required(ErrorMessage = "Password is required.")]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// Whether to persist the login session across browser sessions.
        /// </summary>
        [Display(Name = "Remember me")]
        public bool RememberMe { get; set; }
    }
}