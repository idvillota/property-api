using Microsoft.AspNetCore.Mvc;
using Property.Api.Authorization;
using Property.Application.Authorization;
using Property.Application.Common.Interfaces;
using Property.Application.Features.Billing.BreB;

namespace Property.Api.Controllers;

[ApiController]
[RequireFeature(FeatureCodes.BillingSettings)]
[Route("api/[controller]")]
public sealed class BreBPaymentProfilesController : ControllerBase
{
    private readonly IBreBPaymentProfileService _service;

    public BreBPaymentProfilesController(IBreBPaymentProfileService service) => _service = service;

    [HttpGet("{horizontalPropertyId:guid}")]
    public async Task<ActionResult<BreBPaymentProfileDto>> Get(
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
    public async Task<ActionResult<BreBPaymentProfileDto>> Update(
        Guid horizontalPropertyId,
        [FromBody] UpdateBreBPaymentProfileDto dto,
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
