using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PharmaGo.Domain.Models;

namespace PharmaGo.Infrastructure.Persistence.Configurations;

public class ReservationConfiguration : IEntityTypeConfiguration<Reservation>
{
    public void Configure(EntityTypeBuilder<Reservation> builder)
    {
        builder.ToTable("reservations");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.ReservationNumber).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Status).IsRequired();
        builder.Property(x => x.Notes).HasMaxLength(1000);
        builder.Property(x => x.TotalAmount).HasColumnType("numeric(18,2)");
        builder.Property(x => x.TelegramChatId).HasMaxLength(128);
        builder.Property(x => x.ConcurrencyToken)
            .HasDefaultValueSql("gen_random_uuid()")
            .IsConcurrencyToken();

        builder.HasIndex(x => x.ReservationNumber).IsUnique();
        builder.HasIndex(x => new { x.CustomerId, x.Status });
        builder.HasIndex(x => new { x.PharmacyId, x.Status });

        builder.HasOne(x => x.Customer)
            .WithMany(x => x.Reservations)
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Pharmacy)
            .WithMany(x => x.Reservations)
            .HasForeignKey(x => x.PharmacyId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
