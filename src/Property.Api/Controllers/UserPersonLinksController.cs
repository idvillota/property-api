using Microsoft.AspNetCore.Mvc;
using Property.Api.Authorization;
using Property.Application.Authorization;
using Property.Application.Common.Interfaces;
using Property.Application.Features.Organization.UserPersonLinks;

namespace Property.Api.Controllers;

[ApiController]
[RequireFeature(FeatureCodes.OrganizationTeam)]
[Route("api/people/{personId:guid}/access-link")]
public sealed class UserPersonLinksController : ControllerBase
{
    private readonly IUserPersonLinkService _service;

    public UserPersonLinksController(IUserPersonLinkService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<UserPersonLinkDto?>> Get(Guid personId, CancellationToken cancellationToken)
    {
        var link = await _service.GetByPersonIdAsync(personId, cancellationToken);
        return Ok(link);
    }

    [HttpPut]
    public async Task<ActionResult<UserPersonLinkDto>> Link(
        Guid personId,
        [FromBody] LinkUserToPersonDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            var link = await _service.LinkAsync(personId, dto, cancellationToken);
            return Ok(link);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete]
    public async Task<IActionResult> Unlink(Guid personId, CancellationToken cancellationToken)
    {
        try
        {
            await _service.UnlinkAsync(personId, cancellationToken);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
