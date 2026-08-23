using Microsoft.AspNetCore.Mvc;
using Property.Api.Authorization;
using Property.Application.Authorization;
using Property.Application.Common.Interfaces;
using Property.Application.Features.Billing.Dunning;

namespace Property.Api.Controllers;

[ApiController]
[RequireFeature(FeatureCodes.BillingCharges)]
[Route("api/dunning")]
public sealed class DunningController : ControllerBase
{
    private readonly IDunningAdminService _service;
    private readonly IDunningJobService _job;

    public DunningController(IDunningAdminService service, IDunningJobService job)
    {
        _service = service;
        _job = job;
    }

    [HttpGet("rules")]
    public async Task<ActionResult<IReadOnlyList<DunningRuleDto>>> ListRules(
        [FromQuery] Guid horizontalPropertyId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return Ok(await _service.ListRulesAsync(horizontalPropertyId, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("rules")]
    public async Task<ActionResult<DunningRuleDto>> UpsertRule(
        [FromBody] UpsertDunningRuleDto dto,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        try
        {
            return Ok(await _service.UpsertRuleAsync(dto, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("rules/{id:guid}")]
    public async Task<IActionResult> DeleteRule(Guid id, CancellationToken cancellationToken = default)
    {
        var ok = await _service.DeleteRuleAsync(id, cancellationToken);
        return ok ? NoContent() : NotFound();
    }

    [HttpGet("sends")]
    public async Task<ActionResult<IReadOnlyList<DunningSendLogDto>>> ListSends(
        [FromQuery] Guid horizontalPropertyId,
        [FromQuery] int take = 50,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return Ok(await _service.ListRecentSendsAsync(horizontalPropertyId, take, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>Manual trigger for admins / demo (also runs on property-worker schedule).</summary>
    [HttpPost("run")]
    public async Task<ActionResult<object>> RunNow(CancellationToken cancellationToken = default)
    {
        var count = await _job.RunAsync(cancellationToken: cancellationToken);
        return Ok(new { enqueued = count });
    }
}
