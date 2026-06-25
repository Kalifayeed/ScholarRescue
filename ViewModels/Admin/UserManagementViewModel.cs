using System.ComponentModel.DataAnnotations;

namespace ScholarRescue.ViewModels.Admin
{
    /// <summary>
    /// ViewModel for displaying a user in the admin user management list.
    /// </summary>
    public class UserManagementViewModel
    {
        public string Id { get; set; } = string.Empty;

        [Display(Name = "Name")]
        public string FullName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        [Display(Name = "Type")]
        public string UserType { get; set; } = string.Empty;

        public string Role { get; set; } = string.Empty;

        [Display(Name = "Email Confirmed")]
        public bool EmailConfirmed { get; set; }

        [Display(Name = "Locked")]
        public bool IsLockedOut { get; set; }

        [Display(Name = "Active")]
        public bool IsActive { get; set; }

        [Display(Name = "Deleted")]
        public bool IsDeleted { get; set; }

        [Display(Name = "Registered")]
        public DateTime CreatedAt { get; set; }

        [Display(Name = "Last Login")]
        public DateTime? LastLoginAt { get; set; }
    }
}