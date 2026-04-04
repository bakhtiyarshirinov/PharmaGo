namespace PharmaGo.Application.Audit.Queries.GetAuditLogs;

public class AuditLogResponse
{
    public Guid Id { get; init; }
    public string Action { get; init; } = string.Empty;
    public string EntityName { get; init; } = string.Empty;
    public string? EntityId { get; init; }
    public string Description { get; init; } = string.Empty;
    public string? MetadataJson { get; init; }
    public Guid? UserId { get; init; }
    public string? UserFullName { get; init; }
    public Guid? PharmacyId { get; init; }
    public string? PharmacyName { get; init; }
    public DateTime CreatedAtUtc { get; init; }
}
