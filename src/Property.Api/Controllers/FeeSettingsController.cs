using Microsoft.AspNetCore.Mvc;
using Property.Api.Authorization;
using Property.Application.Authorization;
using Property.Application.Common.Interfaces;
using Property.Application.Features.Billing.FeeSettings;

namespace Property.Api.Controllers;

[ApiController]
[RequireFeature(FeatureCodes.BillingSettings)]
[Route("api/[controller]")]
public sealed class FeeSettingsController : ControllerBase
{
    private readonly IFeeSettingsService _service;

    public FeeSettingsController(IFeeSettingsService service) => _service = service;

    [HttpGet("{horizontalPropertyId:guid}")]
    public async Task<ActionResult<FeeSettingsDto>> Get(
        Guid horizontalPropertyId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return Ok(await _service.GetOrCreateAsync(horizontalPropertyId, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{horizontalPropertyId:guid}")]
    public async Task<ActionResult<FeeSettingsDto>> Update(
        Guid horizontalPropertyId,
        [FromBody] UpdateFeeSettingsDto dto,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        try
        {
            var updated = await _service.UpdateAsync(horizontalPropertyId, dto, cancellationToken);
            return updated is null ? NotFound() : Ok(updated);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
