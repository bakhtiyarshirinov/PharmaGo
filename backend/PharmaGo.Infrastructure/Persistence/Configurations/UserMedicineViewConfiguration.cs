using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PharmaGo.Domain.Models;

namespace PharmaGo.Infrastructure.Persistence.Configurations;

public class UserMedicineViewConfiguration : IEntityTypeConfiguration<UserMedicineView>
{
    public void Configure(EntityTypeBuilder<UserMedicineView> builder)
    {
        builder.ToTable("user_medicine_views");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.LastViewedAtUtc).IsRequired();
        builder.Property(x => x.ViewCount).IsRequired();

        builder.HasIndex(x => new { x.UserId, x.MedicineId }).IsUnique();
        builder.HasIndex(x => new { x.UserId, x.LastViewedAtUtc });

        builder.HasOne(x => x.User)
            .WithMany(x => x.MedicineViews)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Medicine)
            .WithMany(x => x.ViewedByUsers)
            .HasForeignKey(x => x.MedicineId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
