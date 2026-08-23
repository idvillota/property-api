using Microsoft.AspNetCore.Mvc;
using Property.Api.Authorization;
using Property.Application.Authorization;
using Property.Application.Common.Interfaces;
using Property.Application.Features.Holding;

namespace Property.Api.Controllers;

[ApiController]
[RequireFeature(FeatureCodes.StructureProperties)]
[Route("api/holding")]
public sealed class HoldingController : ControllerBase
{
    private readonly IHoldingService _service;

    public HoldingController(IHoldingService service) => _service = service;

    [HttpGet("summary")]
    public async Task<ActionResult<HoldingSummaryDto>> GetSummary(
        CancellationToken cancellationToken = default) =>
        Ok(await _service.GetSummaryAsync(cancellationToken));
}
