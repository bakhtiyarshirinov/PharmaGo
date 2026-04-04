using Microsoft.EntityFrameworkCore;
using PharmaGo.Domain.Models;

namespace PharmaGo.Application.Abstractions;

public interface IApplicationDbContext
{
    DbSet<AuditLog> AuditLogs { get; }
    DbSet<AppUser> Users { get; }
    DbSet<Depot> Depots { get; }
    DbSet<Medicine> Medicines { get; }
    DbSet<MedicineCategory> MedicineCategories { get; }
    DbSet<Pharmacy> Pharmacies { get; }
    DbSet<PharmacyChain> PharmacyChains { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<Reservation> Reservations { get; }
    DbSet<ReservationItem> ReservationItems { get; }
    DbSet<StockItem> StockItems { get; }
    DbSet<SupplierMedicine> SupplierMedicines { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
