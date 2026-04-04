using System.ComponentModel.DataAnnotations;

namespace PharmaGo.Application.Auth.Contracts;

public class LogoutRequest
{
    [Required]
    public string RefreshToken { get; init; } = string.Empty;
}
