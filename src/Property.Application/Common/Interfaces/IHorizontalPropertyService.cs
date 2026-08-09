using Property.Application.Common.Models;
using Property.Application.Features.Structure.HorizontalProperties;

namespace Property.Application.Common.Interfaces;

public interface IHorizontalPropertyService
{
    Task<PagedResult<HorizontalPropertyDto>> ListAsync(ListQuery query, CancellationToken cancellationToken = default);
    Task<HorizontalPropertyDto?> GetCurrentAsync(CancellationToken cancellationToken = default);
    Task<HorizontalPropertyDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<HorizontalPropertyDto> CreateAsync(CreateHorizontalPropertyDto dto, CancellationToken cancellationToken = default);
    Task<HorizontalPropertyDto?> UpdateAsync(Guid id, UpdateHorizontalPropertyDto dto, CancellationToken cancellationToken = default);
    Task<bool> SoftDeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
