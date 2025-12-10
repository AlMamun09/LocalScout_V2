using LocalScout.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LocalScout.Infrastructure.Data.Configurations
{
    public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
    {
        public void Configure(EntityTypeBuilder<Notification> entity)
        {
            entity.HasKey(e => e.NotificationId);

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
        }
    }
}
