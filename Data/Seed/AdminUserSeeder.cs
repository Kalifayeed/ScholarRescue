using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using ScholarRescue.Models;

namespace ScholarRescue.Data.Seed
{
    /// <summary>
    /// Seeds the first administrator account from environment variables.
    /// Reads SEED_ADMIN_EMAIL and SEED_ADMIN_PASSWORD from environment.
    /// Falls back to hardcoded defaults ONLY if environment variables are not set
    /// (safe for local development).
    /// </summary>
    public static class AdminUserSeeder
    {
        /// <summary>
        /// Ensures an administrator account exists using environment variables.
        /// Environment variables (set in deployment) override hardcoded defaults.
        /// </summary>
        public static async Task SeedAdminUserAsync(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ILogger? logger = null)
        {
            // Use environment variables first, fall back to safe defaults for local dev
            string adminEmail = Environment.GetEnvironmentVariable("SEED_ADMIN_EMAIL")
                ?? "admin@scholarrescue.com";
            string adminPassword = Environment.GetEnvironmentVariable("SEED_ADMIN_PASSWORD")
                ?? "Admin123!";

            // Ensure the Administrator role exists first
            if (!await roleManager.RoleExistsAsync("Administrator"))
            {
                await roleManager.CreateAsync(new IdentityRole("Administrator"));
            }

            // Check if the admin user already exists
            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            if (adminUser != null)
            {
                // Admin already exists, no need to create
                logger?.LogInformation("Admin seed user already exists. Skipping.");
                return;
            }

            // Create the administrator account
            adminUser = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                FirstName = "System",
                LastName = "Administrator",
                UserType = "Administrator",
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(adminUser, adminPassword);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, "Administrator");
                logger?.LogInformation("Admin seed user ensured.");
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
