using Microsoft.EntityFrameworkCore;
using PharmaGo.Application.Abstractions;
using PharmaGo.Domain.Models;

namespace PharmaGo.Infrastructure.Persistence;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : DbContext(options), IApplicationDbContext
{
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<Depot> Depots => Set<Depot>();
    public DbSet<Medicine> Medicines => Set<Medicine>();
    public DbSet<MedicineCategory> MedicineCategories => Set<MedicineCategory>();
    public DbSet<Pharmacy> Pharmacies => Set<Pharmacy>();
    public DbSet<PharmacyChain> PharmacyChains => Set<PharmacyChain>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Reservation> Reservations => Set<Reservation>();
    public DbSet<ReservationItem> ReservationItems => Set<ReservationItem>();
    public DbSet<StockItem> StockItems => Set<StockItem>();
    public DbSet<SupplierMedicine> SupplierMedicines => Set<SupplierMedicine>();
    public DbSet<UserFavoriteMedicine> UserFavoriteMedicines => Set<UserFavoriteMedicine>();
    public DbSet<UserFavoritePharmacy> UserFavoritePharmacies => Set<UserFavoritePharmacy>();
    public DbSet<UserMedicineView> UserMedicineViews => Set<UserMedicineView>();
    public DbSet<UserPharmacyView> UserPharmacyViews => Set<UserPharmacyView>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        modelBuilder.HasDefaultSchema("public");
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var utcNow = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAtUtc = utcNow;
            }

            if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAtUtc = utcNow;
            }
        }

        foreach (var entry in ChangeTracker.Entries<StockItem>())
        {
            if (entry.State == EntityState.Added && entry.Entity.ConcurrencyToken == Guid.Empty)
            {
                entry.Entity.ConcurrencyToken = Guid.NewGuid();
            }

            if (entry.State == EntityState.Modified)
            {
                entry.Entity.ConcurrencyToken = Guid.NewGuid();
                entry.Entity.LastStockUpdatedAtUtc = utcNow;
            }
        }

        foreach (var entry in ChangeTracker.Entries<Reservation>())
        {
            if (entry.State == EntityState.Added && entry.Entity.ConcurrencyToken == Guid.Empty)
            {
                entry.Entity.ConcurrencyToken = Guid.NewGuid();
            }

            if (entry.State == EntityState.Modified)
            {
                entry.Entity.ConcurrencyToken = Guid.NewGuid();
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
