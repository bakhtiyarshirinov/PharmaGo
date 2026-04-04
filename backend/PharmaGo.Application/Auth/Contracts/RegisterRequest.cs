namespace PharmaGo.Application.Auth.Contracts;

public class RegisterRequest
{
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string PhoneNumber { get; init; } = string.Empty;
    public string? Email { get; init; }
    public string Password { get; init; } = string.Empty;
    public string? TelegramUsername { get; init; }
    public string? TelegramChatId { get; init; }
}
