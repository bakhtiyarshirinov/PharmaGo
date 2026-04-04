using PharmaGo.Domain.Models.Enums;

namespace PharmaGo.Application.Users.Contracts;

public class UserManagementResponse
{
    public Guid Id { get; init; }
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string PhoneNumber { get; init; } = string.Empty;
    public string? Email { get; init; }
    public string? TelegramUsername { get; init; }
    public string? TelegramChatId { get; init; }
    public UserRole Role { get; init; }
    public bool IsActive { get; init; }
    public Guid? PharmacyId { get; init; }
    public string? PharmacyName { get; init; }
    public DateTime CreatedAtUtc { get; init; }
    public DateTime? UpdatedAtUtc { get; init; }
}
