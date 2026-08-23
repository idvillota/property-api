using Microsoft.EntityFrameworkCore;
using Property.Application.Common.Interfaces;
using Property.Application.Features.Platform.Audit;
using Property.Infrastructure.Persistence;

namespace Property.Infrastructure.Auditing;

public sealed class AuditQueryService : IAuditQueryService
{
    private readonly ApplicationDbContext _db;

    public AuditQueryService(ApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<AuditEventDto>> ListAsync(
        AuditEventListQuery query,
        CancellationToken cancellationToken = default)
    {
        var limit = Math.Clamp(query.Take <= 0 ? 50 : query.Take, 1, 200);
        var q = _db.AuditEvents.AsNoTracking().AsQueryable();

        if (query.ActorUserId is Guid actorId)
            q = q.Where(e => e.ActorUserId == actorId);
        if (!string.IsNullOrWhiteSpace(query.EntityType))
        {
            var type = query.EntityType.Trim();
            q = q.Where(e => e.EntityType == type);
        }

        if (!string.IsNullOrWhiteSpace(query.EntityId))
        {
            var entityId = query.EntityId.Trim();
            q = q.Where(e => e.EntityId == entityId);
        }

        if (query.FromUtc is DateTime from)
        {
            var fromUtc = from.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(from, DateTimeKind.Utc)
                : from.ToUniversalTime();
            q = q.Where(e => e.OccurredAtUtc >= fromUtc);
        }

        if (query.ToUtc is DateTime to)
        {
            var toUtc = to.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(to, DateTimeKind.Utc)
                : to.ToUniversalTime();
            q = q.Where(e => e.OccurredAtUtc <= toUtc);
        }

        return await q
            .OrderByDescending(e => e.OccurredAtUtc)
            .Take(limit)
            .Select(e => new AuditEventDto
            {
                Id = e.Id,
                ActorUserId = e.ActorUserId,
                EntityType = e.EntityType,
                EntityId = e.EntityId,
                Action = e.Action,
                BeforeJson = e.BeforeJson,
                AfterJson = e.AfterJson,
                OccurredAtUtc = e.OccurredAtUtc,
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<AuditEventDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _db.AuditEvents
            .AsNoTracking()
            .Where(e => e.Id == id)
            .Select(e => new AuditEventDto
            {
                Id = e.Id,
                ActorUserId = e.ActorUserId,
                EntityType = e.EntityType,
                EntityId = e.EntityId,
                Action = e.Action,
                BeforeJson = e.BeforeJson,
                AfterJson = e.AfterJson,
                OccurredAtUtc = e.OccurredAtUtc,
            })
            .FirstOrDefaultAsync(cancellationToken);
    }
}
