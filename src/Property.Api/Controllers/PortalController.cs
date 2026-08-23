using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Property.Api.Authorization;
using Property.Application.Authorization;
using Property.Application.Common.Interfaces;
using Property.Application.Features.Billing.PaymentIntents;
using Property.Application.Features.Community.Mural;
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

    /// <summary>COM-014 — active common areas for portal booking.</summary>
    [HttpGet("reservas/areas")]
    [RequirePersonLink]
    [RequireFeature(FeatureCodes.PortalReservations)]
    public async Task<ActionResult<IReadOnlyList<PortalCommonAreaDto>>> ReservationAreas(
        CancellationToken cancellationToken)
    {
        if (_tenant.UserId is not Guid userId)
            return Unauthorized();

        try
        {
            return Ok(await _portal.ListReservationAreasAsync(userId, cancellationToken));
        }
        catch (UnauthorizedAccessException)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Person link required for portal access." });
        }
    }

    /// <summary>COM-014 — occupied Pending/Confirmed slots for an area (availability).</summary>
    [HttpGet("reservas/areas/{commonAreaId:guid}/ocupados")]
    [RequirePersonLink]
    [RequireFeature(FeatureCodes.PortalReservations)]
    public async Task<ActionResult<IReadOnlyList<PortalReservationSlotDto>>> OccupiedSlots(
        Guid commonAreaId,
        [FromQuery] DateTime fromUtc,
        [FromQuery] DateTime toUtc,
        CancellationToken cancellationToken)
    {
        if (_tenant.UserId is not Guid userId)
            return Unauthorized();

        try
        {
            return Ok(await _portal.ListOccupiedSlotsAsync(userId, commonAreaId, fromUtc, toUtc, cancellationToken));
        }
        catch (UnauthorizedAccessException)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Person link required for portal access." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>COM-014 — caller's reservations.</summary>
    [HttpGet("reservas")]
    [RequirePersonLink]
    [RequireFeature(FeatureCodes.PortalReservations)]
    public async Task<ActionResult<IReadOnlyList<PortalReservationDto>>> MyReservations(
        CancellationToken cancellationToken)
    {
        if (_tenant.UserId is not Guid userId)
            return Unauthorized();

        try
        {
            return Ok(await _portal.ListMyReservationsAsync(userId, cancellationToken));
        }
        catch (UnauthorizedAccessException)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Person link required for portal access." });
        }
    }

    /// <summary>COM-014 — create reservation for a linked unit.</summary>
    [HttpPost("reservas")]
    [RequirePersonLink]
    [RequireFeature(FeatureCodes.PortalReservations)]
    public async Task<ActionResult<PortalReservationDto>> CreateReservation(
        [FromBody] CreatePortalReservationDto dto,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        if (_tenant.UserId is not Guid userId)
            return Unauthorized();

        try
        {
            var created = await _portal.CreateReservationAsync(userId, dto, cancellationToken);
            return CreatedAtAction(nameof(MyReservations), created);
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

    /// <summary>COM-014 — cancel own future reservation (soft-inactive).</summary>
    [HttpPost("reservas/{id:guid}/cancel")]
    [RequirePersonLink]
    [RequireFeature(FeatureCodes.PortalReservations)]
    public async Task<IActionResult> CancelReservation(Guid id, CancellationToken cancellationToken)
    {
        if (_tenant.UserId is not Guid userId)
            return Unauthorized();

        try
        {
            await _portal.CancelReservationAsync(userId, id, cancellationToken);
            return NoContent();
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

    /// <summary>COM-003 — portal mural feed (active posts only).</summary>
    [HttpGet("mural")]
    [RequirePersonLink]
    [RequireFeature(FeatureCodes.PortalMuralRead)]
    public async Task<ActionResult<IReadOnlyList<MuralPostDto>>> ListMural(CancellationToken cancellationToken)
    {
        if (_tenant.UserId is not Guid userId)
            return Unauthorized();

        try
        {
            return Ok(await _portal.ListMuralPostsAsync(userId, cancellationToken));
        }
        catch (UnauthorizedAccessException)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Person link required for portal access." });
        }
    }

    /// <summary>BIL-011 — start PSE checkout (Wompi) for a linked unit.</summary>
    [HttpPost("pagos/iniciar")]
    [RequirePersonLink]
    [RequireFeature(FeatureCodes.PortalPay)]
    [RequireTenantModule(TenantModuleKeys.DigitalPayments)]
    public async Task<ActionResult<PortalCheckoutDto>> StartPayment(
        [FromBody] StartPortalPaymentDto dto,
        CancellationToken cancellationToken)
    {
        if (_tenant.UserId is not Guid userId)
            return Unauthorized();

        try
        {
            return Ok(await _portal.StartPaymentAsync(userId, dto, cancellationToken));
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

    [HttpGet("pagos/intents/{id:guid}")]
    [RequirePersonLink]
    [RequireFeature(FeatureCodes.PortalPay)]
    [RequireTenantModule(TenantModuleKeys.DigitalPayments)]
    public async Task<ActionResult<PaymentIntentDto>> GetPaymentIntent(
        Guid id,
        CancellationToken cancellationToken)
    {
        if (_tenant.UserId is not Guid userId)
            return Unauthorized();

        try
        {
            var intent = await _portal.GetPaymentIntentAsync(userId, id, cancellationToken);
            return intent is null ? NotFound() : Ok(intent);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
    }

    /// <summary>Sandbox mock confirm (UseMockCheckout) — simulates approved Wompi webhook.</summary>
    [HttpPost("pagos/intents/{id:guid}/sandbox-confirm")]
    [RequirePersonLink]
    [RequireFeature(FeatureCodes.PortalPay)]
    [RequireTenantModule(TenantModuleKeys.DigitalPayments)]
    public async Task<ActionResult<PaymentIntentDto>> ConfirmSandboxPayment(
        Guid id,
        CancellationToken cancellationToken)
    {
        if (_tenant.UserId is not Guid userId)
            return Unauthorized();

        try
        {
            return Ok(await _portal.ConfirmSandboxPaymentAsync(userId, id, cancellationToken));
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
