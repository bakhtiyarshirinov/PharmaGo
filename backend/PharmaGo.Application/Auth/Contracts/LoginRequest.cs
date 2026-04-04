namespace PharmaGo.Application.Auth.Contracts;

public class LoginRequest
{
    public string PhoneNumber { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
}
