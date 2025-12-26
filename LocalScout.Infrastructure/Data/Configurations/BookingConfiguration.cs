using LocalScout.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LocalScout.Infrastructure.Data.Configurations
{
    public class BookingConfiguration : IEntityTypeConfiguration<Booking>
    {
        public void Configure(EntityTypeBuilder<Booking> builder)
        {
            builder.HasKey(b => b.BookingId);

            builder.Property(b => b.ServiceId)
                .IsRequired();

            builder.Property(b => b.UserId)
                .IsRequired()
                .HasMaxLength(450);

            builder.Property(b => b.ProviderId)
                .IsRequired()
                .HasMaxLength(450);

            builder.Property(b => b.Description)
                .HasMaxLength(2000);

            builder.Property(b => b.ImagePaths)
                .HasColumnType("nvarchar(max)");

            builder.Property(b => b.AddressArea)
                .HasMaxLength(500);

            builder.Property(b => b.NegotiatedPrice)
                .HasColumnType("decimal(18,2)");

            builder.Property(b => b.ProviderNotes)
                .HasMaxLength(1000);

            builder.Property(b => b.CancellationReason)
                .HasMaxLength(500);

            builder.Property(b => b.CancelledBy)
                .HasMaxLength(50);

            // Indexes for common queries
            builder.HasIndex(b => b.UserId)
                .HasDatabaseName("IX_Bookings_UserId");

            builder.HasIndex(b => b.ProviderId)
                .HasDatabaseName("IX_Bookings_ProviderId");

            builder.HasIndex(b => b.Status)
                .HasDatabaseName("IX_Bookings_Status");

            builder.HasIndex(b => b.CreatedAt)
                .HasDatabaseName("IX_Bookings_CreatedAt");

            builder.HasIndex(b => new { b.ProviderId, b.Status })
                .HasDatabaseName("IX_Bookings_Provider_Status");

            builder.HasIndex(b => new { b.UserId, b.Status })
                .HasDatabaseName("IX_Bookings_User_Status");
        }
    }
}
