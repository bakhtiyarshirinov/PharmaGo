using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PharmaGo.Domain.Models.Enums;
using PharmaGo.IntegrationTests.Infrastructure;
using PharmaGo.Infrastructure.Persistence;

namespace PharmaGo.IntegrationTests.Persistence;

public class DatabaseSeederTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    public Task InitializeAsync() => factory.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task SeedAsync_ShouldRefreshShowcaseReservations_AndReconcileReservedStock()
    {
        Guid medicineId;

        using (var arrangeScope = factory.Services.CreateScope())
        {
            var db = arrangeScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var demoReservation = await db.Reservations
                .Include(x => x.Items)
                .FirstAsync(x => x.ReservationNumber == "PG-DEMO-1002");

            medicineId = demoReservation.Items.Single().MedicineId;
            var stocks = await db.StockItems
                .Where(x => x.PharmacyId == demoReservation.PharmacyId && x.MedicineId == medicineId)
                .ToListAsync();

            demoReservation.Status = ReservationStatus.Confirmed;
            demoReservation.ReservedUntilUtc = DateTime.UtcNow.AddHours(-3);

            foreach (var stock in stocks)
            {
                stock.ReservedQuantity = 0;
            }

            await db.SaveChangesAsync();
        }

        using (var seedScope = factory.Services.CreateScope())
        {
            var db = seedScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await ApplicationDbContextSeeder.SeedAsync(db);
        }

        using var assertScope = factory.Services.CreateScope();
        var assertDb = assertScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var refreshedReservation = await assertDb.Reservations
            .AsNoTracking()
            .FirstAsync(x => x.ReservationNumber == "PG-DEMO-1002");

        var refreshedReservedQuantity = await assertDb.StockItems
            .AsNoTracking()
            .Where(x => x.PharmacyId == refreshedReservation.PharmacyId && x.MedicineId == medicineId)
            .SumAsync(x => x.ReservedQuantity);

        Assert.True(refreshedReservation.ReservedUntilUtc > DateTime.UtcNow);
        Assert.True(refreshedReservation.PickupAvailableFromUtc.HasValue);
        Assert.True(refreshedReservedQuantity >= 1);
    }
}
