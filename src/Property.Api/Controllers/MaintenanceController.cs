using Microsoft.AspNetCore.Mvc;
using Property.Api.Authorization;
using Property.Application.Authorization;
using Property.Application.Common.Interfaces;
using Property.Application.Features.Community;

namespace Property.Api.Controllers;

[ApiController]
[Route("api/maintenance")]
public sealed class MaintenanceController : ControllerBase
{
    private readonly IMaintenanceService _maintenance;

    public MaintenanceController(IMaintenanceService maintenance) => _maintenance = maintenance;

    [HttpGet]
    [RequireFeature(FeatureCodes.MaintenancePlan)]
    public async Task<ActionResult<IReadOnlyList<MaintenancePlanDto>>> List(
        [FromQuery] Guid horizontalPropertyId,
        CancellationToken cancellationToken = default) =>
        Ok(await _maintenance.ListAsync(horizontalPropertyId, cancellationToken));

    [HttpPost("plans")]
    [RequireFeature(FeatureCodes.MaintenancePlan)]
    public async Task<ActionResult<MaintenancePlanDto>> CreatePlan(
        [FromBody] CreateMaintenancePlanDto dto,
        CancellationToken cancellationToken = default) =>
        Ok(await _maintenance.CreatePlanAsync(dto, cancellationToken));

    [HttpPost("tasks/{taskId:guid}/complete")]
    [RequireFeature(FeatureCodes.MaintenancePlan)]
    public async Task<ActionResult<MaintenanceTaskDto>> CompleteTask(
        Guid taskId,
        CancellationToken cancellationToken = default) =>
        Ok(await _maintenance.CompleteTaskAsync(taskId, cancellationToken));
}
