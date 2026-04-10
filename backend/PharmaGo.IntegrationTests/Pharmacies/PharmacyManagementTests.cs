using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PharmaGo.Application.Auth.Contracts;
using PharmaGo.Application.Common.Contracts;
using PharmaGo.Application.Pharmacies.Contracts;
using PharmaGo.IntegrationTests.Infrastructure;
using PharmaGo.Infrastructure.Persistence;

namespace PharmaGo.IntegrationTests.Pharmacies;

public class PharmacyManagementTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly HttpClient _client = factory.CreateClient();

    public Task InitializeAsync() => factory.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Moderator_ShouldCreateManagedPharmacy_WithNormalizedSchedule()
    {
        var moderator = await LoginAsync("+994500000002", "Moderator123!");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", moderator!.AccessToken);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var chainId = await db.PharmacyChains.Select(x => x.Id).FirstAsync();

        var createResponse = await _client.PostAsJsonAsync("/api/v1/admin/pharmacies", new CreateManagedPharmacyRequest
        {
            Name = "PharmaGo Yasamal",
            Address = "Shafayat Mehdiyev 88",
            City = "Baku",
            Region = "Yasamal",
            PhoneNumber = "+994501006677",
            LocationLatitude = 40.383821m,
            LocationLongitude = 49.811375m,
            IsOpen24Hours = false,
            OpeningHoursJson = """
            {
              "weekly": [
                { "day": "Monday", "open": "09:00", "close": "22:00" },
                { "day": "Saturday", "open": "10:00", "close": "20:00" }
              ]
            }
            """,
            SupportsReservations = true,
            HasDelivery = true,
            PharmacyChainId = chainId
        });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var payload = await createResponse.Content.ReadAsAsync<ManagedPharmacyResponse>();
        Assert.NotNull(payload);
        Assert.Equal("PharmaGo Yasamal", payload!.Name);
        Assert.Contains("\"timeZone\":\"Asia/Baku\"", payload.OpeningHoursJson);
        Assert.Contains("\"day\":\"mon\"", payload.OpeningHoursJson);

        var listResponse = await _client.GetAsync("/api/admin/pharmacies?search=Yasamal");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);

        var listPayload = await listResponse.Content.ReadAsAsync<PagedResponse<ManagedPharmacyResponse>>();
        Assert.NotNull(listPayload);
        Assert.Single(listPayload!.Items);
    }

    [Fact]
    public async Task Moderator_ShouldUpdatePharmacySchedule_AndSoftDeleteRestore()
    {
        var moderator = await LoginAsync("+994500000002", "Moderator123!");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", moderator!.AccessToken);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var pharmacyId = await db.Pharmacies
            .Where(x => x.Name == "PharmaGo North")
            .Select(x => x.Id)
            .FirstAsync();

        var scheduleResponse = await _client.PutAsJsonAsync($"/api/admin/pharmacies/{pharmacyId}/schedule", new UpdatePharmacyScheduleRequest
        {
            IsOpen24Hours = false,
            OpeningHoursJson = """
            {
              "timeZone": "Asia/Baku",
              "weekly": [
                { "day": "Mon", "open": "08:30", "close": "22:30" },
                { "day": "Tue", "open": "08:30", "close": "22:30" },
                { "day": "Wed", "open": "08:30", "close": "22:30" }
              ]
            }
            """
        });

        Assert.Equal(HttpStatusCode.OK, scheduleResponse.StatusCode);

        var updated = await scheduleResponse.Content.ReadAsAsync<ManagedPharmacyResponse>();
        Assert.NotNull(updated);
        Assert.Contains("\"open\":\"08:30\"", updated!.OpeningHoursJson);

        var deactivateResponse = await _client.DeleteAsync($"/api/admin/pharmacies/{pharmacyId}");
        Assert.Equal(HttpStatusCode.NoContent, deactivateResponse.StatusCode);

        var inactiveResponse = await _client.GetAsync($"/api/admin/pharmacies/{pharmacyId}");
        var inactivePayload = await inactiveResponse.Content.ReadAsAsync<ManagedPharmacyResponse>();
        Assert.NotNull(inactivePayload);
        Assert.False(inactivePayload!.IsActive);

        var restoreResponse = await _client.PostAsync($"/api/v1/admin/pharmacies/{pharmacyId}/restore", null);
        Assert.Equal(HttpStatusCode.OK, restoreResponse.StatusCode);

        var restoredPayload = await restoreResponse.Content.ReadAsAsync<ManagedPharmacyResponse>();
        Assert.NotNull(restoredPayload);
        Assert.True(restoredPayload!.IsActive);
    }

    [Fact]
    public async Task Moderator_ShouldRejectInvalidSchedule()
    {
        var moderator = await LoginAsync("+994500000002", "Moderator123!");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", moderator!.AccessToken);

        var response = await _client.PostAsJsonAsync("/api/admin/pharmacies", new CreateManagedPharmacyRequest
        {
            Name = "Broken Schedule Pharmacy",
            Address = "Test 1",
            City = "Baku",
            IsOpen24Hours = false,
            OpeningHoursJson = """
            {
              "weekly": [
                { "day": "Mon", "open": "09:00", "close": "09:00" }
              ]
            }
            """
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("pharmacy_schedule_invalid", document.RootElement.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Pharmacist_ShouldNotAccessPharmacyAdminEndpoints()
    {
        var pharmacist = await LoginAsync("+994500000001", "Pharmacist123!");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", pharmacist!.AccessToken);

        var response = await _client.GetAsync("/api/admin/pharmacies");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Moderator_ShouldRejectTooShortPhoneNumber_WhenCreatingManagedPharmacy()
    {
        var moderator = await LoginAsync("+994500000002", "Moderator123!");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", moderator!.AccessToken);

        var response = await _client.PostAsJsonAsync("/api/admin/pharmacies", new CreateManagedPharmacyRequest
        {
            Name = "Short Phone Pharmacy",
            Address = "Test 2",
            City = "Baku",
            PhoneNumber = "123",
            IsOpen24Hours = true
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal("pharmacy_phone_invalid", problem!.Extensions["code"]?.ToString());
    }

    private async Task<AuthResponse?> LoginAsync(string phoneNumber, string password)
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            PhoneNumber = phoneNumber,
            Password = password
        });

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsAsync<AuthResponse>();
    }
}
