namespace PharmaGo.Application.Auth.Contracts;

public class AuthResponse
{
    public string AccessToken { get; init; } = string.Empty;
    public DateTime ExpiresAtUtc { get; init; }
    public UserProfileResponse User { get; init; } = new();
}
