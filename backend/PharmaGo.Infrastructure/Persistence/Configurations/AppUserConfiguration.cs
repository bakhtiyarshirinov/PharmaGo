using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PharmaGo.Domain.Models;

namespace PharmaGo.Infrastructure.Persistence.Configurations;

public class AppUserConfiguration : IEntityTypeConfiguration<AppUser>
{
    public void Configure(EntityTypeBuilder<AppUser> builder)
    {
        builder.ToTable("users");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(x => x.LastName).HasMaxLength(100).IsRequired();
        builder.Property(x => x.PhoneNumber).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Email).HasMaxLength(256);
        builder.Property(x => x.TelegramUsername).HasMaxLength(128);
        builder.Property(x => x.TelegramChatId).HasMaxLength(128);
        builder.Property(x => x.Role).IsRequired();

        builder.HasIndex(x => x.PhoneNumber).IsUnique();
        builder.HasIndex(x => x.Email);

        builder.HasOne(x => x.Pharmacy)
            .WithMany(x => x.Employees)
            .HasForeignKey(x => x.PharmacyId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
