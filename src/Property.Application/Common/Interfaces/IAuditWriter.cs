namespace Property.Application.Common.Interfaces;

/// <summary>Appends AuditEvent rows on the current DbContext (no SaveChanges).</summary>
public interface IAuditWriter
{
    Task WriteAsync(
        string entityType,
        string? entityId,
        string action,
        object? before,
        object? after,
        CancellationToken cancellationToken = default);
}
