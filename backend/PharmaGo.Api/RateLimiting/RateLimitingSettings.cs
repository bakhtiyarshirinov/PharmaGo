namespace PharmaGo.Api.RateLimiting;

public class RateLimitingSettings
{
    public const string SectionName = "RateLimiting";

    public int AuthPermitLimit { get; set; } = 20;
    public int AuthWindowSeconds { get; set; } = 60;

    public int SearchPermitLimit { get; set; } = 120;
    public int SearchWindowSeconds { get; set; } = 60;

    public int ReservationCreatePermitLimit { get; set; } = 10;
    public int ReservationCreateWindowSeconds { get; set; } = 60;
}
