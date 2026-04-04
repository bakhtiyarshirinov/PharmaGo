namespace PharmaGo.Application.Abstractions;

public interface IAuditService
{
    Task WriteAsync(
        string action,
        string entityName,
        string description,
        string? entityId = null,
        Guid? userId = null,
        Guid? pharmacyId = null,
        object? metadata = null,
        CancellationToken cancellationToken = default);
}
