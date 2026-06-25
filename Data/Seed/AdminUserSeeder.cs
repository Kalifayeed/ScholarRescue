using Microsoft.AspNetCore.Identity;
using ScholarRescue.Models;

namespace ScholarRescue.Data.Seed
{
    /// <summary>
    /// Seeds the default administrator account into the database on application startup.
    /// Creates an Administrator user with predefined credentials if one does not already exist.
    /// </summary>
    public static class AdminUserSeeder
    {
        /// <summary>
        /// The default email for the administrator account.
        /// </summary>
        public const string AdminEmail = "admin@scholarrescue.com";

        /// <summary>
        /// The default password for the administrator account.
        /// </summary>
        public const string AdminPassword = "Admin123!";

        /// <summary>
        /// Ensures the default administrator account exists in the database.
        /// </summary>
        public static async Task SeedAdminUserAsync(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            // Ensure the Administrator role exists first
            if (!await roleManager.RoleExistsAsync("Administrator"))
            {
                await roleManager.CreateAsync(new IdentityRole("Administrator"));
            }

            // Check if the admin user already exists
            var adminUser = await userManager.FindByEmailAsync(AdminEmail);
            if (adminUser != null)
            {
                // Admin already exists, no need to create
                return;
            }

            // Create the default administrator account
            adminUser = new ApplicationUser
            {
                UserName = AdminEmail,
                Email = AdminEmail,
                FirstName = "System",
                LastName = "Administrator",
                UserType = "Administrator",
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(adminUser, AdminPassword);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, "Administrator");
            }
            else
            {
                // Log errors if creation fails
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new InvalidOperationException(
                    $"Failed to create default admin user. Errors: {errors}");
            }
        }
    }
}