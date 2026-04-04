using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PharmaGo.Application.Auth.Contracts;
using PharmaGo.Application.Reservations.Commands.CreateReservation;
using PharmaGo.Application.Reservations.Commands.UpdateReservationStatus;
using PharmaGo.Application.Reservations.Queries.GetReservation;
using PharmaGo.Application.Stocks.Queries.GetRestockSuggestions;
using PharmaGo.Domain.Models.Enums;
using PharmaGo.IntegrationTests.Infrastructure;
using PharmaGo.Infrastructure.Persistence;

namespace PharmaGo.IntegrationTests.Reservations;

public class ReservationFlowTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly HttpClient _client = factory.CreateClient();

    public Task InitializeAsync() => factory.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task CreateReservation_ShouldReserveStock_ForAuthenticatedUser()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var pharmacy = await db.Pharmacies.FirstAsync(x => x.Name == "PharmaGo Central");
        var stockItem = await db.StockItems.Include(x => x.Medicine).FirstAsync(x => x.PharmacyId == pharmacy.Id && x.Quantity > 0);

        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            FirstName = "Reserve",
            LastName = "User",
            PhoneNumber = "+994551110010",
            Email = "reserve@example.com",
            Password = "TestPassword123!"
        });

        var auth = await registerResponse.Content.ReadAsAsync<AuthResponse>();
        Assert.NotNull(auth);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.AccessToken);

        var reservationResponse = await _client.PostAsJsonAsync("/api/reservations", new CreateReservationRequest
        {
            PharmacyId = pharmacy.Id,
            ReserveForHours = 2,
            Notes = "Integration reservation",
            Items =
            [
                new CreateReservationItemRequest
                {
                    MedicineId = stockItem.MedicineId,
                    Quantity = 2
                }
            ]
        });

        Assert.Equal(HttpStatusCode.Created, reservationResponse.StatusCode);

        var payload = await reservationResponse.Content.ReadAsAsync<ReservationResponse>();
        Assert.NotNull(payload);
        Assert.Equal(ReservationStatus.Confirmed, payload!.Status);
        Assert.Single(payload.Items);

        var updatedStock = await db.StockItems.AsNoTracking().FirstAsync(x => x.Id == stockItem.Id);
        Assert.Equal(2, updatedStock.ReservedQuantity);
    }

    [Fact]
    public async Task Pharmacist_ShouldCompleteReservation_AndDeductStock()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var pharmacy = await db.Pharmacies.FirstAsync(x => x.Name == "PharmaGo Central");
        var stockItem = await db.StockItems.Include(x => x.Medicine).FirstAsync(x => x.PharmacyId == pharmacy.Id && x.Quantity >= 3);
        var initialQuantity = stockItem.Quantity;

        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            FirstName = "Flow",
            LastName = "User",
            PhoneNumber = "+994551110011",
            Email = "flow@example.com",
            Password = "TestPassword123!"
        });

        var userAuth = await registerResponse.Content.ReadAsAsync<AuthResponse>();
        Assert.NotNull(userAuth);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", userAuth!.AccessToken);

        var createResponse = await _client.PostAsJsonAsync("/api/reservations", new CreateReservationRequest
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

        var reservation = await createResponse.Content.ReadAsAsync<ReservationResponse>();
        Assert.NotNull(reservation);

        var pharmacistLogin = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            PhoneNumber = "+994500000001",
            Password = "Pharmacist123!"
        });

        var pharmacistAuth = await pharmacistLogin.Content.ReadAsAsync<AuthResponse>();
        Assert.NotNull(pharmacistAuth);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", pharmacistAuth!.AccessToken);

        var readyResponse = await _client.PatchAsJsonAsync(
            $"/api/reservations/{reservation!.ReservationId}/status",
            new UpdateReservationStatusRequest { Status = ReservationStatus.ReadyForPickup });

        Assert.Equal(HttpStatusCode.OK, readyResponse.StatusCode);

        var completeResponse = await _client.PatchAsJsonAsync(
            $"/api/reservations/{reservation.ReservationId}/status",
            new UpdateReservationStatusRequest { Status = ReservationStatus.Completed });

        Assert.Equal(HttpStatusCode.OK, completeResponse.StatusCode);

        var updatedStock = await db.StockItems.AsNoTracking().FirstAsync(x => x.Id == stockItem.Id);
        var updatedReservation = await db.Reservations.AsNoTracking().FirstAsync(x => x.Id == reservation.ReservationId);

        Assert.Equal(initialQuantity - 1, updatedStock.Quantity);
        Assert.Equal(0, updatedStock.ReservedQuantity);
        Assert.Equal(ReservationStatus.Completed, updatedReservation.Status);
    }

    [Fact]
    public async Task Pharmacist_ShouldGetRestockSuggestions_ForOwnPharmacy()
    {
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var stockItem = await db.StockItems
                .Include(x => x.Pharmacy)
                .Include(x => x.Medicine)
                .FirstAsync(x => x.Pharmacy!.Name == "PharmaGo Central" && x.Medicine!.BrandName == "Panadol");

            stockItem.Quantity = 12;
            stockItem.ReservedQuantity = 0;
            stockItem.ReorderLevel = 20;
            await db.SaveChangesAsync();
        }

        var pharmacistLogin = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            PhoneNumber = "+994500000001",
            Password = "Pharmacist123!"
        });

        var pharmacistAuth = await pharmacistLogin.Content.ReadAsAsync<AuthResponse>();
        Assert.NotNull(pharmacistAuth);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", pharmacistAuth!.AccessToken);

        var response = await _client.GetAsync("/api/stocks/alerts/restock-suggestions");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadAsAsync<IReadOnlyCollection<RestockSuggestionResponse>>();
        Assert.NotNull(payload);

        var suggestion = Assert.Single(payload!);
        Assert.Equal("PharmaGo Central", suggestion.PharmacyName);
        Assert.Equal("Panadol", suggestion.MedicineName);
        Assert.Equal(8, suggestion.Deficit);
        Assert.Equal(50, suggestion.SuggestedOrderQuantity);
        Assert.Equal("PharmaGo Main Depot", suggestion.DepotName);
        Assert.Equal(50m, suggestion.EstimatedWholesaleCost);
    }

    [Fact]
    public async Task ConcurrentReservationRequests_ShouldNotOversellStock()
    {
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var stockItem = await db.StockItems
                .Include(x => x.Pharmacy)
                .Include(x => x.Medicine)
                .FirstAsync(x => x.Pharmacy!.Name == "PharmaGo Central" && x.Medicine!.BrandName == "Panadol");

            stockItem.Quantity = 100;
            stockItem.ReservedQuantity = 0;
            stockItem.LastStockUpdatedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync();
        }

        var client1 = factory.CreateClient();
        var client2 = factory.CreateClient();

        var auth1 = await RegisterAndAuthorizeAsync(client1, "+994551110101", "race1@example.com");
        var auth2 = await RegisterAndAuthorizeAsync(client2, "+994551110102", "race2@example.com");

        Assert.NotNull(auth1);
        Assert.NotNull(auth2);

        using var scope2 = factory.Services.CreateScope();
        var db2 = scope2.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var pharmacyId = await db2.Pharmacies.Where(x => x.Name == "PharmaGo Central").Select(x => x.Id).FirstAsync();
        var medicineId = await db2.Medicines.Where(x => x.BrandName == "Panadol").Select(x => x.Id).FirstAsync();

        var request = new CreateReservationRequest
        {
            PharmacyId = pharmacyId,
            ReserveForHours = 2,
            Items =
            [
                new CreateReservationItemRequest
                {
                    MedicineId = medicineId,
                    Quantity = 80
                }
            ]
        };

        var task1 = client1.PostAsJsonAsync("/api/reservations", request);
        var task2 = client2.PostAsJsonAsync("/api/reservations", request);

        var responses = await Task.WhenAll(task1, task2);
        var createdCount = responses.Count(x => x.StatusCode == HttpStatusCode.Created);
        var rejectedCount = responses.Count(x =>
            x.StatusCode == HttpStatusCode.Conflict ||
            x.StatusCode == HttpStatusCode.BadRequest);

        Assert.Equal(1, createdCount);
        Assert.Equal(1, rejectedCount);

        var stockAfter = await db2.StockItems.AsNoTracking()
            .FirstAsync(x => x.PharmacyId == pharmacyId && x.MedicineId == medicineId);
        var reservations = await db2.Reservations.AsNoTracking()
            .Where(x => x.PharmacyId == pharmacyId && x.CustomerId != Guid.Empty)
            .ToListAsync();

        Assert.Equal(80, stockAfter.ReservedQuantity);
        Assert.Equal(100, stockAfter.Quantity);
        Assert.Single(reservations, x => x.Status == ReservationStatus.Confirmed);
    }

    private static async Task<AuthResponse?> RegisterAndAuthorizeAsync(HttpClient client, string phoneNumber, string email)
    {
        var registerResponse = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            FirstName = "Race",
            LastName = "User",
            PhoneNumber = phoneNumber,
            Email = email,
            Password = "TestPassword123!"
        });

        var auth = await registerResponse.Content.ReadAsAsync<AuthResponse>();
        if (auth is not null)
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        }

        return auth;
    }
}
