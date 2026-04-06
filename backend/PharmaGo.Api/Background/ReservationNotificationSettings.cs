namespace PharmaGo.Api.Background;

public class ReservationNotificationSettings
{
    public const string SectionName = "ReservationNotifications";

    public int PollingIntervalSeconds { get; set; } = 300;
    public int[] ExpiringSoonReminderMinutes { get; set; } = [45, 30, 15];
}
