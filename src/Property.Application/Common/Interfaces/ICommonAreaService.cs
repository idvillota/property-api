using Property.Application.Common.Models;
using Property.Application.Features.Amenities.CommonAreas;

namespace Property.Application.Common.Interfaces;

public interface ICommonAreaService
{
    Task<PagedResult<CommonAreaDto>> ListAsync(ListQuery query, CancellationToken cancellationToken = default);
    Task<CommonAreaDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<CommonAreaDto> CreateAsync(CreateCommonAreaDto dto, CancellationToken cancellationToken = default);
    Task<CommonAreaDto?> UpdateAsync(Guid id, UpdateCommonAreaDto dto, CancellationToken cancellationToken = default);
    Task<bool> SoftDeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
