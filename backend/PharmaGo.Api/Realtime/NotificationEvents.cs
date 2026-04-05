namespace PharmaGo.Api.Realtime;

public static class NotificationEvents
{
    public const string NotificationReceived = "notification.received";
    public const string ReservationCreated = "reservation.created";
    public const string ReservationStatusChanged = "reservation.status.changed";
    public const string StockLow = "stock.low";
    public const string StockRestored = "stock.restored";
}
