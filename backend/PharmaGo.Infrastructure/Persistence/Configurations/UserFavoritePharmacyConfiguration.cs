using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PharmaGo.Domain.Models;

namespace PharmaGo.Infrastructure.Persistence.Configurations;

public class UserFavoritePharmacyConfiguration : IEntityTypeConfiguration<UserFavoritePharmacy>
{
    public void Configure(EntityTypeBuilder<UserFavoritePharmacy> builder)
    {
        builder.ToTable("user_favorite_pharmacies");

        builder.HasKey(x => x.Id);

        builder.HasIndex(x => new { x.UserId, x.PharmacyId }).IsUnique();
        builder.HasIndex(x => new { x.UserId, x.CreatedAtUtc });

        builder.HasOne(x => x.User)
            .WithMany(x => x.FavoritePharmacies)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Pharmacy)
            .WithMany(x => x.FavoritedByUsers)
            .HasForeignKey(x => x.PharmacyId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
