using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PharmaGo.Application.Auth.Contracts;
using PharmaGo.Application.Medicines.Queries.GetConsumerMedicineFeed;
using PharmaGo.Application.Medicines.Queries.GetMedicineDetail;
using PharmaGo.Application.Reservations.Commands.CreateReservation;
using PharmaGo.IntegrationTests.Infrastructure;
using PharmaGo.Infrastructure.Persistence;

namespace PharmaGo.IntegrationTests.Medicines;

public class ConsumerMedicineFlowTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly HttpClient _client = factory.CreateClient();

    public Task InitializeAsync() => factory.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task ViewingMedicineCard_ShouldPopulateRecentFeed_ForAuthenticatedUser()
    {
        var auth = await RegisterAndAuthorizeAsync("+994551110201", "recent-flow@example.com");
        Assert.NotNull(auth);

        Guid medicineId;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            medicineId = await db.Medicines
                .Where(x => x.BrandName == "Panadol")
                .Select(x => x.Id)
                .FirstAsync();
        }

        var medicineResponse = await _client.GetAsync($"/api/medicines/{medicineId}");
        Assert.Equal(HttpStatusCode.OK, medicineResponse.StatusCode);

        var detail = await medicineResponse.Content.ReadAsAsync<MedicineDetailResponse>();
        Assert.NotNull(detail);

        var recentResponse = await _client.GetAsync("/api/me/medicines/recent");
        Assert.Equal(HttpStatusCode.OK, recentResponse.StatusCode);

        var payload = await recentResponse.Content.ReadAsAsync<IReadOnlyCollection<ConsumerMedicineFeedItemResponse>>();
        Assert.NotNull(payload);

        var item = Assert.Single(payload!);
        Assert.Equal(medicineId, item.MedicineId);
        Assert.NotNull(item.LastViewedAtUtc);
    }

    [Fact]
    public async Task Favorites_ShouldSupportAddListAndRemove()
    {
        var auth = await RegisterAndAuthorizeAsync("+994551110202", "favorites-flow@example.com");
        Assert.NotNull(auth);

        Guid medicineId;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            medicineId = await db.Medicines
                .Where(x => x.BrandName == "Nurofen")
                .Select(x => x.Id)
                .FirstAsync();
        }

        var addResponse = await _client.PostAsync($"/api/me/medicines/favorites/{medicineId}", content: null);
        Assert.Equal(HttpStatusCode.NoContent, addResponse.StatusCode);

        var favoritesResponse = await _client.GetAsync("/api/me/medicines/favorites");
        Assert.Equal(HttpStatusCode.OK, favoritesResponse.StatusCode);

        var favorites = await favoritesResponse.Content.ReadAsAsync<IReadOnlyCollection<ConsumerMedicineFeedItemResponse>>();
        Assert.NotNull(favorites);

        var favorite = Assert.Single(favorites!);
        Assert.Equal(medicineId, favorite.MedicineId);
        Assert.True(favorite.IsFavorite);
        Assert.NotNull(favorite.FavoritedAtUtc);

        var removeResponse = await _client.DeleteAsync($"/api/me/medicines/favorites/{medicineId}");
        Assert.Equal(HttpStatusCode.NoContent, removeResponse.StatusCode);

        var favoritesAfterRemoveResponse = await _client.GetAsync("/api/me/medicines/favorites");
        var favoritesAfterRemove = await favoritesAfterRemoveResponse.Content.ReadAsAsync<IReadOnlyCollection<ConsumerMedicineFeedItemResponse>>();
        Assert.NotNull(favoritesAfterRemove);
        Assert.Empty(favoritesAfterRemove!);
    }

    [Fact]
    public async Task Popular_ShouldReflectConsumerActivity_AndMarkUserFavorites()
    {
        var auth = await RegisterAndAuthorizeAsync("+994551110203", "popular-flow@example.com");
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
            medicineId = await db.Medicines
                .Where(x => x.BrandName == "Nurofen")
                .Select(x => x.Id)
                .FirstAsync();
        }

        var favoriteResponse = await _client.PostAsync($"/api/me/medicines/favorites/{medicineId}", content: null);
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

        var popularResponse = await _client.GetAsync("/api/medicines/popular?limit=5");
        Assert.Equal(HttpStatusCode.OK, popularResponse.StatusCode);

        var payload = await popularResponse.Content.ReadAsAsync<IReadOnlyCollection<ConsumerMedicineFeedItemResponse>>();
        Assert.NotNull(payload);

        var popularItems = payload!.ToList();
        Assert.NotEmpty(popularItems);
        var top = popularItems[0];
        Assert.Equal("Nurofen", top.BrandName);
        Assert.True(top.IsFavorite);
        Assert.True(top.PopularityScore.HasValue);
    }

    private async Task<AuthResponse?> RegisterAndAuthorizeAsync(string phoneNumber, string email)
    {
        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            FirstName = "Consumer",
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
