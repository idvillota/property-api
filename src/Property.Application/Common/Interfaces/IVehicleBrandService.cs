using Property.Application.Common.Models;
using Property.Application.Features.Structure.VehicleBrands;

namespace Property.Application.Common.Interfaces;

public interface IVehicleBrandService
{
    Task<PagedResult<VehicleBrandDto>> ListAsync(ListQuery query, CancellationToken cancellationToken = default);
    Task<VehicleBrandDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<VehicleBrandDto> CreateAsync(CreateVehicleBrandDto dto, CancellationToken cancellationToken = default);
    Task<VehicleBrandDto?> UpdateAsync(Guid id, UpdateVehicleBrandDto dto, CancellationToken cancellationToken = default);
    Task<bool> SoftDeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
