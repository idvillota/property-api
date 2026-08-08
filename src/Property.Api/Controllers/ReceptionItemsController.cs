using Microsoft.AspNetCore.Mvc;
using Property.Api.Authorization;
using Property.Application.Authorization;
using Property.Application.Common.Interfaces;
using Property.Application.Common.Models;
using Property.Application.Features.Reception.ReceptionItems;

namespace Property.Api.Controllers;

[ApiController]
[RequireFeature(FeatureCodes.ReceptionManage)]
[Route("api/[controller]")]
public sealed class ReceptionItemsController : ControllerBase
{
    private readonly IReceptionItemService _service;

    public ReceptionItemsController(IReceptionItemService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<PagedResult<ReceptionItemDto>>> List(
        [FromQuery] ListQuery query,
        CancellationToken cancellationToken = default) =>
        Ok(await _service.ListAsync(query, cancellationToken));

    [HttpGet("pending-count")]
    public async Task<ActionResult<ReceptionPendingCountDto>> PendingCount(
        [FromQuery] Guid horizontalPropertyId,
        CancellationToken cancellationToken = default)
    {
        if (horizontalPropertyId == Guid.Empty)
            return BadRequest(new { message = "horizontalPropertyId is required." });

        try
        {
            return Ok(await _service.GetPendingCountAsync(horizontalPropertyId, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ReceptionItemDto>> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        var item = await _service.GetByIdAsync(id, cancellationToken);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<ReceptionItemDto>> Create(
        [FromBody] CreateReceptionItemDto dto,
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
    public async Task<ActionResult<ReceptionItemDto>> Update(
        Guid id,
        [FromBody] UpdateReceptionItemDto dto,
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

    [HttpPost("{id:guid}/deliver")]
    public async Task<ActionResult<ReceptionItemDto>> Deliver(
        Guid id,
        [FromBody] DeliverReceptionItemDto? dto,
        CancellationToken cancellationToken = default)
    {
        dto ??= new DeliverReceptionItemDto();

        try
        {
            var updated = await _service.DeliverAsync(id, dto, cancellationToken);
            return updated is null ? NotFound() : Ok(updated);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id:guid}/return")]
    public async Task<ActionResult<ReceptionItemDto>> Return(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var updated = await _service.ReturnAsync(id, cancellationToken);
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
