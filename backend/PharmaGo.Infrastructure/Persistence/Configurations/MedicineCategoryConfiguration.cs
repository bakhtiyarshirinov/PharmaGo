using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PharmaGo.Domain.Models;

namespace PharmaGo.Infrastructure.Persistence.Configurations;

public class MedicineCategoryConfiguration : IEntityTypeConfiguration<MedicineCategory>
{
    public void Configure(EntityTypeBuilder<MedicineCategory> builder)
    {
        builder.ToTable("medicine_categories");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name).HasMaxLength(150).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(1000);

        builder.HasIndex(x => x.Name).IsUnique();
    }
}
