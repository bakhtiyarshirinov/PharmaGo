using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PharmaGo.Application.Common.Contracts;
using PharmaGo.Application.Pharmacies.Queries.SearchNearbyPharmacies;
using PharmaGo.IntegrationTests.Infrastructure;
using PharmaGo.Infrastructure.Persistence;

namespace PharmaGo.IntegrationTests.Pharmacies;

public class PharmacyDiscoveryTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly HttpClient _client = factory.CreateClient();

    public Task InitializeAsync() => factory.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task SearchNearby_ShouldReturnPharmaciesOrderedByDistance()
    {
        var response = await _client.GetAsync("/api/pharmacies/search?latitude=40.3777&longitude=49.8920&radiusKm=10");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadAsAsync<PagedResponse<NearbyPharmacyResponse>>();
        Assert.NotNull(payload);
        Assert.True(payload!.Items.Count >= 2);
        Assert.Equal("distance", payload.SortBy);

        var first = payload.Items.First();
        Assert.Equal("PharmaGo Central", first.Name);
        Assert.NotNull(first.DistanceKm);
        Assert.True(first.DistanceKm <= 0.05d);
    }

    [Fact]
    public async Task SearchNearby_WithOpenNow_ShouldReturnOnlyOpenPharmacies()
    {
        var response = await _client.GetAsync("/api/pharmacies/search?latitude=40.3777&longitude=49.8920&radiusKm=10&openNow=true");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadAsAsync<PagedResponse<NearbyPharmacyResponse>>();
        Assert.NotNull(payload);
        Assert.All(payload!.Items, pharmacy => Assert.True(pharmacy.IsOpenNow));
        Assert.Contains(payload.Items, pharmacy => pharmacy.Name == "PharmaGo Central");
    }

    [Fact]
    public async Task SearchNearby_WithMissingLongitude_ShouldReturnBadRequest()
    {
        var response = await _client.GetAsync("/api/pharmacies/search?latitude=40.3777");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SearchNearby_WithOutOfRangeLongitude_ShouldReturnBadRequest()
    {
        var response = await _client.GetAsync("/api/pharmacies/search?latitude=40.3777&longitude=249.8920");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
