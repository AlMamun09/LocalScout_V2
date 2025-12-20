using LocalScout.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LocalScout.Infrastructure.Data.Configurations
{
    public class ServiceConfiguration : IEntityTypeConfiguration<Service>
    {
        public void Configure(EntityTypeBuilder<Service> entity)
        {
            entity.HasKey(e => e.ServiceId);

            entity.Property(e => e.Id)
                .HasMaxLength(450); // Provider's User ID

            entity.Property(e => e.ServiceName)
                .HasMaxLength(200);

            entity.Property(e => e.Description)
                .HasMaxLength(2000);

            entity.Property(e => e.PricingUnit)
                .HasMaxLength(50);

            entity.Property(e => e.MinPrice)
                .HasColumnType("decimal(18,2)");

            entity.HasIndex(e => e.Id)
                .HasDatabaseName("IX_Services_ProviderId");

            entity.HasIndex(e => e.ServiceCategoryId)
                .HasDatabaseName("IX_Services_CategoryId");

            entity.HasIndex(e => new { e.Id, e.IsActive, e.IsDeleted })
                .HasDatabaseName("IX_Services_Provider_Status");
        }
    }
}
