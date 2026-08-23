using Property.Application.Features.Platform.Audit;

namespace Property.Application.Common.Interfaces;

public interface IAuditQueryService
{
    Task<IReadOnlyList<AuditEventDto>> ListRecentAsync(
        int take = 50,
        CancellationToken cancellationToken = default);

    Task<AuditEventDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
