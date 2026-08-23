using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Property.Api.Authorization;
using Property.Application.Authorization;
using Property.Application.Common.Interfaces;
using Property.Application.Features.Organization.UserPersonLinks;
using Property.Application.Features.Portal;

namespace Property.Api.Controllers;

[ApiController]
[Authorize]
[RequireFeature(FeatureCodes.PortalAccess)]
[RequireTenantModule(TenantModuleKeys.Portal)]
[Route("api/portal")]
public sealed class PortalController : ControllerBase
{
    private readonly IPortalService _portal;
    private readonly ICurrentTenantContext _tenant;

    public PortalController(IPortalService portal, ICurrentTenantContext tenant)
    {
        _portal = portal;
        _tenant = tenant;
    }

    /// <summary>Portal identity — requires PersonLink (PRT-001 / ARCH-001).</summary>
    [HttpGet("me")]
    [RequirePersonLink]
    public async Task<ActionResult<PortalMeDto>> Me(CancellationToken cancellationToken)
    {
        if (_tenant.UserId is not Guid userId)
            return Unauthorized();

        try
        {
            return Ok(await _portal.GetMeAsync(userId, cancellationToken));
        }
        catch (UnauthorizedAccessException)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Person link required for portal access." });
        }
    }

    /// <summary>Home payload scoped to linked units only.</summary>
    [HttpGet("home")]
    [RequirePersonLink]
    public async Task<ActionResult<PortalHomeDto>> Home(CancellationToken cancellationToken)
    {
        if (_tenant.UserId is not Guid userId)
            return Unauthorized();

        try
        {
            return Ok(await _portal.GetHomeAsync(userId, cancellationToken));
        }
        catch (UnauthorizedAccessException)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Person link required for portal access." });
        }
    }

    /// <summary>BIL-010 — estado de cuenta for linked units only.</summary>
    [HttpGet("cuenta")]
    [RequirePersonLink]
    [RequireFeature(FeatureCodes.PortalAccountRead)]
    public async Task<ActionResult<PortalAccountDto>> Cuenta(CancellationToken cancellationToken)
    {
        if (_tenant.UserId is not Guid userId)
            return Unauthorized();

        try
        {
            return Ok(await _portal.GetAccountAsync(userId, cancellationToken));
        }
        catch (UnauthorizedAccessException)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Person link required for portal access." });
        }
    }

    /// <summary>BIL-010 — unit account detail (scoped).</summary>
    [HttpGet("cuenta/units/{unitId:guid}")]
    [RequirePersonLink]
    [RequireFeature(FeatureCodes.PortalAccountRead)]
    public async Task<ActionResult<PortalAccountUnitDetailDto>> CuentaUnit(
        Guid unitId,
        CancellationToken cancellationToken)
    {
        if (_tenant.UserId is not Guid userId)
            return Unauthorized();

        try
        {
            return Ok(await _portal.GetAccountUnitAsync(userId, unitId, cancellationToken));
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
