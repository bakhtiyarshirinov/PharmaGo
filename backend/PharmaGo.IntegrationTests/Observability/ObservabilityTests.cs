using System.Net;
using System.Text.Json;
using PharmaGo.IntegrationTests.Infrastructure;

namespace PharmaGo.IntegrationTests.Observability;

public class ObservabilityTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly HttpClient _client = factory.CreateClient();

    public Task InitializeAsync() => factory.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task HealthEndpoints_ShouldReturnJsonPayloads()
    {
        var liveResponse = await _client.GetAsync("/health/live");
        var readyResponse = await _client.GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.OK, liveResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, readyResponse.StatusCode);

        using var liveDocument = JsonDocument.Parse(await liveResponse.Content.ReadAsStringAsync());
        using var readyDocument = JsonDocument.Parse(await readyResponse.Content.ReadAsStringAsync());

        Assert.Equal("Healthy", liveDocument.RootElement.GetProperty("status").GetString());
        Assert.True(readyDocument.RootElement.TryGetProperty("checks", out var checks));
        Assert.True(checks.TryGetProperty("database", out _));
        Assert.True(checks.TryGetProperty("background_workers", out _));
    }
}
