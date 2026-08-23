using Microsoft.EntityFrameworkCore;
using Property.Application.Common.Interfaces;
using Property.Application.Features.Platform.Audit;
using Property.Infrastructure.Persistence;

namespace Property.Infrastructure.Auditing;

public sealed class AuditQueryService : IAuditQueryService
{
    private readonly ApplicationDbContext _db;

    public AuditQueryService(ApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<AuditEventDto>> ListRecentAsync(
        int take = 50,
        CancellationToken cancellationToken = default)
    {
        var limit = Math.Clamp(take, 1, 200);
        return await _db.AuditEvents
            .AsNoTracking()
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
