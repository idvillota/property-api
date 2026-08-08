using Microsoft.AspNetCore.Mvc;
using Property.Api.Authorization;
using Property.Application.Authorization;
using Property.Application.Common.Interfaces;
using Property.Application.Common.Models;
using Property.Application.Features.Billing.BillingPeriods;

namespace Property.Api.Controllers;

[ApiController]
[RequireFeature(FeatureCodes.BillingCharges)]
[Route("api/[controller]")]
public sealed class BillingPeriodsController : ControllerBase
{
    private readonly IBillingPeriodService _service;

    public BillingPeriodsController(IBillingPeriodService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<PagedResult<BillingPeriodDto>>> List(
        [FromQuery] ListQuery query,
        CancellationToken cancellationToken = default) =>
        Ok(await _service.ListAsync(query, cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<BillingPeriodDto>> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        var item = await _service.GetByIdAsync(id, cancellationToken);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<BillingPeriodDto>> Create(
        [FromBody] CreateBillingPeriodDto dto,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        try
        {
            var created = await _service.CreateAsync(dto, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<BillingPeriodDto>> Update(
        Guid id,
        [FromBody] UpdateBillingPeriodDto dto,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        try
        {
            var updated = await _service.UpdateAsync(id, dto, cancellationToken);
            return updated is null ? NotFound() : Ok(updated);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> SoftClose(Guid id, CancellationToken cancellationToken = default)
    {
        var ok = await _service.SoftCloseAsync(id, cancellationToken);
        return ok ? NoContent() : NotFound();
    }
}
