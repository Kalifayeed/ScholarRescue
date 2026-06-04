using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ScholarRescue.Models;

namespace ScholarRescue.Data
{
    public class ScholarRescueDbContext
        : IdentityDbContext<ApplicationUser>
    {
        public ScholarRescueDbContext(
            DbContextOptions<ScholarRescueDbContext> options)
            : base(options)
        {
        }
    }
}