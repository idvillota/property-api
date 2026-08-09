using Microsoft.AspNetCore.Mvc;
using Property.Api.Authorization;
using Property.Application.Authorization;
using Property.Application.Common.Interfaces;
using Property.Application.Common.Models;
using Property.Application.Features.Structure.HorizontalProperties;

namespace Property.Api.Controllers;

[ApiController]
[RequireFeature(FeatureCodes.StructureProperties)]
[Route("api/[controller]")]
public sealed class HorizontalPropertiesController : ControllerBase
{
    private readonly IHorizontalPropertyService _service;

    public HorizontalPropertiesController(IHorizontalPropertyService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<PagedResult<HorizontalPropertyDto>>> List(
        [FromQuery] ListQuery query,
        CancellationToken cancellationToken = default) =>
        Ok(await _service.ListAsync(query, cancellationToken));

    /// <summary>Returns the single horizontal property for the current tenant (Colombia 1:1 model).</summary>
    [HttpGet("current")]
    public async Task<ActionResult<HorizontalPropertyDto>> GetCurrent(CancellationToken cancellationToken = default)
    {
        var item = await _service.GetCurrentAsync(cancellationToken);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<HorizontalPropertyDto>> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        var item = await _service.GetByIdAsync(id, cancellationToken);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<HorizontalPropertyDto>> Create(
        [FromBody] CreateHorizontalPropertyDto dto,
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
    public async Task<ActionResult<HorizontalPropertyDto>> Update(
        Guid id,
        [FromBody] UpdateHorizontalPropertyDto dto,
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
            await _service.SoftDeleteAsync(id, cancellationToken);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
