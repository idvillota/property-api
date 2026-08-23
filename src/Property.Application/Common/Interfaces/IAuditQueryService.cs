using Property.Application.Features.Platform.Audit;

namespace Property.Application.Common.Interfaces;

public interface IAuditQueryService
{
    Task<IReadOnlyList<AuditEventDto>> ListAsync(
        AuditEventListQuery query,
        CancellationToken cancellationToken = default);

    Task<AuditEventDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
