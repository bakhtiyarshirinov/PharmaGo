using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PharmaGo.Application.Auth.Contracts;
using PharmaGo.Application.Reservations.Commands.CreateReservation;
using PharmaGo.IntegrationTests.Infrastructure;
using PharmaGo.Infrastructure.Persistence;

namespace PharmaGo.IntegrationTests.Auth;

public class RateLimitingTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    public Task InitializeAsync() => factory.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Login_ShouldReturn429_WhenAuthLimitIsExceeded()
    {
        await using var limitedFactory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["RateLimiting:AuthPermitLimit"] = "2",
                    ["RateLimiting:AuthWindowSeconds"] = "60"
                });
            });
        });

        var client = limitedFactory.CreateClient();

        for (var attempt = 0; attempt < 2; attempt++)
        {
            var response = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest
            {
                PhoneNumber = "+994500099999",
                Password = "WrongPassword!"
            });

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        var throttledResponse = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest
        {
            PhoneNumber = "+994500099999",
            Password = "WrongPassword!"
        });

        await AssertRateLimitedAsync(throttledResponse);
    }

    [Fact]
    public async Task Suggestions_ShouldReturn429_WhenSearchLimitIsExceeded()
    {
        await using var limitedFactory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["RateLimiting:SearchPermitLimit"] = "2",
                    ["RateLimiting:SearchWindowSeconds"] = "60"
                });
            });
        });

        var client = limitedFactory.CreateClient();

        for (var attempt = 0; attempt < 2; attempt++)
        {
            var response = await client.GetAsync("/api/v1/medicines/suggestions?q=par&limit=3");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        var throttledResponse = await client.GetAsync("/api/v1/medicines/suggestions?q=par&limit=3");
        await AssertRateLimitedAsync(throttledResponse);
    }

    [Fact]
    public async Task ReservationCreate_ShouldReturn429_WhenPerUserLimitIsExceeded()
    {
        await using var limitedFactory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["RateLimiting:ReservationCreatePermitLimit"] = "1",
                    ["RateLimiting:ReservationCreateWindowSeconds"] = "60"
                });
            });
        });

        var client = limitedFactory.CreateClient();

        using var scope = limitedFactory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var pharmacy = await db.Pharmacies.FirstAsync(x => x.Name == "PharmaGo Central");
        var stockItem = await db.StockItems.FirstAsync(x => x.PharmacyId == pharmacy.Id && x.Quantity >= 2);

        var registerResponse = await client.PostAsJsonAsync("/api/v1/auth/register", new RegisterRequest
        {
            FirstName = "Rate",
            LastName = "Limited",
            PhoneNumber = "+994551110501",
            Email = "rate-limit@example.com",
            Password = "TestPassword123!"
        });

        var auth = await registerResponse.Content.ReadAsAsync<AuthResponse>();
        Assert.NotNull(auth);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.AccessToken);

        var firstResponse = await client.PostAsJsonAsync("/api/v1/reservations", new CreateReservationRequest
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

        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);

        var secondResponse = await client.PostAsJsonAsync("/api/v1/reservations", new CreateReservationRequest
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

        await AssertRateLimitedAsync(secondResponse);
    }

    private static async Task AssertRateLimitedAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
        Assert.True(response.Headers.TryGetValues("Retry-After", out _));

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(JsonExtensions.SerializerOptions);
        Assert.NotNull(problem);
        Assert.Equal(429, problem!.Status);
        Assert.Equal("https://pharmago.local/problems/rate_limit_exceeded", problem.Type);
        Assert.Equal("Rate limit exceeded", problem.Title);
    }
}
