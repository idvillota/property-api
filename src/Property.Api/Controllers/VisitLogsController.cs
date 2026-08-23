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
    private readonly IIntercomService _intercom;
    private readonly ICurrentTenantContext _tenant;

    public VisitLogsController(
        IVisitLogService service,
        IIntercomService intercom,
        ICurrentTenantContext tenant)
    {
        _service = service;
        _intercom = intercom;
        _tenant = tenant;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<VisitLogDto>>> List(
        [FromQuery] ListQuery query,
        CancellationToken cancellationToken = default) =>
        Ok(await _service.ListAsync(query, cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<VisitLogDto>> GetById(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var item = await _service.GetByIdAsync(id, cancellationToken);
        return item is null ? NotFound() : Ok(item);
    }

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
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id:guid}/override-approve")]
    public async Task<ActionResult<VisitLogDto>> OverrideApprove(
        Guid id,
        [FromBody] OverrideApproveVisitDto dto,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        if (_tenant.UserId is not Guid userId)
            return Unauthorized();

        try
        {
            return Ok(await _service.OverrideApproveAsync(id, userId, dto.Reason, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id:guid}/notify")]
    [RequireFeature(FeatureCodes.ReceptionIntercom)]
    public async Task<IActionResult> NotifyUnit(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _intercom.NotifyUnitAsync(id, cancellationToken);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
