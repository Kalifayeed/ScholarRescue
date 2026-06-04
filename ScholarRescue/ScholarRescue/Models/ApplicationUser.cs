using Microsoft.AspNetCore.Identity;

namespace ScholarRescue.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FirstName { get; set; } = string.Empty;

        public string LastName { get; set; } = string.Empty;

        public string UserType { get; set; } = string.Empty;
    }
}