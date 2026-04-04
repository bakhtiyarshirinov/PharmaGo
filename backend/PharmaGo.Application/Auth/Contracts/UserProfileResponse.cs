using PharmaGo.Domain.Models.Enums;

namespace PharmaGo.Application.Auth.Contracts;

public class UserProfileResponse
{
    public Guid Id { get; init; }
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string PhoneNumber { get; init; } = string.Empty;
    public string? Email { get; init; }
    public string? TelegramUsername { get; init; }
    public string? TelegramChatId { get; init; }
    public UserRole Role { get; init; }
    public Guid? PharmacyId { get; init; }
}
