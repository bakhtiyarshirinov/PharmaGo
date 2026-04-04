using System.Text.Json;
using PharmaGo.Application.Abstractions;
using PharmaGo.Domain.Models;
using PharmaGo.Infrastructure.Persistence;

namespace PharmaGo.Infrastructure.Services;

public class AuditService(ApplicationDbContext context) : IAuditService
{
    public async Task WriteAsync(
        string action,
        string entityName,
        string description,
        string? entityId = null,
        Guid? userId = null,
        Guid? pharmacyId = null,
        object? metadata = null,
        CancellationToken cancellationToken = default)
    {
        var auditLog = new AuditLog
        {
            Action = action,
            EntityName = entityName,
            EntityId = entityId,
            Description = description,
            UserId = userId,
            PharmacyId = pharmacyId,
            MetadataJson = metadata is null ? null : JsonSerializer.Serialize(metadata)
        };

        await context.AuditLogs.AddAsync(auditLog, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }
}
