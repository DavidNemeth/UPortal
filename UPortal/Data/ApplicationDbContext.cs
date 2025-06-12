using Microsoft.EntityFrameworkCore;
using UPortal.Data.Models;

namespace UPortal.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<AppUser> AppUsers { get; set; }
        public DbSet<Location> Locations { get; set; }
        public DbSet<Machine> Machines { get; set; }
        public DbSet<ExternalApplication> ExternalApplications { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<AppUser>()
               .HasIndex(u => u.AzureAdObjectId)
               .IsUnique(); // Ensures no two users can have the same Azure AD ID
        }
    }
}