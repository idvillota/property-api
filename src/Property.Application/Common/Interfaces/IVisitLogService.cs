using Property.Application.Common.Models;
using Property.Application.Features.Reception.Visits;

namespace Property.Application.Common.Interfaces;

public interface IVisitLogService
{
    Task<PagedResult<VisitLogDto>> ListAsync(
        ListQuery query,
        CancellationToken cancellationToken = default);

    Task<VisitLogDto> RegisterAsync(
        RegisterVisitDto dto,
        CancellationToken cancellationToken = default);
}
