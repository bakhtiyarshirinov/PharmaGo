using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PharmaGo.Application.Auth.Contracts;
using PharmaGo.IntegrationTests.Infrastructure;
using PharmaGo.Infrastructure.Persistence;

namespace PharmaGo.IntegrationTests.Dashboard;

public class DashboardAccessTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly HttpClient _client = factory.CreateClient();

    public Task InitializeAsync() => factory.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Pharmacist_ShouldNotAccessDashboardWithoutAssignedPharmacy()
    {
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            PhoneNumber = "+994500000001",
            Password = "Pharmacist123!"
        });

        var auth = await loginResponse.Content.ReadAsAsync<AuthResponse>();
        Assert.NotNull(auth);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.AccessToken);

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var pharmacist = await db.Users.FirstAsync(x => x.PhoneNumber == "+994500000001");
            pharmacist.PharmacyId = null;
            await db.SaveChangesAsync();
        }

        var summaryResponse = await _client.GetAsync("/api/dashboard/summary");
        Assert.Equal(HttpStatusCode.Forbidden, summaryResponse.StatusCode);

        var recentResponse = await _client.GetAsync("/api/dashboard/recent-reservations");
        Assert.Equal(HttpStatusCode.Forbidden, recentResponse.StatusCode);
    }
}
