namespace PharmaGo.Api.Background;

public class ReservationExpirationSettings
{
    public const string SectionName = "ReservationExpiration";

    public int PollingIntervalSeconds { get; init; } = 60;
}
