using Microsoft.AspNetCore.Mvc;
using Property.Api.Authorization;
using Property.Application.Authorization;
using Property.Application.Common.Interfaces;
using Property.Application.Common.Models;
using Property.Application.Features.Community.Pqrs;

namespace Property.Api.Controllers;

[ApiController]
[RequireFeature(FeatureCodes.PqrsManage)]
[Route("api/[controller]")]
public sealed class PqrsCasesController : ControllerBase
{
    private readonly IPqrsService _service;

    public PqrsCasesController(IPqrsService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<PagedResult<PqrsCaseDto>>> List(
        [FromQuery] ListQuery query,
        CancellationToken cancellationToken = default) =>
        Ok(await _service.ListAsync(query, cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PqrsCaseDto>> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        var item = await _service.GetByIdAsync(id, cancellationToken);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPut("{id:guid}/status")]
    public async Task<ActionResult<PqrsCaseDto>> Transition(
        Guid id,
        [FromBody] UpdatePqrsCaseStatusDto dto,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        try
        {
            var updated = await _service.TransitionStatusAsync(
                id,
                dto.Status,
                dto.DueAtUtc,
                dto.Comment,
                cancellationToken);
            return updated is null ? NotFound() : Ok(updated);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id:guid}/comments")]
    public async Task<ActionResult<PqrsCommentDto>> AddComment(
        Guid id,
        [FromBody] AddPqrsCommentDto dto,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        try
        {
            var created = await _service.AddCommentAsync(id, dto, cancellationToken);
            return created is null ? NotFound() : Ok(created);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
