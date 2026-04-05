using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PharmaGo.Application.Auth.Contracts;
using PharmaGo.Application.Pharmacies.Queries.GetConsumerPharmacyFeed;
using PharmaGo.Application.Pharmacies.Queries.GetPharmacyDetail;
using PharmaGo.Application.Reservations.Commands.CreateReservation;
using PharmaGo.IntegrationTests.Infrastructure;
using PharmaGo.Infrastructure.Persistence;

namespace PharmaGo.IntegrationTests.Pharmacies;

public class ConsumerPharmacyFlowTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly HttpClient _client = factory.CreateClient();

    public Task InitializeAsync() => factory.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task ViewingPharmacyCard_ShouldPopulateRecentFeed_ForAuthenticatedUser()
    {
        var auth = await RegisterAndAuthorizeAsync("+994551110301", "recent-pharmacy@example.com");
        Assert.NotNull(auth);

        Guid pharmacyId;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            pharmacyId = await db.Pharmacies
                .Where(x => x.Name == "PharmaGo Central")
                .Select(x => x.Id)
                .FirstAsync();
        }

        var pharmacyResponse = await _client.GetAsync($"/api/pharmacies/{pharmacyId}");
        Assert.Equal(HttpStatusCode.OK, pharmacyResponse.StatusCode);

        var detail = await pharmacyResponse.Content.ReadAsAsync<PharmacyDetailResponse>();
        Assert.NotNull(detail);

        var recentResponse = await _client.GetAsync("/api/me/pharmacies/recent");
        Assert.Equal(HttpStatusCode.OK, recentResponse.StatusCode);

        var payload = await recentResponse.Content.ReadAsAsync<IReadOnlyCollection<ConsumerPharmacyFeedItemResponse>>();
        Assert.NotNull(payload);

        var item = Assert.Single(payload!);
        Assert.Equal(pharmacyId, item.PharmacyId);
        Assert.NotNull(item.LastViewedAtUtc);
    }

    [Fact]
    public async Task FavoritePharmacies_ShouldSupportAddListAndRemove()
    {
        var auth = await RegisterAndAuthorizeAsync("+994551110302", "favorites-pharmacy@example.com");
        Assert.NotNull(auth);

        Guid pharmacyId;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            pharmacyId = await db.Pharmacies
                .Where(x => x.Name == "PharmaGo North")
                .Select(x => x.Id)
                .FirstAsync();
        }

        var addResponse = await _client.PostAsync($"/api/me/pharmacies/favorites/{pharmacyId}", content: null);
        Assert.Equal(HttpStatusCode.NoContent, addResponse.StatusCode);

        var favoritesResponse = await _client.GetAsync("/api/me/pharmacies/favorites");
        Assert.Equal(HttpStatusCode.OK, favoritesResponse.StatusCode);

        var favorites = await favoritesResponse.Content.ReadAsAsync<IReadOnlyCollection<ConsumerPharmacyFeedItemResponse>>();
        Assert.NotNull(favorites);

        var favorite = Assert.Single(favorites!);
        Assert.Equal(pharmacyId, favorite.PharmacyId);
        Assert.True(favorite.IsFavorite);
        Assert.NotNull(favorite.FavoritedAtUtc);

        var removeResponse = await _client.DeleteAsync($"/api/me/pharmacies/favorites/{pharmacyId}");
        Assert.Equal(HttpStatusCode.NoContent, removeResponse.StatusCode);

        var favoritesAfterRemoveResponse = await _client.GetAsync("/api/me/pharmacies/favorites");
        var favoritesAfterRemove = await favoritesAfterRemoveResponse.Content.ReadAsAsync<IReadOnlyCollection<ConsumerPharmacyFeedItemResponse>>();
        Assert.NotNull(favoritesAfterRemove);
        Assert.Empty(favoritesAfterRemove!);
    }

    [Fact]
    public async Task PopularPharmacies_ShouldReflectConsumerActivity_AndMarkFavorite()
    {
        var auth = await RegisterAndAuthorizeAsync("+994551110303", "popular-pharmacy@example.com");
        Assert.NotNull(auth);

        Guid pharmacyId;
        Guid medicineId;

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            pharmacyId = await db.Pharmacies
                .Where(x => x.Name == "PharmaGo Central")
                .Select(x => x.Id)
                .FirstAsync();
            medicineId = await db.StockItems
                .Where(x => x.PharmacyId == pharmacyId && x.Quantity > x.ReservedQuantity)
                .Select(x => x.MedicineId)
                .FirstAsync();
        }

        var favoriteResponse = await _client.PostAsync($"/api/me/pharmacies/favorites/{pharmacyId}", content: null);
        Assert.Equal(HttpStatusCode.NoContent, favoriteResponse.StatusCode);

        var reservationResponse = await _client.PostAsJsonAsync("/api/reservations", new CreateReservationRequest
        {
            PharmacyId = pharmacyId,
            ReserveForHours = 2,
            Items =
            [
                new CreateReservationItemRequest
                {
                    MedicineId = medicineId,
                    Quantity = 1
                }
            ]
        });

        Assert.Equal(HttpStatusCode.Created, reservationResponse.StatusCode);

        var popularResponse = await _client.GetAsync("/api/pharmacies/popular?limit=5");
        Assert.Equal(HttpStatusCode.OK, popularResponse.StatusCode);

        var payload = await popularResponse.Content.ReadAsAsync<IReadOnlyCollection<ConsumerPharmacyFeedItemResponse>>();
        Assert.NotNull(payload);

        var popularItems = payload!.ToList();
        Assert.NotEmpty(popularItems);
        var top = popularItems[0];
        Assert.Equal("PharmaGo Central", top.Name);
        Assert.True(top.IsFavorite);
        Assert.True(top.PopularityScore.HasValue);
    }

    private async Task<AuthResponse?> RegisterAndAuthorizeAsync(string phoneNumber, string email)
    {
        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            FirstName = "Pharmacy",
            LastName = "Flow",
            PhoneNumber = phoneNumber,
            Email = email,
            Password = "TestPassword123!"
        });

        var auth = await registerResponse.Content.ReadAsAsync<AuthResponse>();
        if (auth is not null)
        {
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        }

        return auth;
    }
}
