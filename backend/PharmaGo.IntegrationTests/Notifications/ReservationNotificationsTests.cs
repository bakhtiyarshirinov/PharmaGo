using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PharmaGo.Application.Abstractions;
using PharmaGo.Application.Auth.Contracts;
using PharmaGo.Application.Notifications.Contracts;
using PharmaGo.Application.Reservations.Commands.CreateReservation;
using PharmaGo.Application.Reservations.Queries.GetReservation;
using PharmaGo.Domain.Models.Enums;
using PharmaGo.IntegrationTests.Infrastructure;
using PharmaGo.Infrastructure.Persistence;

namespace PharmaGo.IntegrationTests.Notifications;

public class ReservationNotificationsTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly HttpClient _client = factory.CreateClient();

    public Task InitializeAsync() => factory.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task ReadyForPickup_ShouldWriteDeliveryLog_ForCustomer()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var pharmacy = await db.Pharmacies.FirstAsync(x => x.Name == "PharmaGo Central");
        var stockItem = await db.StockItems.FirstAsync(x => x.PharmacyId == pharmacy.Id && x.Quantity >= 1);

        var userAuth = await RegisterAndAuthorizeAsync(_client, "+994551110203", "notify-ready@example.com");
        Assert.NotNull(userAuth);

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

        var pharmacistClient = factory.CreateClient();
        var pharmacistLogin = await pharmacistClient.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            PhoneNumber = "+994500000001",
            Password = "Pharmacist123!"
        });

        var pharmacistAuth = await pharmacistLogin.Content.ReadAsAsync<AuthResponse>();
        Assert.NotNull(pharmacistAuth);
        pharmacistClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", pharmacistAuth!.AccessToken);

        var readyResponse = await pharmacistClient.PostAsync($"/api/reservations/{reservation!.ReservationId}/ready-for-pickup", null);
        Assert.Equal(HttpStatusCode.OK, readyResponse.StatusCode);

        var readyLog = await db.NotificationDeliveryLogs
            .AsNoTracking()
            .Where(x => x.ReservationId == reservation.ReservationId && x.EventType == NotificationEventType.ReservationReadyForPickup)
            .OrderByDescending(x => x.CreatedAtUtc)
            .FirstOrDefaultAsync();

        Assert.NotNull(readyLog);
        Assert.Equal(NotificationDeliveryStatus.Sent, readyLog!.Status);
        Assert.Equal(NotificationChannel.InApp, readyLog.Channel);
    }

    [Fact]
    public async Task ExpiringSoonDispatch_ShouldHonorPreferences_AndSendOnlyOnce()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var pharmacy = await db.Pharmacies.FirstAsync(x => x.Name == "PharmaGo Central");
        var stockItem = await db.StockItems.FirstAsync(x => x.PharmacyId == pharmacy.Id && x.Quantity >= 1);

        var auth = await RegisterAndAuthorizeAsync(_client, "+994551110204", "notify-expiring@example.com");
        Assert.NotNull(auth);

        await _client.PutAsJsonAsync("/api/notifications/preferences", new UpdateNotificationPreferencesRequest
        {
            InAppEnabled = true,
            TelegramEnabled = false,
            ReservationConfirmedEnabled = true,
            ReservationReadyEnabled = true,
            ReservationCancelledEnabled = true,
            ReservationExpiredEnabled = true,
            ReservationExpiringSoonEnabled = true
        });

        var createResponse = await _client.PostAsJsonAsync("/api/reservations", new CreateReservationRequest
        {
            PharmacyId = pharmacy.Id,
            ReserveForHours = 1,
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

        var trackedReservation = await db.Reservations.FirstAsync(x => x.Id == reservation!.ReservationId);
        trackedReservation.ReservedUntilUtc = DateTime.UtcNow.AddMinutes(15);
        await db.SaveChangesAsync();

        var notificationService = scope.ServiceProvider.GetRequiredService<IReservationNotificationService>();
        var firstDispatch = await notificationService.DispatchExpiringSoonNotificationsAsync();
        var secondDispatch = await notificationService.DispatchExpiringSoonNotificationsAsync();

        Assert.Equal(1, firstDispatch);
        Assert.Equal(0, secondDispatch);

        var logs = await db.NotificationDeliveryLogs
            .AsNoTracking()
            .Where(x => x.ReservationId == reservation.ReservationId && x.EventType == NotificationEventType.ReservationExpiringSoon)
            .ToListAsync();

        Assert.Single(logs);
        Assert.Equal(NotificationDeliveryStatus.Sent, logs[0].Status);
    }

    private static async Task<AuthResponse?> RegisterAndAuthorizeAsync(HttpClient client, string phoneNumber, string email)
    {
        var registerResponse = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            FirstName = "Notify",
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
