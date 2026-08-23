using Microsoft.AspNetCore.Mvc;
using Property.Api.Authorization;
using Property.Application.Authorization;
using Property.Application.Common.Interfaces;
using Property.Application.Features.AiAssist;

namespace Property.Api.Controllers;

[ApiController]
[RequireFeature(FeatureCodes.AiOps)]
[Route("api/ai-assist")]
public sealed class AiAssistController : ControllerBase
{
    private readonly IAiAssistService _service;

    public AiAssistController(IAiAssistService service) => _service = service;

    [HttpPost("suggest-pqrs/{pqrsId:guid}")]
    public async Task<ActionResult<AiAssistDraftDto>> SuggestPqrsReply(
        Guid pqrsId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var draft = await _service.SuggestPqrsReplyAsync(pqrsId, cancellationToken);
            return Ok(new AiAssistDraftDto { Draft = draft });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("suggest-dunning")]
    public async Task<ActionResult<AiAssistDraftDto>> SuggestDunningText(
        [FromBody] SuggestDunningTextRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        try
        {
            var draft = await _service.SuggestDunningTextAsync(request, cancellationToken);
            return Ok(new AiAssistDraftDto { Draft = draft });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
