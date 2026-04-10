using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PharmaGo.Application.Auth.Contracts;
using PharmaGo.Application.Common.Contracts;
using PharmaGo.Application.Notifications.Contracts;
using PharmaGo.Application.Reservations.Commands.CreateReservation;
using PharmaGo.Application.Reservations.Queries.GetReservation;
using PharmaGo.Domain.Models;
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

        var historyResponse = await _client.GetAsync("/api/notifications/history?page=1&pageSize=5");

        Assert.Equal(HttpStatusCode.OK, historyResponse.StatusCode);

        var history = await historyResponse.Content.ReadAsAsync<PagedResponse<NotificationHistoryItemResponse>>();
        Assert.NotNull(history);
        Assert.NotEmpty(history!.Items);

        var readyNotification = history.Items.FirstOrDefault(x =>
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

        var historyResponse = await _client.GetAsync("/api/notifications/history?page=1&pageSize=5");
        var history = await historyResponse.Content.ReadAsAsync<PagedResponse<NotificationHistoryItemResponse>>();
        var notificationId = history!.Items
            .Where(x => x.ReservationId == reservation.ReservationId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => x.NotificationId)
            .First();

        var unreadBeforeResponse = await _client.GetAsync("/api/notifications/unread");
        var unreadBefore = await unreadBeforeResponse.Content.ReadAsAsync<NotificationInboxSummaryResponse>();

        Assert.NotNull(unreadBefore);
        Assert.True(unreadBefore!.UnreadCount >= 1);

        var markReadResponse = await _client.PostAsync($"/api/notifications/{notificationId}/read", null);
        Assert.Equal(HttpStatusCode.NoContent, markReadResponse.StatusCode);

        var unreadAfterResponse = await _client.GetAsync("/api/notifications/unread");
        var unreadAfter = await unreadAfterResponse.Content.ReadAsAsync<NotificationInboxSummaryResponse>();

        Assert.NotNull(unreadAfter);
        Assert.Equal(unreadBefore.UnreadCount - 1, unreadAfter!.UnreadCount);

        var markUnreadResponse = await _client.PostAsync($"/api/notifications/{notificationId}/unread", null);
        Assert.Equal(HttpStatusCode.NoContent, markUnreadResponse.StatusCode);

        var refreshedHistoryResponse = await _client.GetAsync("/api/notifications/history?page=1&pageSize=5&unreadOnly=true");
        var refreshedHistory = await refreshedHistoryResponse.Content.ReadAsAsync<PagedResponse<NotificationHistoryItemResponse>>();
        var updatedNotification = refreshedHistory!.Items.Single(x => x.NotificationId == notificationId);

        Assert.False(updatedNotification.IsRead);
        Assert.Null(updatedNotification.ReadAtUtc);
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
        var unreadPayload = await unreadResponse.Content.ReadAsAsync<NotificationInboxSummaryResponse>();

        Assert.NotNull(unreadPayload);
        Assert.Equal(0, unreadPayload!.UnreadCount);

        otherClient.DefaultRequestHeaders.Authorization = null;
        var otherAuth = await LoginAsync(otherClient, "+994551110208", "TestPassword123!");
        Assert.NotNull(otherAuth);

        var otherUnreadResponse = await otherClient.GetAsync("/api/notifications/unread");
        var otherUnreadPayload = await otherUnreadResponse.Content.ReadAsAsync<NotificationInboxSummaryResponse>();

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

    [Fact]
    public async Task History_ShouldSupportUnreadFilters_AndBulkStatusUpdate()
    {
        var reservation = await CreateReadyReservationAsync("+994551110209", "notify-bulk@example.com");

        var historyResponse = await _client.GetAsync("/api/notifications/history?page=1&pageSize=10&sortBy=eventType&sortDirection=asc");
        Assert.Equal(HttpStatusCode.OK, historyResponse.StatusCode);

        var history = await historyResponse.Content.ReadAsAsync<PagedResponse<NotificationHistoryItemResponse>>();
        Assert.NotNull(history);
        Assert.True(history!.TotalCount >= 1);

        var targetIds = history.Items
            .Where(x => x.ReservationId == reservation.ReservationId)
            .Take(2)
            .Select(x => x.NotificationId)
            .ToArray();

        Assert.NotEmpty(targetIds);

        var bulkReadResponse = await _client.PostAsJsonAsync("/api/notifications/status/bulk", new NotificationBulkStatusUpdateRequest
        {
            NotificationIds = targetIds,
            MarkAsRead = true
        });

        Assert.Equal(HttpStatusCode.OK, bulkReadResponse.StatusCode);
        var bulkReadPayload = await bulkReadResponse.Content.ReadAsAsync<NotificationBulkStatusUpdateResponse>();
        Assert.NotNull(bulkReadPayload);
        Assert.Equal(targetIds.Length, bulkReadPayload!.RequestedCount);
        Assert.True(bulkReadPayload.UpdatedCount >= 1);

        var bulkUnreadResponse = await _client.PostAsJsonAsync("/api/notifications/status/bulk", new NotificationBulkStatusUpdateRequest
        {
            NotificationIds = targetIds,
            MarkAsRead = false
        });

        Assert.Equal(HttpStatusCode.OK, bulkUnreadResponse.StatusCode);
        var bulkUnreadPayload = await bulkUnreadResponse.Content.ReadAsAsync<NotificationBulkStatusUpdateResponse>();
        Assert.NotNull(bulkUnreadPayload);
        Assert.Equal(targetIds.Length, bulkUnreadPayload!.RequestedCount);

        var unreadOnlyResponse = await _client.GetAsync("/api/notifications/history?page=1&pageSize=10&unreadOnly=true&status=1");
        Assert.Equal(HttpStatusCode.OK, unreadOnlyResponse.StatusCode);

        var unreadOnlyPayload = await unreadOnlyResponse.Content.ReadAsAsync<PagedResponse<NotificationHistoryItemResponse>>();
        Assert.NotNull(unreadOnlyPayload);
        Assert.Contains(unreadOnlyPayload!.Items, x => targetIds.Contains(x.NotificationId));
    }

    [Fact]
    public async Task ReadEndpoints_ShouldIgnoreFailedNotifications()
    {
        var auth = await RegisterAndAuthorizeAsync(_client, "+994551110210", "notify-failed@example.com");
        Assert.NotNull(auth);

        Guid userId;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            userId = await db.Users
                .Where(x => x.Email == "notify-failed@example.com")
                .Select(x => x.Id)
                .SingleAsync();

            await db.NotificationDeliveryLogs.AddAsync(new NotificationDeliveryLog
            {
                UserId = userId,
                EventType = NotificationEventType.ReservationReadyForPickup,
                Channel = NotificationChannel.InApp,
                Status = NotificationDeliveryStatus.Failed,
                Title = "Delivery failed",
                Message = "This notification should not become readable.",
                ErrorMessage = "Simulated failure"
            });

            await db.SaveChangesAsync();
        }

        Guid failedNotificationId;
        using (var verificationScope = factory.Services.CreateScope())
        {
            var db = verificationScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            failedNotificationId = await db.NotificationDeliveryLogs
                .Where(x => x.UserId == userId && x.Status == NotificationDeliveryStatus.Failed)
                .Select(x => x.Id)
                .SingleAsync();
        }

        var markReadResponse = await _client.PostAsync($"/api/notifications/{failedNotificationId}/read", null);
        Assert.Equal(HttpStatusCode.NotFound, markReadResponse.StatusCode);

        var bulkResponse = await _client.PostAsJsonAsync("/api/notifications/status/bulk", new NotificationBulkStatusUpdateRequest
        {
            NotificationIds = [failedNotificationId],
            MarkAsRead = true
        });

        Assert.Equal(HttpStatusCode.OK, bulkResponse.StatusCode);

        using var finalScope = factory.Services.CreateScope();
        var finalDb = finalScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var failedNotification = await finalDb.NotificationDeliveryLogs
            .AsNoTracking()
            .SingleAsync(x => x.Id == failedNotificationId);

        Assert.Null(failedNotification.ReadAtUtc);
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

        var confirmResponse = await pharmacistClient.PostAsync($"/api/reservations/{reservation!.ReservationId}/confirm", null);
        Assert.Equal(HttpStatusCode.OK, confirmResponse.StatusCode);

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
