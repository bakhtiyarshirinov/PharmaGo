using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PharmaGo.Domain.Models;

namespace PharmaGo.Infrastructure.Persistence.Configurations;

public class ReservationIdempotencyRecordConfiguration : IEntityTypeConfiguration<ReservationIdempotencyRecord>
{
    public void Configure(EntityTypeBuilder<ReservationIdempotencyRecord> builder)
    {
        builder.ToTable("reservation_idempotency_records");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.IdempotencyKey)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(x => x.RequestHash)
            .HasMaxLength(128)
            .IsRequired();

        builder.HasIndex(x => new { x.UserId, x.IdempotencyKey })
            .IsUnique();

        builder.HasIndex(x => x.ReservationId);

        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Reservation)
            .WithMany()
            .HasForeignKey(x => x.ReservationId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
