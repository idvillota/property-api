using Property.Application.Common.Models;
using Property.Application.Features.Structure.Buildings;

namespace Property.Application.Common.Interfaces;

public interface IBuildingService
{
    Task<PagedResult<BuildingDto>> ListAsync(ListQuery query, CancellationToken cancellationToken = default);
    Task<BuildingDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<BuildingDto> CreateAsync(CreateBuildingDto dto, CancellationToken cancellationToken = default);
    Task<BuildingDto?> UpdateAsync(Guid id, UpdateBuildingDto dto, CancellationToken cancellationToken = default);
    Task<bool> SoftDeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
