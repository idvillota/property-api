using Microsoft.AspNetCore.Mvc;
using Property.Api.Authorization;
using Property.Application.Authorization;
using Property.Application.Common.Interfaces;
using Property.Application.Features.Organization.TenantUsers;

namespace Property.Api.Controllers;

[ApiController]
[RequireFeature(FeatureCodes.OrganizationTeam)]
[Route("api/[controller]")]
public sealed class TenantUsersController : ControllerBase
{
    private readonly ITenantUserInviteService _inviteService;

    public TenantUsersController(ITenantUserInviteService inviteService) => _inviteService = inviteService;

    /// <summary>Lists users that belong to the current tenant (Equipo).</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TenantUserListItemDto>>> List(
        CancellationToken cancellationToken = default) =>
        Ok(await _inviteService.ListAsync(cancellationToken));

    /// <summary>Adds a user to the current tenant (new account or existing global user not yet in this tenant).</summary>
    [HttpPost("invite")]
    public async Task<ActionResult<InvitedTenantUserDto>> Invite(
        [FromBody] InviteTenantUserDto dto,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        try
        {
            var result = await _inviteService.InviteAsync(dto, cancellationToken);
            return StatusCode(StatusCodes.Status201Created, result);
        }
        catch (InvalidOperationException ex)
        {
            if (ex.Message.Contains("already", StringComparison.OrdinalIgnoreCase))
                return Conflict(new { message = ex.Message });
            return BadRequest(new { message = ex.Message });
        }
    }
}
