using Property.Application.Common.Models;
using Property.Application.Features.Structure.Neighborhoods;

namespace Property.Application.Common.Interfaces;

public interface INeighborhoodService
{
    Task<PagedResult<NeighborhoodDto>> ListAsync(ListQuery query, CancellationToken cancellationToken = default);
    Task<NeighborhoodDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<NeighborhoodDto> CreateAsync(CreateNeighborhoodDto dto, CancellationToken cancellationToken = default);
    Task<NeighborhoodDto?> UpdateAsync(Guid id, UpdateNeighborhoodDto dto, CancellationToken cancellationToken = default);
    Task<bool> SoftDeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
