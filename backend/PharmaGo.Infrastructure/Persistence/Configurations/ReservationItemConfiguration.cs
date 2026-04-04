using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PharmaGo.Domain.Models;

namespace PharmaGo.Infrastructure.Persistence.Configurations;

public class ReservationItemConfiguration : IEntityTypeConfiguration<ReservationItem>
{
    public void Configure(EntityTypeBuilder<ReservationItem> builder)
    {
        builder.ToTable("reservation_items");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.UnitPrice).HasColumnType("numeric(18,2)");
        builder.Property(x => x.Quantity).IsRequired();

        builder.HasOne(x => x.Reservation)
            .WithMany(x => x.Items)
            .HasForeignKey(x => x.ReservationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Medicine)
            .WithMany(x => x.ReservationItems)
            .HasForeignKey(x => x.MedicineId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
