using Microsoft.AspNetCore.Mvc;
using Property.Api.Authorization;
using Property.Application.Authorization;
using Property.Application.Common.Interfaces;
using Property.Application.Features.Parking;

namespace Property.Api.Controllers;

[ApiController]
[Route("api/visitor-parking")]
public sealed class VisitorParkingController : ControllerBase
{
    private readonly IVisitorParkingService _parking;

    public VisitorParkingController(IVisitorParkingService parking) => _parking = parking;

    [HttpGet("settings")]
    [RequireFeature(FeatureCodes.VisitorParking)]
    public async Task<ActionResult<VisitorParkingSettingsDto>> GetSettings(
        [FromQuery] Guid horizontalPropertyId,
        CancellationToken cancellationToken = default)
    {
        var item = await _parking.GetSettingsAsync(horizontalPropertyId, cancellationToken);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPut("settings")]
    [RequireFeature(FeatureCodes.VisitorParking)]
    public async Task<ActionResult<VisitorParkingSettingsDto>> UpsertSettings(
        [FromBody] UpsertVisitorParkingSettingsDto dto,
        CancellationToken cancellationToken = default) =>
        Ok(await _parking.UpsertSettingsAsync(dto, cancellationToken));

    [HttpGet("shifts/open")]
    [RequireFeature(FeatureCodes.VisitorParking)]
    public async Task<ActionResult<ParkingShiftDto>> GetOpenShift(
        [FromQuery] Guid horizontalPropertyId,
        CancellationToken cancellationToken = default)
    {
        var shift = await _parking.GetOpenShiftAsync(horizontalPropertyId, cancellationToken);
        return shift is null ? NotFound() : Ok(shift);
    }

    [HttpPost("shifts/open")]
    [RequireFeature(FeatureCodes.VisitorParking)]
    public async Task<ActionResult<ParkingShiftDto>> OpenShift(
        [FromBody] OpenParkingShiftDto dto,
        CancellationToken cancellationToken = default) =>
        Ok(await _parking.OpenShiftAsync(dto, cancellationToken));

    [HttpPost("shifts/close")]
    [RequireFeature(FeatureCodes.VisitorParking)]
    public async Task<ActionResult<ParkingShiftDto>> CloseShift(
        [FromBody] CloseParkingShiftDto dto,
        CancellationToken cancellationToken = default) =>
        Ok(await _parking.CloseShiftAsync(dto, cancellationToken));

    [HttpPost("tickets")]
    [RequireFeature(FeatureCodes.VisitorParking)]
    public async Task<ActionResult<ParkingTicketDto>> IssueTicket(
        [FromBody] IssueParkingTicketDto dto,
        CancellationToken cancellationToken = default) =>
        Ok(await _parking.IssueTicketAsync(dto, cancellationToken));

    [HttpPost("tickets/exit")]
    [RequireFeature(FeatureCodes.VisitorParking)]
    public async Task<ActionResult<ParkingTicketDto>> ExitTicket(
        [FromBody] ExitParkingTicketDto dto,
        CancellationToken cancellationToken = default) =>
        Ok(await _parking.ExitTicketAsync(dto, cancellationToken));
}
