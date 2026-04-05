using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PharmaGo.Domain.Models;

namespace PharmaGo.Infrastructure.Persistence.Configurations;

public class UserPharmacyViewConfiguration : IEntityTypeConfiguration<UserPharmacyView>
{
    public void Configure(EntityTypeBuilder<UserPharmacyView> builder)
    {
        builder.ToTable("user_pharmacy_views");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.LastViewedAtUtc).IsRequired();
        builder.Property(x => x.ViewCount).IsRequired();

        builder.HasIndex(x => new { x.UserId, x.PharmacyId }).IsUnique();
        builder.HasIndex(x => new { x.UserId, x.LastViewedAtUtc });

        builder.HasOne(x => x.User)
            .WithMany(x => x.PharmacyViews)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Pharmacy)
            .WithMany(x => x.ViewedByUsers)
            .HasForeignKey(x => x.PharmacyId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
