using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PharmaGo.Domain.Models;

namespace PharmaGo.Infrastructure.Persistence.Configurations;

public class StockItemConfiguration : IEntityTypeConfiguration<StockItem>
{
    public void Configure(EntityTypeBuilder<StockItem> builder)
    {
        builder.ToTable("stock_items");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.BatchNumber).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Quantity).IsRequired();
        builder.Property(x => x.ReservedQuantity).IsRequired();
        builder.Property(x => x.PurchasePrice).HasColumnType("numeric(18,2)");
        builder.Property(x => x.RetailPrice).HasColumnType("numeric(18,2)");
        builder.Property(x => x.ReorderLevel).IsRequired();
        builder.Property(x => x.IsReservable).HasDefaultValue(true);

        builder.HasIndex(x => new { x.PharmacyId, x.MedicineId });
        builder.HasIndex(x => x.ExpirationDate);
        builder.HasIndex(x => new { x.MedicineId, x.PharmacyId, x.IsActive, x.IsReservable, x.ExpirationDate });
        builder.HasIndex(x => new { x.PharmacyId, x.IsActive, x.ExpirationDate });

        builder.HasOne(x => x.Pharmacy)
            .WithMany(x => x.StockItems)
            .HasForeignKey(x => x.PharmacyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Medicine)
            .WithMany(x => x.StockItems)
            .HasForeignKey(x => x.MedicineId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
