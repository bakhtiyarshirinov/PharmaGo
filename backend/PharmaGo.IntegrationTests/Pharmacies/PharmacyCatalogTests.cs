using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PharmaGo.Application.Common.Contracts;
using PharmaGo.Application.Pharmacies.Queries.GetPharmacyDetail;
using PharmaGo.Application.Pharmacies.Queries.GetPharmacyMedicines;
using PharmaGo.IntegrationTests.Infrastructure;
using PharmaGo.Infrastructure.Persistence;

namespace PharmaGo.IntegrationTests.Pharmacies;

public class PharmacyCatalogTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly HttpClient _client = factory.CreateClient();

    public Task InitializeAsync() => factory.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task GetById_ShouldReturnPharmacyCard()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var pharmacyId = await db.Pharmacies
            .Where(x => x.Name == "PharmaGo Central")
            .Select(x => x.Id)
            .FirstAsync();

        var response = await _client.GetAsync($"/api/pharmacies/{pharmacyId}?latitude=40.3777&longitude=49.8920");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadAsAsync<PharmacyDetailResponse>();
        Assert.NotNull(payload);
        Assert.Equal("PharmaGo Central", payload!.Name);
        Assert.True(payload.SupportsReservations);
        Assert.True(payload.IsOpenNow);
        Assert.NotNull(payload.DistanceKm);
        Assert.True(payload.AvailableMedicineCount >= 2);
    }

    [Fact]
    public async Task GetMedicines_ShouldReturnPagedPharmacyCatalog()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var pharmacyId = await db.Pharmacies
            .Where(x => x.Name == "PharmaGo Central")
            .Select(x => x.Id)
            .FirstAsync();

        var response = await _client.GetAsync($"/api/pharmacies/{pharmacyId}/medicines?sortBy=price&sortDirection=asc&page=1&pageSize=10");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadAsAsync<PagedResponse<PharmacyMedicineResponse>>();
        Assert.NotNull(payload);
        Assert.True(payload!.Items.Count >= 2);
        Assert.Equal("price", payload.SortBy);
        Assert.Equal("asc", payload.SortDirection);

        var first = payload.Items.First();
        Assert.False(string.IsNullOrWhiteSpace(first.BrandName));
        Assert.True(first.AvailableQuantity > 0);
        Assert.NotNull(first.MinRetailPrice);
    }
}
