using System.Security.Claims;
using PharmaGo.Domain.Models.Enums;

namespace PharmaGo.Infrastructure.Auth;

public static class RolePermissionProvider
{
    public const string PermissionClaimType = "permission";

    public static IReadOnlyCollection<string> GetPermissions(UserRole role)
    {
        return role switch
        {
            UserRole.Moderator =>
            [
                PermissionNames.ManageUsers,
                PermissionNames.ManagePharmacies,
                PermissionNames.ManageOrders,
                PermissionNames.ManageInventory,
                PermissionNames.ViewDashboard,
                PermissionNames.ReadAuditLogs,
                PermissionNames.SearchMedicines,
                PermissionNames.CreateReservations,
                PermissionNames.ReadOwnReservations
            ],
            UserRole.Pharmacist =>
            [
                PermissionNames.ManageOrders,
                PermissionNames.ManageInventory,
                PermissionNames.ViewDashboard,
                PermissionNames.ReadAuditLogs,
                PermissionNames.SearchMedicines,
                PermissionNames.ReadOwnReservations
            ],
            _ =>
            [
                PermissionNames.SearchMedicines,
                PermissionNames.CreateReservations,
                PermissionNames.ReadOwnReservations
            ]
        };
    }

    public static IEnumerable<Claim> GetPermissionClaims(UserRole role)
    {
        return GetPermissions(role).Select(permission => new Claim(PermissionClaimType, permission));
    }
}
