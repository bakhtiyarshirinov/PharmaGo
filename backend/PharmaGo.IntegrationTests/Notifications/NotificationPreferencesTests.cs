using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using PharmaGo.Application.Auth.Contracts;
using PharmaGo.Application.Notifications.Contracts;
using PharmaGo.IntegrationTests.Infrastructure;
using PharmaGo.Infrastructure.Persistence;

namespace PharmaGo.IntegrationTests.Notifications;

public class NotificationPreferencesTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly HttpClient _client = factory.CreateClient();

    public Task InitializeAsync() => factory.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task GetPreferences_ShouldReturnDefaults_ForAuthenticatedUser()
    {
        var auth = await RegisterAndAuthorizeAsync("+994551110201", "notify-defaults@example.com");
        Assert.NotNull(auth);

        var response = await _client.GetAsync("/api/notifications/preferences");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<NotificationPreferencesResponse>();
        Assert.NotNull(payload);
        Assert.True(payload!.InAppEnabled);
        Assert.True(payload.ReservationReadyEnabled);
        Assert.False(payload.TelegramLinked);
    }

    [Fact]
    public async Task UpdatePreferences_ShouldPersistChanges()
    {
        var auth = await RegisterAndAuthorizeAsync("+994551110202", "notify-update@example.com");
        Assert.NotNull(auth);

        var updateResponse = await _client.PutAsJsonAsync("/api/notifications/preferences", new UpdateNotificationPreferencesRequest
        {
            InAppEnabled = true,
            TelegramEnabled = true,
            ReservationConfirmedEnabled = false,
            ReservationReadyEnabled = true,
            ReservationCancelledEnabled = false,
            ReservationExpiredEnabled = true,
            ReservationExpiringSoonEnabled = false
        });

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        var payload = await updateResponse.Content.ReadFromJsonAsync<NotificationPreferencesResponse>();
        Assert.NotNull(payload);
        Assert.True(payload!.TelegramEnabled);
        Assert.False(payload.ReservationConfirmedEnabled);
        Assert.False(payload.ReservationCancelledEnabled);
        Assert.False(payload.ReservationExpiringSoonEnabled);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userId = db.Users.Where(x => x.Email == "notify-update@example.com").Select(x => x.Id).Single();
        var preference = db.NotificationPreferences.Single(x => x.UserId == userId);

        Assert.True(preference.TelegramEnabled);
        Assert.False(preference.ReservationConfirmedEnabled);
        Assert.False(preference.ReservationCancelledEnabled);
        Assert.False(preference.ReservationExpiringSoonEnabled);
    }

    private async Task<AuthResponse?> RegisterAndAuthorizeAsync(string phoneNumber, string email)
    {
        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
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
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        }

        return auth;
    }
}
