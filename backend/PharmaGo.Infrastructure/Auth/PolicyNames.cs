namespace PharmaGo.Infrastructure.Auth;

public static class PolicyNames
{
    public const string ManageUsers = "CanManageUsers";
    public const string ManagePharmacies = "CanManagePharmacies";
    public const string ManageMasterData = "CanManageMasterData";
    public const string ManageOrders = "CanManageOrders";
    public const string ManageInventory = "CanManageInventory";
    public const string ViewDashboard = "CanViewDashboard";
    public const string ReadAuditLogs = "CanReadAuditLogs";
    public const string SearchMedicines = "CanSearchMedicines";
    public const string CreateReservations = "CanCreateReservations";
    public const string ReadOwnReservations = "CanReadOwnReservations";
}
