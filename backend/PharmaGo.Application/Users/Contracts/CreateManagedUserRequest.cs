using System.ComponentModel.DataAnnotations;
using PharmaGo.Domain.Models.Enums;

namespace PharmaGo.Application.Users.Contracts;

public class CreateManagedUserRequest
{
    [Required]
    [MaxLength(100)]
    public string FirstName { get; init; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string LastName { get; init; } = string.Empty;

    [Required]
    [MaxLength(32)]
    public string PhoneNumber { get; init; } = string.Empty;

    [EmailAddress]
    [MaxLength(256)]
    public string? Email { get; init; }

    [Required]
    [MinLength(8)]
    [MaxLength(128)]
    public string Password { get; init; } = string.Empty;

    [MaxLength(100)]
    public string? TelegramUsername { get; init; }

    [MaxLength(100)]
    public string? TelegramChatId { get; init; }

    [Required]
    public UserRole Role { get; init; }

    public Guid? PharmacyId { get; init; }
}
