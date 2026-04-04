using PharmaGo.Domain.Models.Enums;

namespace PharmaGo.Application.Auth.Contracts;

public class UpdateUserRoleRequest
{
    public UserRole Role { get; init; }
}
