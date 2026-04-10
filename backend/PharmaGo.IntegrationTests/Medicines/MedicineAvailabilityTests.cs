using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PharmaGo.Application.Medicines.Queries.GetMedicineAvailability;
using PharmaGo.IntegrationTests.Infrastructure;
using PharmaGo.Infrastructure.Persistence;

namespace PharmaGo.IntegrationTests.Medicines;

public class MedicineAvailabilityTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly HttpClient _client = factory.CreateClient();

    public Task InitializeAsync() => factory.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task GetAvailability_ShouldReturnNearbyPharmaciesForMedicine()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var medicineId = await db.Medicines
            .Where(x => x.BrandName == "Panadol")
            .Select(x => x.Id)
            .FirstAsync();

        var response = await _client.GetAsync($"/api/medicines/{medicineId}/availability?latitude=40.3777&longitude=49.8920&radiusKm=10");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadAsAsync<MedicineAvailabilityResponse>();
        Assert.NotNull(payload);
        Assert.Equal("Panadol", payload!.BrandName);
        Assert.True(payload.PharmacyCount >= 2);
        Assert.True(payload.TotalAvailableQuantity > 0);
        Assert.NotEmpty(payload.Availabilities);
        Assert.Equal("PharmaGo Central", payload.Availabilities.First().PharmacyName);
        Assert.All(payload.Availabilities, x => Assert.True(!x.DistanceKm.HasValue || x.DistanceKm <= 10d));
    }

    [Fact]
    public async Task GetAvailability_OnlyReservable_ShouldFilterNonReservableStock()
    {
        Guid medicineId;

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            medicineId = await db.Medicines
                .Where(x => x.BrandName == "Panadol")
                .Select(x => x.Id)
                .FirstAsync();

            var northStock = await db.StockItems
                .Include(x => x.Pharmacy)
                .FirstAsync(x => x.MedicineId == medicineId && x.Pharmacy!.Name == "PharmaGo North");

            northStock.IsReservable = false;
            northStock.LastStockUpdatedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync();
        }

        var response = await _client.GetAsync($"/api/medicines/{medicineId}/availability?onlyReservable=true&sortBy=price");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadAsAsync<MedicineAvailabilityResponse>();
        Assert.NotNull(payload);
        Assert.NotEmpty(payload!.Availabilities);
        Assert.DoesNotContain(payload.Availabilities, x => x.PharmacyName == "PharmaGo North");
    }

    [Fact]
    public async Task GetAvailability_WithMissingLongitude_ShouldReturnBadRequest()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var medicineId = await db.Medicines
            .Where(x => x.BrandName == "Panadol")
            .Select(x => x.Id)
            .FirstAsync();

        var response = await _client.GetAsync($"/api/medicines/{medicineId}/availability?latitude=40.3777");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetAvailability_WithOutOfRangeLongitude_ShouldReturnBadRequest()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var medicineId = await db.Medicines
            .Where(x => x.BrandName == "Panadol")
            .Select(x => x.Id)
            .FirstAsync();

        var response = await _client.GetAsync($"/api/medicines/{medicineId}/availability?latitude=40.3777&longitude=249.8920");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
