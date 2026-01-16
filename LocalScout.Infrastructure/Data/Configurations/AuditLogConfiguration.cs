using LocalScout.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LocalScout.Infrastructure.Data.Configurations
{
    public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
    {
        public void Configure(EntityTypeBuilder<AuditLog> entity)
        {
            entity.HasKey(e => e.AuditLogId);

            // Indexes for common audit log queries
            entity.HasIndex(e => e.Timestamp)
                .HasDatabaseName("IX_AuditLogs_Timestamp");

            entity.HasIndex(e => e.Category)
                .HasDatabaseName("IX_AuditLogs_Category");

            entity.HasIndex(e => e.Action)
                .HasDatabaseName("IX_AuditLogs_Action");

            entity.HasIndex(e => e.UserId)
                .HasDatabaseName("IX_AuditLogs_UserId");
        }
    }
}
