namespace PharmaGo.Application.Abstractions;

public interface ICurrentUserService
{
    Guid? UserId { get; }
    string? PhoneNumber { get; }
    string? Role { get; }
    bool IsAuthenticated { get; }
}
