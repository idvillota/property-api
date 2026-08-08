using Property.Application.Common.Models;
using Property.Application.Features.Structure.Floors;

namespace Property.Application.Common.Interfaces;

public interface IFloorService
{
    Task<PagedResult<FloorDto>> ListAsync(ListQuery query, CancellationToken cancellationToken = default);
    Task<FloorDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<FloorDto> CreateAsync(CreateFloorDto dto, CancellationToken cancellationToken = default);
    Task<FloorDto?> UpdateAsync(Guid id, UpdateFloorDto dto, CancellationToken cancellationToken = default);
    Task<bool> SoftDeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
