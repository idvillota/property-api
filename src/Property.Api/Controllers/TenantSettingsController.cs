using Microsoft.AspNetCore.Mvc;
using Property.Api.Authorization;
using Property.Application.Authorization;
using Property.Application.Common.Interfaces;
using Property.Application.Features.Organization.TenantSettings;

namespace Property.Api.Controllers;

[ApiController]
[RequireFeature(FeatureCodes.SettingsTenant)]
[Route("api/tenant/settings")]
public sealed class TenantSettingsController : ControllerBase
{
    private readonly ITenantSettingsService _service;

    public TenantSettingsController(ITenantSettingsService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<TenantSettingsDto>> Get(CancellationToken cancellationToken = default) =>
        Ok(await _service.GetAsync(cancellationToken));

    [HttpPut]
    public async Task<ActionResult<TenantSettingsDto>> Update(
        [FromBody] UpdateTenantSettingsDto dto,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        try
        {
            return Ok(await _service.UpdateAsync(dto, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
