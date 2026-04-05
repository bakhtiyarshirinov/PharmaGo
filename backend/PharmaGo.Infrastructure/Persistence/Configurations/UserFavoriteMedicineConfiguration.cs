using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PharmaGo.Domain.Models;

namespace PharmaGo.Infrastructure.Persistence.Configurations;

public class UserFavoriteMedicineConfiguration : IEntityTypeConfiguration<UserFavoriteMedicine>
{
    public void Configure(EntityTypeBuilder<UserFavoriteMedicine> builder)
    {
        builder.ToTable("user_favorite_medicines");

        builder.HasKey(x => x.Id);

        builder.HasIndex(x => new { x.UserId, x.MedicineId }).IsUnique();
        builder.HasIndex(x => new { x.UserId, x.CreatedAtUtc });

        builder.HasOne(x => x.User)
            .WithMany(x => x.FavoriteMedicines)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Medicine)
            .WithMany(x => x.FavoritedByUsers)
            .HasForeignKey(x => x.MedicineId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
