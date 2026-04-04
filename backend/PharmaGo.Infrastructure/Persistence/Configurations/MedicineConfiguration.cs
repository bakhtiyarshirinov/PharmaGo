using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PharmaGo.Domain.Models;

namespace PharmaGo.Infrastructure.Persistence.Configurations;

public class MedicineConfiguration : IEntityTypeConfiguration<Medicine>
{
    public void Configure(EntityTypeBuilder<Medicine> builder)
    {
        builder.ToTable("medicines");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.BrandName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.GenericName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(2000);
        builder.Property(x => x.DosageForm).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Strength).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Manufacturer).HasMaxLength(200).IsRequired();
        builder.Property(x => x.CountryOfOrigin).HasMaxLength(100);
        builder.Property(x => x.Barcode).HasMaxLength(64);

        builder.HasIndex(x => x.BrandName);
        builder.HasIndex(x => x.GenericName);
        builder.HasIndex(x => x.Barcode).IsUnique();

        builder.HasOne(x => x.Category)
            .WithMany(x => x.Medicines)
            .HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
