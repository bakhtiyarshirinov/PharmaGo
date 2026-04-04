namespace PharmaGo.Infrastructure.Auth;

public static class PermissionNames
{
    public const string ManageUsers = "users.manage";
    public const string ManageOrders = "orders.manage";
    public const string ManageInventory = "inventory.manage";
    public const string ViewDashboard = "dashboard.view";
    public const string ReadAuditLogs = "audit.read";
    public const string SearchMedicines = "medicines.search";
    public const string CreateReservations = "reservations.create";
    public const string ReadOwnReservations = "reservations.read.own";
}
