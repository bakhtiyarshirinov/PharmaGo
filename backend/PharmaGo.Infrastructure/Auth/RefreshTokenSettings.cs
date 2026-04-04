namespace PharmaGo.Infrastructure.Auth;

public class RefreshTokenSettings
{
    public const string SectionName = "RefreshToken";

    public int ExpirationDays { get; init; } = 14;
}
