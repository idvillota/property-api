using Property.Application.Common.Models;
using Property.Application.Features.People.UnitOccupancies;

namespace Property.Application.Common.Interfaces;

public interface IUnitOccupancyService
{
    Task<PagedResult<UnitOccupancyDto>> ListAsync(ListQuery query, CancellationToken cancellationToken = default);
    Task<UnitOccupancyDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<UnitOccupancyDto> CreateAsync(CreateUnitOccupancyDto dto, CancellationToken cancellationToken = default);
    Task<UnitOccupancyDto?> UpdateAsync(Guid id, UpdateUnitOccupancyDto dto, CancellationToken cancellationToken = default);
    Task<bool> SoftDeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
