using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using PharmaGo.Application.Auth.Contracts;
using PharmaGo.IntegrationTests.Infrastructure;
using PharmaGo.Infrastructure.Persistence;

namespace PharmaGo.IntegrationTests.Auth;

public class AuthFlowTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly HttpClient _client = factory.CreateClient();

    public Task InitializeAsync() => factory.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Register_ShouldReturnAccessAndRefreshTokens()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            FirstName = "Test",
            LastName = "User",
            PhoneNumber = "+994551110001",
            Email = "test1@example.com",
            Password = "TestPassword123!"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var payload = await response.Content.ReadAsAsync<AuthResponse>();
        Assert.NotNull(payload);
        Assert.False(string.IsNullOrWhiteSpace(payload!.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(payload.RefreshToken));
        Assert.True(payload.RefreshTokenExpiresAtUtc > payload.ExpiresAtUtc);
        Assert.Equal("+994551110001", payload.User.PhoneNumber);
    }

    [Fact]
    public async Task Refresh_ShouldRotateToken_AndInvalidateOldToken()
    {
        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            FirstName = "Refresh",
            LastName = "User",
            PhoneNumber = "+994551110002",
            Email = "test2@example.com",
            Password = "TestPassword123!"
        });

        var auth = await registerResponse.Content.ReadAsAsync<AuthResponse>();
        Assert.NotNull(auth);

        var refreshResponse = await _client.PostAsJsonAsync("/api/auth/refresh", new RefreshTokenRequest
        {
            RefreshToken = auth!.RefreshToken
        });

        Assert.Equal(HttpStatusCode.OK, refreshResponse.StatusCode);

        var rotated = await refreshResponse.Content.ReadAsAsync<AuthResponse>();
        Assert.NotNull(rotated);
        Assert.NotEqual(auth.RefreshToken, rotated!.RefreshToken);

        var oldTokenResponse = await _client.PostAsJsonAsync("/api/auth/refresh", new RefreshTokenRequest
        {
            RefreshToken = auth.RefreshToken
        });

        Assert.Equal(HttpStatusCode.Unauthorized, oldTokenResponse.StatusCode);
    }

    [Fact]
    public async Task Logout_ShouldRevokeRefreshToken()
    {
        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            FirstName = "Logout",
            LastName = "User",
            PhoneNumber = "+994551110003",
            Email = "test3@example.com",
            Password = "TestPassword123!"
        });

        var auth = await registerResponse.Content.ReadAsAsync<AuthResponse>();
        Assert.NotNull(auth);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.AccessToken);

        var logoutResponse = await _client.PostAsJsonAsync("/api/auth/logout", new LogoutRequest
        {
            RefreshToken = auth.RefreshToken
        });

        Assert.Equal(HttpStatusCode.NoContent, logoutResponse.StatusCode);

        var refreshResponse = await _client.PostAsJsonAsync("/api/auth/refresh", new RefreshTokenRequest
        {
            RefreshToken = auth.RefreshToken
        });

        Assert.Equal(HttpStatusCode.Unauthorized, refreshResponse.StatusCode);
    }

    [Fact]
    public async Task RevokeAll_ShouldInvalidateAllActiveRefreshTokens()
    {
        const string phoneNumber = "+994551110004";
        const string password = "TestPassword123!";

        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            FirstName = "Revoke",
            LastName = "All",
            PhoneNumber = phoneNumber,
            Email = "test4@example.com",
            Password = password
        });

        var firstSession = await registerResponse.Content.ReadAsAsync<AuthResponse>();
        Assert.NotNull(firstSession);

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            PhoneNumber = phoneNumber,
            Password = password
        });

        var secondSession = await loginResponse.Content.ReadAsAsync<AuthResponse>();
        Assert.NotNull(secondSession);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", firstSession!.AccessToken);

        var revokeResponse = await _client.PostAsync("/api/auth/revoke-all", content: null);
        Assert.Equal(HttpStatusCode.NoContent, revokeResponse.StatusCode);

        var refreshFirst = await _client.PostAsJsonAsync("/api/auth/refresh", new RefreshTokenRequest
        {
            RefreshToken = firstSession.RefreshToken
        });
        var refreshSecond = await _client.PostAsJsonAsync("/api/auth/refresh", new RefreshTokenRequest
        {
            RefreshToken = secondSession!.RefreshToken
        });

        Assert.Equal(HttpStatusCode.Unauthorized, refreshFirst.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, refreshSecond.StatusCode);
    }

    [Fact]
    public async Task Register_ShouldRejectWeakPassword()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            FirstName = "Weak",
            LastName = "Password",
            PhoneNumber = "+994551110010",
            Email = "weak@example.com",
            Password = "alllowercase1"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal("auth_password_too_weak", problem!.Extensions["code"]?.ToString());
    }

    [Fact]
    public async Task Register_ShouldUseGenericConflict_WhenAccountAlreadyExists()
    {
        var initialResponse = await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            FirstName = "Existing",
            LastName = "User",
            PhoneNumber = "+994551110011",
            Email = "existing@example.com",
            Password = "TestPassword123!"
        });

        Assert.Equal(HttpStatusCode.Created, initialResponse.StatusCode);

        var duplicatePhoneResponse = await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            FirstName = "Duplicate",
            LastName = "Phone",
            PhoneNumber = "+994551110011",
            Email = "new-address@example.com",
            Password = "TestPassword123!"
        });

        Assert.Equal(HttpStatusCode.Conflict, duplicatePhoneResponse.StatusCode);

        var duplicatePhoneProblem = await duplicatePhoneResponse.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(duplicatePhoneProblem);
        Assert.Equal("auth_account_already_exists", duplicatePhoneProblem!.Extensions["code"]?.ToString());

        var duplicateEmailResponse = await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            FirstName = "Duplicate",
            LastName = "Email",
            PhoneNumber = "+994551110012",
            Email = "existing@example.com",
            Password = "TestPassword123!"
        });

        Assert.Equal(HttpStatusCode.Conflict, duplicateEmailResponse.StatusCode);

        var duplicateEmailProblem = await duplicateEmailResponse.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(duplicateEmailProblem);
        Assert.Equal("auth_account_already_exists", duplicateEmailProblem!.Extensions["code"]?.ToString());
    }
}
