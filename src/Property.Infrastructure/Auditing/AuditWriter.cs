using System.Text.Json;
using Property.Application.Common.Interfaces;
using Property.Domain.Entities;
using Property.Infrastructure.Persistence;

namespace Property.Infrastructure.Auditing;

public sealed class AuditWriter : IAuditWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
    };

    private readonly ApplicationDbContext _db;
    private readonly ICurrentTenantContext _tenant;

    public AuditWriter(ApplicationDbContext db, ICurrentTenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task WriteAsync(
        string entityType,
        string? entityId,
        string action,
        object? before,
        object? after,
        CancellationToken cancellationToken = default)
    {
        if (_tenant.TenantId is not Guid tenantId || tenantId == Guid.Empty)
            throw new InvalidOperationException("Tenant context is required to write audit events.");

        var evt = new AuditEvent
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ActorUserId = _tenant.UserId,
            EntityType = entityType,
            EntityId = entityId,
            Action = action,
            BeforeJson = before is null ? null : JsonSerializer.Serialize(before, JsonOptions),
            AfterJson = after is null ? null : JsonSerializer.Serialize(after, JsonOptions),
            OccurredAtUtc = DateTime.UtcNow,
        };

        await _db.AuditEvents.AddAsync(evt, cancellationToken);
    }
}
