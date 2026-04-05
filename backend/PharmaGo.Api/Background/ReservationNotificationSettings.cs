namespace PharmaGo.Api.Background;

public class ReservationNotificationSettings
{
    public const string SectionName = "ReservationNotifications";

    public int PollingIntervalSeconds { get; set; } = 300;
    public int ExpiringSoonLeadMinutes { get; set; } = 30;
}
