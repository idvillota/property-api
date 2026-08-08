using Microsoft.AspNetCore.Mvc;
using Property.Api.Authorization;
using Property.Application.Authorization;
using Property.Application.Common.Interfaces;
using Property.Application.Features.Dashboard;

namespace Property.Api.Controllers;

[ApiController]
[Route("api/dashboard")]
public sealed class DashboardController : ControllerBase
{
    private readonly IDashboardService _service;

    public DashboardController(IDashboardService service) => _service = service;

    [HttpGet("layout")]
    [RequireFeature(FeatureCodes.DashboardView)]
    public async Task<ActionResult<DashboardLayoutDto>> GetLayout(CancellationToken cancellationToken = default) =>
        Ok(await _service.GetLayoutAsync(cancellationToken));

    [HttpPut("layout")]
    [RequireFeature(FeatureCodes.DashboardConfigure)]
    public async Task<ActionResult<DashboardLayoutDto>> UpdateLayout(
        [FromBody] DashboardLayoutDto layout,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        return Ok(await _service.UpdateLayoutAsync(layout, cancellationToken));
    }

    [HttpGet("catalog")]
    [RequireFeature(FeatureCodes.DashboardConfigure)]
    public ActionResult<IReadOnlyList<DashboardWidgetDefinitionDto>> GetCatalog() =>
        Ok(_service.GetCatalog());
}
