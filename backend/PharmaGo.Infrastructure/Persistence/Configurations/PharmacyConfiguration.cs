using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PharmaGo.Domain.Models;

namespace PharmaGo.Infrastructure.Persistence.Configurations;

public class PharmacyConfiguration : IEntityTypeConfiguration<Pharmacy>
{
    public void Configure(EntityTypeBuilder<Pharmacy> builder)
    {
        builder.ToTable("pharmacies");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Address).HasMaxLength(400).IsRequired();
        builder.Property(x => x.City).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Region).HasMaxLength(100);
        builder.Property(x => x.PhoneNumber).HasMaxLength(32);
        builder.Property(x => x.Latitude).HasMaxLength(32);
        builder.Property(x => x.Longitude).HasMaxLength(32);
        builder.Property(x => x.LocationLatitude).HasColumnType("numeric(9,6)");
        builder.Property(x => x.LocationLongitude).HasColumnType("numeric(9,6)");
        builder.Property(x => x.OpeningHoursJson).HasMaxLength(4000);
        builder.Property(x => x.SupportsReservations).HasDefaultValue(true);

        builder.HasIndex(x => new { x.City, x.IsActive });
        builder.HasIndex(x => new { x.IsActive, x.LocationLatitude, x.LocationLongitude });
        builder.HasIndex(x => new { x.IsActive, x.SupportsReservations });

        builder.HasOne(x => x.PharmacyChain)
            .WithMany(x => x.Pharmacies)
            .HasForeignKey(x => x.PharmacyChainId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
