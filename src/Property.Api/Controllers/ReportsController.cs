using Microsoft.AspNetCore.Mvc;
using Property.Api.Authorization;
using Property.Application.Authorization;
using Property.Application.Common.Interfaces;
using Property.Application.Features.Reports;

namespace Property.Api.Controllers;

[ApiController]
[RequireFeature(FeatureCodes.ReportsView)]
[Route("api/[controller]")]
public sealed class ReportsController : ControllerBase
{
    private readonly IReportService _service;

    public ReportsController(IReportService service) => _service = service;

    [HttpGet("overview")]
    public async Task<ActionResult<ReportOverviewDto>> Overview(
        [FromQuery] Guid horizontalPropertyId,
        CancellationToken cancellationToken = default)
    {
        if (horizontalPropertyId == Guid.Empty)
            return BadRequest(new { message = "horizontalPropertyId is required." });

        try
        {
            return Ok(await _service.GetOverviewAsync(horizontalPropertyId, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("cartera")]
    public async Task<ActionResult<CarteraReportDto>> Cartera(
        [FromQuery] Guid horizontalPropertyId,
        [FromQuery] DateOnly? asOfDate,
        CancellationToken cancellationToken = default)
    {
        if (horizontalPropertyId == Guid.Empty)
            return BadRequest(new { message = "horizontalPropertyId is required." });

        try
        {
            return Ok(await _service.GetCarteraAsync(horizontalPropertyId, asOfDate, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("collections")]
    public async Task<ActionResult<CollectionsReportDto>> Collections(
        [FromQuery] Guid horizontalPropertyId,
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        CancellationToken cancellationToken = default)
    {
        if (horizontalPropertyId == Guid.Empty)
            return BadRequest(new { message = "horizontalPropertyId is required." });

        try
        {
            return Ok(await _service.GetCollectionsAsync(horizontalPropertyId, from, to, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("occupancy")]
    public async Task<ActionResult<OccupancyReportDto>> Occupancy(
        [FromQuery] Guid horizontalPropertyId,
        CancellationToken cancellationToken = default)
    {
        if (horizontalPropertyId == Guid.Empty)
            return BadRequest(new { message = "horizontalPropertyId is required." });

        try
        {
            return Ok(await _service.GetOccupancyAsync(horizontalPropertyId, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("coefficients")]
    public async Task<ActionResult<CoefficientsReportDto>> Coefficients(
        [FromQuery] Guid horizontalPropertyId,
        CancellationToken cancellationToken = default)
    {
        if (horizontalPropertyId == Guid.Empty)
            return BadRequest(new { message = "horizontalPropertyId is required." });

        try
        {
            return Ok(await _service.GetCoefficientsAsync(horizontalPropertyId, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("reservation-usage")]
    public async Task<ActionResult<ReservationUsageReportDto>> ReservationUsage(
        [FromQuery] Guid horizontalPropertyId,
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        CancellationToken cancellationToken = default)
    {
        if (horizontalPropertyId == Guid.Empty)
            return BadRequest(new { message = "horizontalPropertyId is required." });

        try
        {
            return Ok(await _service.GetReservationUsageAsync(horizontalPropertyId, from, to, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("reception")]
    public async Task<ActionResult<ReceptionReportDto>> Reception(
        [FromQuery] Guid horizontalPropertyId,
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        CancellationToken cancellationToken = default)
    {
        if (horizontalPropertyId == Guid.Empty)
            return BadRequest(new { message = "horizontalPropertyId is required." });

        try
        {
            return Ok(await _service.GetReceptionAsync(horizontalPropertyId, from, to, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
