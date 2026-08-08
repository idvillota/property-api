using Property.Application.Common.Models;
using Property.Application.Features.Structure.Units;

namespace Property.Application.Common.Interfaces;

public interface IUnitService
{
    Task<PagedResult<UnitDto>> ListAsync(ListQuery query, CancellationToken cancellationToken = default);
    Task<UnitDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<UnitDto> CreateAsync(CreateUnitDto dto, CancellationToken cancellationToken = default);
    Task<UnitDto?> UpdateAsync(Guid id, UpdateUnitDto dto, CancellationToken cancellationToken = default);
    Task<bool> SoftDeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<UnitCoefficientSummaryDto> GetCoefficientSummaryAsync(Guid horizontalPropertyId, CancellationToken cancellationToken = default);
}
