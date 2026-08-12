using Property.Application.Common.Models;
using Property.Application.Features.Structure.Vehicles;

namespace Property.Application.Common.Interfaces;

public interface IVehicleService
{
    Task<PagedResult<VehicleDto>> ListAsync(ListQuery query, CancellationToken cancellationToken = default);
    Task<VehicleDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<VehicleDto> CreateAsync(CreateVehicleDto dto, CancellationToken cancellationToken = default);
    Task<VehicleDto?> UpdateAsync(Guid id, UpdateVehicleDto dto, CancellationToken cancellationToken = default);
    Task<VehicleDto?> AssignUnitAsync(Guid id, AssignVehicleUnitDto dto, CancellationToken cancellationToken = default);
    Task<VehicleDto?> UnassignUnitAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> SoftDeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
