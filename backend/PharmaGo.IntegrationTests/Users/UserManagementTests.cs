using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PharmaGo.Application.Auth.Contracts;
using PharmaGo.Application.Users.Contracts;
using PharmaGo.Domain.Models.Enums;
using PharmaGo.IntegrationTests.Infrastructure;
using PharmaGo.Infrastructure.Persistence;

namespace PharmaGo.IntegrationTests.Users;

public class UserManagementTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly HttpClient _client = factory.CreateClient();

    public Task InitializeAsync() => factory.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Moderator_ShouldCreatePharmacist_ForSpecificPharmacy()
    {
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            PhoneNumber = "+994500000002",
            Password = "Moderator123!"
        });

        var auth = await loginResponse.Content.ReadAsAsync<AuthResponse>();
        Assert.NotNull(auth);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var pharmacy = await db.Pharmacies.FirstAsync(x => x.Name == "PharmaGo Central");

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.AccessToken);

        var createResponse = await _client.PostAsJsonAsync("/api/users", new CreateManagedUserRequest
        {
            FirstName = "Aysel",
            LastName = "Karimova",
            PhoneNumber = "+994551110020",
            Email = "aysel.pharmacist@example.com",
            Password = "TestPassword123!",
            Role = UserRole.Pharmacist,
            PharmacyId = pharmacy.Id
        });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var user = await createResponse.Content.ReadAsAsync<UserManagementResponse>();
        Assert.NotNull(user);
        Assert.Equal(UserRole.Pharmacist, user!.Role);
        Assert.Equal(pharmacy.Id, user.PharmacyId);
        Assert.True(user.IsActive);
    }

    [Fact]
    public async Task Moderator_SoftDelete_ShouldBlockLogin()
    {
        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            FirstName = "Delete",
            LastName = "Me",
            PhoneNumber = "+994551110021",
            Email = "softdelete@example.com",
            Password = "TestPassword123!"
        });

        var registered = await registerResponse.Content.ReadAsAsync<AuthResponse>();
        Assert.NotNull(registered);

        var moderatorLogin = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            PhoneNumber = "+994500000002",
            Password = "Moderator123!"
        });

        var moderator = await moderatorLogin.Content.ReadAsAsync<AuthResponse>();
        Assert.NotNull(moderator);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", moderator!.AccessToken);

        var deactivateResponse = await _client.DeleteAsync($"/api/users/{registered!.User.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deactivateResponse.StatusCode);

        var loginAgain = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            PhoneNumber = "+994551110021",
            Password = "TestPassword123!"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, loginAgain.StatusCode);
    }

    [Fact]
    public async Task Moderator_Restore_ShouldAllowLoginAgain()
    {
        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            FirstName = "Restore",
            LastName = "Me",
            PhoneNumber = "+994551110022",
            Email = "restore@example.com",
            Password = "TestPassword123!"
        });

        var registered = await registerResponse.Content.ReadAsAsync<AuthResponse>();
        Assert.NotNull(registered);

        var moderatorLogin = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            PhoneNumber = "+994500000002",
            Password = "Moderator123!"
        });

        var moderator = await moderatorLogin.Content.ReadAsAsync<AuthResponse>();
        Assert.NotNull(moderator);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", moderator!.AccessToken);

        var deactivateResponse = await _client.DeleteAsync($"/api/users/{registered!.User.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deactivateResponse.StatusCode);

        var restoreResponse = await _client.PostAsync($"/api/users/{registered.User.Id}/restore", null);
        Assert.Equal(HttpStatusCode.OK, restoreResponse.StatusCode);

        var loginAgain = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            PhoneNumber = "+994551110022",
            Password = "TestPassword123!"
        });

        Assert.Equal(HttpStatusCode.OK, loginAgain.StatusCode);
    }
}
