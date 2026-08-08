using Microsoft.AspNetCore.Mvc;
using Property.Api.Authorization;
using Property.Application.Authorization;
using Property.Application.Common.Interfaces;
using Property.Application.Common.Models;
using Property.Application.Features.Billing.FeeCharges;

namespace Property.Api.Controllers;

[ApiController]
[RequireFeature(FeatureCodes.BillingCharges)]
[Route("api/[controller]")]
public sealed class FeeChargesController : ControllerBase
{
    private readonly IFeeChargeService _service;

    public FeeChargesController(IFeeChargeService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<PagedResult<FeeChargeDto>>> List(
        [FromQuery] ListQuery query,
        CancellationToken cancellationToken = default) =>
        Ok(await _service.ListAsync(query, cancellationToken));

    [HttpGet("arrears-summary")]
    public async Task<ActionResult<ArrearsSummaryDto>> ArrearsSummary(
        [FromQuery] Guid horizontalPropertyId,
        CancellationToken cancellationToken = default)
    {
        if (horizontalPropertyId == Guid.Empty)
            return BadRequest(new { message = "horizontalPropertyId is required." });

        try
        {
            return Ok(await _service.GetArrearsSummaryAsync(horizontalPropertyId, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<FeeChargeDto>> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        var item = await _service.GetByIdAsync(id, cancellationToken);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<FeeChargeDto>> Create(
        [FromBody] CreateFeeChargeDto dto,
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

    [HttpPost("generate-ordinary")]
    public async Task<ActionResult<GenerateChargesResultDto>> GenerateOrdinary(
        [FromBody] GenerateOrdinaryChargesDto dto,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        try
        {
            return Ok(await _service.GenerateOrdinaryAsync(dto, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("generate-interest")]
    public async Task<ActionResult<GenerateChargesResultDto>> GenerateInterest(
        [FromBody] GenerateInterestChargesDto dto,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        try
        {
            return Ok(await _service.GenerateInterestAsync(dto, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<FeeChargeDto>> Update(
        Guid id,
        [FromBody] UpdateFeeChargeDto dto,
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
    public async Task<IActionResult> SoftDelete(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var ok = await _service.SoftDeleteAsync(id, cancellationToken);
            return ok ? NoContent() : NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
