using LocalScout.Domain.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace LocalScout.Infrastructure.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<ServiceCategory> ServiceCategories { get; set; }
        public DbSet<VerificationRequest> VerificationRequests { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<Service> Services { get; set; }
        public DbSet<CategoryRequest> CategoryRequests { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        }
    }
}
