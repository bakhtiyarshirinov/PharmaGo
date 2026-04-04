using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PharmaGo.Domain.Models;

namespace PharmaGo.Infrastructure.Persistence.Configurations;

public class PharmacyChainConfiguration : IEntityTypeConfiguration<PharmacyChain>
{
    public void Configure(EntityTypeBuilder<PharmacyChain> builder)
    {
        builder.ToTable("pharmacy_chains");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.LegalName).HasMaxLength(250);
        builder.Property(x => x.TaxNumber).HasMaxLength(64);
        builder.Property(x => x.SupportPhone).HasMaxLength(32);
        builder.Property(x => x.SupportEmail).HasMaxLength(256);

        builder.HasIndex(x => x.Name).IsUnique();
    }
}
