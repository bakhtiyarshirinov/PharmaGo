namespace PharmaGo.Domain.Models;

public class AuditLog : BaseEntity
{
    public string Action { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public string? EntityId { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? MetadataJson { get; set; }

    public Guid? UserId { get; set; }
    public AppUser? User { get; set; }

    public Guid? PharmacyId { get; set; }
    public Pharmacy? Pharmacy { get; set; }
}
