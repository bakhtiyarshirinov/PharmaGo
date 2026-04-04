using System.ComponentModel.DataAnnotations;

namespace PharmaGo.Application.Auth.Contracts;

public class LoginRequest
{
    [Required]
    [MaxLength(32)]
    public string PhoneNumber { get; init; } = string.Empty;

    [Required]
    [MinLength(8)]
    [MaxLength(128)]
    public string Password { get; init; } = string.Empty;
}
