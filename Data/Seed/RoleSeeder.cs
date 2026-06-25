using Microsoft.AspNetCore.Identity;

namespace ScholarRescue.Data.Seed
{
    /// <summary>
    /// Seeds the application roles into the database on startup.
    /// </summary>
    public static class RoleSeeder
    {
        /// <summary>
        /// Ensures all required roles exist in the database.
        /// </summary>
        public static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
        {
            string[] roles =
            {
                "Administrator",
                "Writer",
                "Client"
            };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }
        }
    }
}