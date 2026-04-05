using System.Net;
using PharmaGo.Application.Medicines.Queries.SearchMedicines;
using PharmaGo.IntegrationTests.Infrastructure;

namespace PharmaGo.IntegrationTests.Medicines;

public class MedicineSuggestionsTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly HttpClient _client = factory.CreateClient();

    public Task InitializeAsync() => factory.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Suggestions_ShouldReturnLightweightMedicineMatches()
    {
        var response = await _client.GetAsync("/api/medicines/suggestions?q=pan&limit=5");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadAsAsync<IReadOnlyCollection<MedicineSuggestionResponse>>();
        Assert.NotNull(payload);
        var suggestion = Assert.Single(payload!);
        Assert.Equal("Panadol", suggestion.BrandName);
        Assert.True(suggestion.PharmacyCount >= 1);
    }

    [Fact]
    public async Task Suggestions_WithoutQuery_ShouldReturnBadRequest()
    {
        var response = await _client.GetAsync("/api/medicines/suggestions?q=");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
