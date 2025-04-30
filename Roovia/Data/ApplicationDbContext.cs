using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Roovia.Models.Users;

namespace Roovia.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public DbSet<Company> Companies { get; set; }
        public DbSet<Branch> Branches { get; set; }

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure Address as an owned entity for Company
            modelBuilder.Entity<Company>().OwnsOne(c => c.Address);

            // Configure Address as an owned entity for Branch
            modelBuilder.Entity<Branch>().OwnsOne(b => b.Address);

            // Additional configurations (if any) can go here
        }
    }
}
