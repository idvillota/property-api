using Property.Application.Common.Models;
using Property.Application.Features.Reception.Visits;

namespace Property.Application.Common.Interfaces;

public interface IVisitLogService
{
    Task<PagedResult<VisitLogDto>> ListAsync(
        ListQuery query,
        CancellationToken cancellationToken = default);

    Task<VisitLogDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<VisitLogDto> RegisterAsync(
        RegisterVisitDto dto,
        CancellationToken cancellationToken = default);

    Task<VisitLogDto> ApproveAsync(
        Guid visitId,
        Guid personId,
        Guid? userId = null,
        CancellationToken cancellationToken = default);

    Task<VisitLogDto> RejectAsync(
        Guid visitId,
        Guid personId,
        Guid? userId = null,
        CancellationToken cancellationToken = default);

    Task<VisitLogDto> OverrideApproveAsync(
        Guid visitId,
        Guid userId,
        string reason,
        CancellationToken cancellationToken = default);

    /// <summary>Marks overdue PendingApproval visits as Expired. Returns count updated.</summary>
    Task<int> ExpireOverdueAsync(CancellationToken cancellationToken = default);
}
