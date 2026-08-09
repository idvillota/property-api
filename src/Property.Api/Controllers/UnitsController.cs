using Microsoft.AspNetCore.Mvc;
using Property.Api.Authorization;
using Property.Application.Authorization;
using Property.Application.Common.Interfaces;
using Property.Application.Common.Models;
using Property.Application.Features.Structure.Units;

namespace Property.Api.Controllers;

[ApiController]
[RequireFeature(FeatureCodes.StructureUnits)]
[Route("api/[controller]")]
public sealed class UnitsController : ControllerBase
{
    private readonly IUnitService _service;

    public UnitsController(IUnitService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<PagedResult<UnitDto>>> List(
        [FromQuery] ListQuery query,
        CancellationToken cancellationToken = default) =>
        Ok(await _service.ListAsync(query, cancellationToken));

    [HttpGet("coefficient-summary")]
    public async Task<ActionResult<UnitCoefficientSummaryDto>> CoefficientSummary(
        [FromQuery] Guid horizontalPropertyId,
        CancellationToken cancellationToken = default)
    {
        if (horizontalPropertyId == Guid.Empty)
            return BadRequest(new { message = "horizontalPropertyId is required." });

        try
        {
            return Ok(await _service.GetCoefficientSummaryAsync(horizontalPropertyId, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("recalculate-coefficients")]
    public async Task<ActionResult<UnitCoefficientSummaryDto>> RecalculateCoefficients(
        [FromQuery] Guid horizontalPropertyId,
        CancellationToken cancellationToken = default)
    {
        if (horizontalPropertyId == Guid.Empty)
            return BadRequest(new { message = "horizontalPropertyId is required." });

        try
        {
            return Ok(await _service.RecalculateCoefficientsAsync(horizontalPropertyId, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<UnitDto>> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        var item = await _service.GetByIdAsync(id, cancellationToken);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpGet("{id:guid}/household")]
    public async Task<ActionResult<UnitHouseholdDto>> GetHousehold(Guid id, CancellationToken cancellationToken = default)
    {
        var item = await _service.GetHouseholdAsync(id, cancellationToken);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<UnitDto>> Create(
        [FromBody] CreateUnitDto dto,
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
    public async Task<ActionResult<UnitDto>> Update(
        Guid id,
        [FromBody] UpdateUnitDto dto,
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
        var ok = await _service.SoftDeleteAsync(id, cancellationToken);
        return ok ? NoContent() : NotFound();
    }
}
