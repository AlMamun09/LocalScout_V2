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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure Notification entity
            modelBuilder.Entity<Notification>(entity =>
            {
                entity.HasKey(e => e.Id);
                
                entity.Property(e => e.UserId)
                    .IsRequired()
                    .HasMaxLength(450); // Match AspNetUsers.Id length
                
                entity.Property(e => e.Title)
                    .IsRequired()
                    .HasMaxLength(200);
                
                entity.Property(e => e.Message)
                    .IsRequired()
                    .HasMaxLength(1000);
                
                entity.Property(e => e.CreatedAt)
                    .IsRequired();
                
                entity.Property(e => e.IsRead)
                    .IsRequired()
                    .HasDefaultValue(false);
                
                entity.Property(e => e.MetaJson)
                    .HasMaxLength(2000);

                // Create indexes for performance
                entity.HasIndex(e => e.UserId)
                    .HasDatabaseName("IX_Notifications_UserId");
                
                entity.HasIndex(e => new { e.UserId, e.IsRead })
                    .HasDatabaseName("IX_Notifications_UserId_IsRead");
                
                entity.HasIndex(e => e.CreatedAt)
                    .HasDatabaseName("IX_Notifications_CreatedAt");
            });
        }
    }
}
