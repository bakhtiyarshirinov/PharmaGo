using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PharmaGo.Application.Medicines.Queries.SearchMedicines;
using PharmaGo.IntegrationTests.Infrastructure;
using PharmaGo.Infrastructure.Persistence;

namespace PharmaGo.IntegrationTests.Medicines;

public class MedicineSearchTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly HttpClient _client = factory.CreateClient();

    public Task InitializeAsync() => factory.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Search_WithGeoParams_ShouldReturnDistanceAwareAvailabilities()
    {
        var response = await _client.GetAsync(
            "/api/medicines/search?query=pan&latitude=40.3777&longitude=49.8920&radiusKm=10&availabilityLimit=1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadAsAsync<IReadOnlyCollection<MedicineSearchResponse>>();
        Assert.NotNull(payload);

        var medicine = Assert.Single(payload!);
        Assert.Equal("Panadol", medicine.BrandName);
        Assert.True(medicine.PharmacyCount >= 2);
        Assert.NotNull(medicine.NearestDistanceKm);

        var availability = Assert.Single(medicine.Availabilities);
        Assert.Equal("PharmaGo Central", availability.PharmacyName);
        Assert.NotNull(availability.DistanceKm);
        Assert.True(availability.DistanceKm <= 0.05d);
        Assert.True(availability.IsOpenNow);
    }

    [Fact]
    public async Task Search_WithOnlyReservable_ShouldExcludeNonReservableStock()
    {
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var panadolId = await db.Medicines
                .Where(x => x.BrandName == "Panadol")
                .Select(x => x.Id)
                .FirstAsync();

            var northStock = await db.StockItems
                .Include(x => x.Pharmacy)
                .FirstAsync(x => x.MedicineId == panadolId && x.Pharmacy!.Name == "PharmaGo North");

            northStock.IsReservable = false;
            northStock.LastStockUpdatedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync();
        }

        var response = await _client.GetAsync("/api/medicines/search?query=pan&onlyReservable=true");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadAsAsync<IReadOnlyCollection<MedicineSearchResponse>>();
        Assert.NotNull(payload);

        var medicine = Assert.Single(payload!);
        Assert.True(medicine.PharmacyCount >= 1);
        Assert.True(medicine.TotalAvailableQuantity > 0);
        Assert.DoesNotContain(medicine.Availabilities, x => x.PharmacyName == "PharmaGo North");
        Assert.All(medicine.Availabilities, x => Assert.True(x.IsReservable));
    }

    [Fact]
    public async Task Search_WithMissingLongitude_ShouldReturnBadRequest()
    {
        var response = await _client.GetAsync("/api/medicines/search?query=pan&latitude=40.3777");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Search_WithOutOfRangeLatitude_ShouldReturnBadRequest()
    {
        var response = await _client.GetAsync("/api/medicines/search?query=pan&latitude=140.3777&longitude=49.8920");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
