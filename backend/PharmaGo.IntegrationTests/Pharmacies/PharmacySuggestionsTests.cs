using System.Net;
using PharmaGo.Application.Pharmacies.Queries.GetNearbyPharmacyMap;
using PharmaGo.Application.Pharmacies.Queries.SuggestPharmacies;
using PharmaGo.IntegrationTests.Infrastructure;

namespace PharmaGo.IntegrationTests.Pharmacies;

public class PharmacySuggestionsTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly HttpClient _client = factory.CreateClient();

    public Task InitializeAsync() => factory.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Suggestions_ShouldReturnLightweightPharmacyMatches()
    {
        var response = await _client.GetAsync("/api/pharmacies/suggestions?q=central&limit=5");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadAsAsync<IReadOnlyCollection<PharmacySuggestionResponse>>();
        Assert.NotNull(payload);
        var suggestion = Assert.Single(payload!);
        Assert.Equal("PharmaGo Central", suggestion.Name);
    }

    [Fact]
    public async Task NearbyMap_ShouldReturnMapPins()
    {
        var response = await _client.GetAsync("/api/pharmacies/nearby-map?latitude=40.3777&longitude=49.8920&radiusKm=10&medicineQuery=pan");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadAsAsync<IReadOnlyCollection<NearbyPharmacyMapResponse>>();
        Assert.NotNull(payload);
        Assert.True(payload!.Count >= 1);

        var first = payload.First();
        Assert.Equal("PharmaGo Central", first.Name);
        Assert.True(first.DistanceKm <= 0.05d);
        Assert.True(first.MatchingMedicineCount >= 1);
    }

    [Fact]
    public async Task NearbyMap_WithoutCoordinates_ShouldReturnBadRequest()
    {
        var response = await _client.GetAsync("/api/pharmacies/nearby-map?radiusKm=10");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
