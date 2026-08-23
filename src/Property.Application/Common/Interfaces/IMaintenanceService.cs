using Property.Application.Features.Community;

namespace Property.Application.Common.Interfaces;

public interface IMaintenanceService
{
    Task<MaintenancePlanDto> CreatePlanAsync(CreateMaintenancePlanDto dto, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MaintenancePlanDto>> ListAsync(Guid horizontalPropertyId, CancellationToken cancellationToken = default);
    Task<MaintenanceTaskDto> CompleteTaskAsync(Guid taskId, CancellationToken cancellationToken = default);
}
