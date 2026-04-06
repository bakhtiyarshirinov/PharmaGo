using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PharmaGo.Application.Auth.Contracts;
using PharmaGo.Application.Reservations.Commands.CreateReservation;
using PharmaGo.IntegrationTests.Infrastructure;
using PharmaGo.Infrastructure.Persistence;

namespace PharmaGo.IntegrationTests.Auth;

public class ApiVersioningAndAccessTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly HttpClient _client = factory.CreateClient();

    public Task InitializeAsync() => factory.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task VersionedPublicRoute_ShouldRemainAvailable()
    {
        var response = await _client.GetAsync("/api/v1/medicines/popular?limit=3");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task VersionedAuthenticatedRoute_ShouldReturnProfile()
    {
        var registerResponse = await _client.PostAsJsonAsync("/api/v1/auth/register", new RegisterRequest
        {
            FirstName = "Versioned",
            LastName = "User",
            PhoneNumber = "+994551110401",
            Email = "versioned-profile@example.com",
            Password = "TestPassword123!"
        });

        Assert.Equal(HttpStatusCode.Created, registerResponse.StatusCode);
        var auth = await registerResponse.Content.ReadAsAsync<AuthResponse>();
        Assert.NotNull(auth);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.AccessToken);

        var meResponse = await _client.GetAsync("/api/v1/auth/me");
        Assert.Equal(HttpStatusCode.OK, meResponse.StatusCode);
    }

    [Fact]
    public async Task User_ShouldBeForbidden_FromStaffEndpoints()
    {
        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            FirstName = "Plain",
            LastName = "User",
            PhoneNumber = "+994551110402",
            Email = "forbidden-user@example.com",
            Password = "TestPassword123!"
        });

        var auth = await registerResponse.Content.ReadAsAsync<AuthResponse>();
        Assert.NotNull(auth);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.AccessToken);

        await AssertForbiddenAsync("/api/v1/dashboard/summary");
        await AssertForbiddenAsync("/api/v1/stocks/alerts/low-stock");
        await AssertForbiddenAsync("/api/v1/auditlogs");
        await AssertForbiddenAsync("/api/v1/users");
    }

    [Fact]
    public async Task Pharmacist_ShouldAccessInventoryButNotModeratorEndpoints()
    {
        var loginResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest
        {
            PhoneNumber = "+994500000001",
            Password = "Pharmacist123!"
        });

        var auth = await loginResponse.Content.ReadAsAsync<AuthResponse>();
        Assert.NotNull(auth);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.AccessToken);

        var inventoryResponse = await _client.GetAsync("/api/v1/stocks/alerts/low-stock");
        Assert.Equal(HttpStatusCode.OK, inventoryResponse.StatusCode);

        await AssertForbiddenAsync("/api/v1/users");
    }

    [Fact]
    public async Task Pharmacist_ShouldNotCreateReservations()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var pharmacy = await db.Pharmacies.FirstAsync(x => x.Name == "PharmaGo Central");
        var stockItem = await db.StockItems.FirstAsync(x => x.PharmacyId == pharmacy.Id && x.Quantity >= 1);

        var loginResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest
        {
            PhoneNumber = "+994500000001",
            Password = "Pharmacist123!"
        });

        var auth = await loginResponse.Content.ReadAsAsync<AuthResponse>();
        Assert.NotNull(auth);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.AccessToken);

        var response = await _client.PostAsJsonAsync("/api/v1/reservations", new CreateReservationRequest
        {
            PharmacyId = pharmacy.Id,
            ReserveForHours = 2,
            Items =
            [
                new CreateReservationItemRequest
                {
                    MedicineId = stockItem.MedicineId,
                    Quantity = 1
                }
            ]
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var problem = await response.Content.ReadAsAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal(StatusCodes.Status403Forbidden, problem!.Status);
    }

    [Fact]
    public async Task Moderator_ShouldAccessModeratorEndpoints_ThroughVersionedRoutes()
    {
        var loginResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest
        {
            PhoneNumber = "+994500000002",
            Password = "Moderator123!"
        });

        var auth = await loginResponse.Content.ReadAsAsync<AuthResponse>();
        Assert.NotNull(auth);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.AccessToken);

        var response = await _client.GetAsync("/api/v1/users?page=1&pageSize=5");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private async Task AssertForbiddenAsync(string url)
    {
        var response = await _client.GetAsync(url);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        var problem = await response.Content.ReadAsAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal(StatusCodes.Status403Forbidden, problem!.Status);
        Assert.Equal("https://pharmago.local/problems/forbidden", problem.Type);
    }
}
