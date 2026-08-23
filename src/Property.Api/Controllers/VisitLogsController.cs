using Microsoft.AspNetCore.Mvc;
using Property.Api.Authorization;
using Property.Application.Authorization;
using Property.Application.Common.Interfaces;
using Property.Application.Common.Models;
using Property.Application.Features.Reception.Visits;

namespace Property.Api.Controllers;

[ApiController]
[RequireAnyFeature(FeatureCodes.ReceptionManage, FeatureCodes.ReceptionVisits)]
[Route("api/visit-logs")]
public sealed class VisitLogsController : ControllerBase
{
    private readonly IVisitLogService _service;

    public VisitLogsController(IVisitLogService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<PagedResult<VisitLogDto>>> List(
        [FromQuery] ListQuery query,
        CancellationToken cancellationToken = default) =>
        Ok(await _service.ListAsync(query, cancellationToken));

    [HttpPost]
    public async Task<ActionResult<VisitLogDto>> Register(
        [FromBody] RegisterVisitDto dto,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        try
        {
            var created = await _service.RegisterAsync(dto, cancellationToken);
            return CreatedAtAction(nameof(List), new { }, created);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
