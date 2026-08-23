using Microsoft.AspNetCore.Mvc;
using Property.Api.Authorization;
using Property.Application.Authorization;
using Property.Application.Common.Interfaces;
using Property.Application.Features.Community;

namespace Property.Api.Controllers;

[ApiController]
[Route("api/polls")]
public sealed class PollsController : ControllerBase
{
    private readonly IPollService _polls;

    public PollsController(IPollService polls) => _polls = polls;

    [HttpGet]
    [RequireFeature(FeatureCodes.PollsManage)]
    public async Task<ActionResult<IReadOnlyList<PollDto>>> ListAdmin(
        [FromQuery] Guid horizontalPropertyId,
        CancellationToken cancellationToken = default) =>
        Ok(await _polls.ListAsync(horizontalPropertyId, cancellationToken));

    [HttpPost]
    [RequireFeature(FeatureCodes.PollsManage)]
    public async Task<ActionResult<PollDto>> Create(
        [FromBody] CreatePollDto dto,
        CancellationToken cancellationToken = default) =>
        Ok(await _polls.CreateAsync(dto, cancellationToken));

    [HttpPost("{pollId:guid}/open")]
    [RequireFeature(FeatureCodes.PollsManage)]
    public async Task<ActionResult<PollDto>> Open(Guid pollId, CancellationToken cancellationToken = default) =>
        Ok(await _polls.OpenAsync(pollId, cancellationToken));

    [HttpPost("{pollId:guid}/close")]
    [RequireFeature(FeatureCodes.PollsManage)]
    public async Task<ActionResult<PollDto>> Close(Guid pollId, CancellationToken cancellationToken = default) =>
        Ok(await _polls.CloseAsync(pollId, cancellationToken));

    [HttpGet("portal")]
    [RequireFeature(FeatureCodes.PortalPolls)]
    public async Task<ActionResult<IReadOnlyList<PollDto>>> ListPortal(
        [FromQuery] Guid horizontalPropertyId,
        CancellationToken cancellationToken = default) =>
        Ok(await _polls.ListAsync(horizontalPropertyId, cancellationToken));

    [HttpPost("votes")]
    [RequireFeature(FeatureCodes.PortalPolls)]
    public async Task<ActionResult<PollDto>> CastVote(
        [FromBody] CastPollVoteDto dto,
        CancellationToken cancellationToken = default) =>
        Ok(await _polls.CastVoteAsync(dto, cancellationToken));
}
