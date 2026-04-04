using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PharmaGo.Domain.Models;

namespace PharmaGo.Infrastructure.Persistence.Configurations;

public class SupplierMedicineConfiguration : IEntityTypeConfiguration<SupplierMedicine>
{
    public void Configure(EntityTypeBuilder<SupplierMedicine> builder)
    {
        builder.ToTable("supplier_medicines");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.WholesalePrice).HasColumnType("numeric(18,2)");
        builder.Property(x => x.AvailableQuantity).IsRequired();
        builder.Property(x => x.MinimumOrderQuantity).IsRequired();
        builder.Property(x => x.EstimatedDeliveryHours).IsRequired();

        builder.HasIndex(x => new { x.DepotId, x.MedicineId }).IsUnique();

        builder.HasOne(x => x.Depot)
            .WithMany(x => x.SupplierMedicines)
            .HasForeignKey(x => x.DepotId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Medicine)
            .WithMany(x => x.SupplierMedicines)
            .HasForeignKey(x => x.MedicineId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
