using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PharmaGo.Application.Auth.Contracts;
using PharmaGo.Application.Notifications.Contracts;
using PharmaGo.Application.Reservations.Commands.CreateReservation;
using PharmaGo.Application.Reservations.Queries.GetReservation;
using PharmaGo.Domain.Models.Enums;
using PharmaGo.IntegrationTests.Infrastructure;
using PharmaGo.Infrastructure.Persistence;

namespace PharmaGo.IntegrationTests.Notifications;

public class NotificationInboxTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly HttpClient _client = factory.CreateClient();

    public Task InitializeAsync() => factory.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task History_ShouldReturnLatestNotifications_ForAuthenticatedUser()
    {
        var reservation = await CreateReadyReservationAsync("+994551110205", "notify-history@example.com");

        var historyResponse = await _client.GetAsync("/api/notifications/history?limit=5");

        Assert.Equal(HttpStatusCode.OK, historyResponse.StatusCode);

        var history = await historyResponse.Content.ReadAsAsync<IReadOnlyCollection<NotificationHistoryItemResponse>>();
        Assert.NotNull(history);
        Assert.NotEmpty(history!);

        var readyNotification = history!.FirstOrDefault(x =>
            x.ReservationId == reservation.ReservationId &&
            x.EventType == NotificationEventType.ReservationReadyForPickup);

        Assert.NotNull(readyNotification);
        Assert.Equal(NotificationDeliveryStatus.Sent, readyNotification!.Status);
        Assert.False(readyNotification.IsRead);
    }

    [Fact]
    public async Task UnreadAndReadEndpoints_ShouldTrackReadState()
    {
        var reservation = await CreateReadyReservationAsync("+994551110206", "notify-read@example.com");

        var historyResponse = await _client.GetAsync("/api/notifications/history?limit=5");
        var history = await historyResponse.Content.ReadAsAsync<IReadOnlyCollection<NotificationHistoryItemResponse>>();
        var notificationId = history!
            .Where(x => x.ReservationId == reservation.ReservationId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => x.NotificationId)
            .First();

        var unreadBeforeResponse = await _client.GetAsync("/api/notifications/unread");
        var unreadBefore = await unreadBeforeResponse.Content.ReadAsAsync<NotificationUnreadCountResponse>();

        Assert.NotNull(unreadBefore);
        Assert.True(unreadBefore!.UnreadCount >= 1);

        var markReadResponse = await _client.PostAsync($"/api/notifications/{notificationId}/read", null);
        Assert.Equal(HttpStatusCode.NoContent, markReadResponse.StatusCode);

        var unreadAfterResponse = await _client.GetAsync("/api/notifications/unread");
        var unreadAfter = await unreadAfterResponse.Content.ReadAsAsync<NotificationUnreadCountResponse>();

        Assert.NotNull(unreadAfter);
        Assert.Equal(unreadBefore.UnreadCount - 1, unreadAfter!.UnreadCount);

        var refreshedHistoryResponse = await _client.GetAsync("/api/notifications/history?limit=5");
        var refreshedHistory = await refreshedHistoryResponse.Content.ReadAsAsync<IReadOnlyCollection<NotificationHistoryItemResponse>>();
        var updatedNotification = refreshedHistory!.Single(x => x.NotificationId == notificationId);

        Assert.True(updatedNotification.IsRead);
        Assert.NotNull(updatedNotification.ReadAtUtc);
    }

    [Fact]
    public async Task ReadAll_ShouldMarkOnlyCurrentUserNotifications()
    {
        await CreateReadyReservationAsync("+994551110207", "notify-read-all@example.com");

        var otherClient = factory.CreateClient();
        await CreateReadyReservationAsync("+994551110208", "notify-other@example.com", otherClient);

        var readAllResponse = await _client.PostAsync("/api/notifications/read-all", null);
        Assert.Equal(HttpStatusCode.NoContent, readAllResponse.StatusCode);

        var unreadResponse = await _client.GetAsync("/api/notifications/unread");
        var unreadPayload = await unreadResponse.Content.ReadAsAsync<NotificationUnreadCountResponse>();

        Assert.NotNull(unreadPayload);
        Assert.Equal(0, unreadPayload!.UnreadCount);

        otherClient.DefaultRequestHeaders.Authorization = null;
        var otherAuth = await LoginAsync(otherClient, "+994551110208", "TestPassword123!");
        Assert.NotNull(otherAuth);

        var otherUnreadResponse = await otherClient.GetAsync("/api/notifications/unread");
        var otherUnreadPayload = await otherUnreadResponse.Content.ReadAsAsync<NotificationUnreadCountResponse>();

        Assert.NotNull(otherUnreadPayload);
        Assert.True(otherUnreadPayload!.UnreadCount >= 1);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var firstUserId = await db.Users.Where(x => x.Email == "notify-read-all@example.com").Select(x => x.Id).SingleAsync();
        var secondUserId = await db.Users.Where(x => x.Email == "notify-other@example.com").Select(x => x.Id).SingleAsync();

        Assert.All(
            await db.NotificationDeliveryLogs.Where(x => x.UserId == firstUserId && x.Status == NotificationDeliveryStatus.Sent).ToListAsync(),
            log => Assert.NotNull(log.ReadAtUtc));
        Assert.Contains(
            await db.NotificationDeliveryLogs.Where(x => x.UserId == secondUserId && x.Status == NotificationDeliveryStatus.Sent).ToListAsync(),
            log => log.ReadAtUtc is null);
    }

    private async Task<ReservationResponse> CreateReadyReservationAsync(string phoneNumber, string email, HttpClient? client = null)
    {
        client ??= _client;

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var pharmacy = await db.Pharmacies.FirstAsync(x => x.Name == "PharmaGo Central");
        var stockItem = await db.StockItems.FirstAsync(x => x.PharmacyId == pharmacy.Id && x.Quantity >= 1);

        var auth = await RegisterAndAuthorizeAsync(client, phoneNumber, email);
        Assert.NotNull(auth);

        var createResponse = await client.PostAsJsonAsync("/api/reservations", new CreateReservationRequest
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

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var reservation = await createResponse.Content.ReadAsAsync<ReservationResponse>();
        Assert.NotNull(reservation);

        var pharmacistClient = factory.CreateClient();
        var pharmacistAuth = await LoginAsync(pharmacistClient, "+994500000001", "Pharmacist123!");
        Assert.NotNull(pharmacistAuth);
        pharmacistClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", pharmacistAuth!.AccessToken);

        var readyResponse = await pharmacistClient.PostAsync($"/api/reservations/{reservation!.ReservationId}/ready-for-pickup", null);
        Assert.Equal(HttpStatusCode.OK, readyResponse.StatusCode);

        return reservation;
    }

    private static async Task<AuthResponse?> RegisterAndAuthorizeAsync(HttpClient client, string phoneNumber, string email)
    {
        var registerResponse = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            FirstName = "Notify",
            LastName = "Inbox",
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

    private static async Task<AuthResponse?> LoginAsync(HttpClient client, string phoneNumber, string password)
    {
        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            PhoneNumber = phoneNumber,
            Password = password
        });

        var auth = await loginResponse.Content.ReadAsAsync<AuthResponse>();
        if (auth is not null)
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        }

        return auth;
    }
}
