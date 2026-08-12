using Microsoft.AspNetCore.Mvc;
using Property.Api.Authorization;
using Property.Application.Authorization;
using Property.Application.Common.Interfaces;
using Property.Application.Features.Structure.Composer;

namespace Property.Api.Controllers;

[ApiController]
[RequireFeature(FeatureCodes.StructureBuildings)]
[Route("api/StructureComposer")]
public sealed class StructureComposerController : ControllerBase
{
    private readonly IStructureComposerService _service;

    public StructureComposerController(IStructureComposerService service) => _service = service;

    [HttpGet("status")]
    public async Task<ActionResult<StructureComposerStatusDto>> Status(
        [FromQuery] Guid horizontalPropertyId,
        CancellationToken cancellationToken = default)
    {
        if (horizontalPropertyId == Guid.Empty)
            return BadRequest(new { message = "horizontalPropertyId is required." });

        try
        {
            return Ok(await _service.GetStatusAsync(horizontalPropertyId, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("workspace")]
    public async Task<ActionResult<StructureWorkspaceDto>> Workspace(
        [FromQuery] Guid horizontalPropertyId,
        CancellationToken cancellationToken = default)
    {
        if (horizontalPropertyId == Guid.Empty)
            return BadRequest(new { message = "horizontalPropertyId is required." });

        try
        {
            return Ok(await _service.GetWorkspaceAsync(horizontalPropertyId, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("preview")]
    public async Task<ActionResult<StructureComposerPreviewDto>> Preview(
        [FromBody] StructureComposerRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        try
        {
            return Ok(await _service.PreviewAsync(request, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("commit")]
    public async Task<ActionResult<StructureComposerCommitResultDto>> Commit(
        [FromBody] StructureComposerRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        try
        {
            return Ok(await _service.CommitAsync(request, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
