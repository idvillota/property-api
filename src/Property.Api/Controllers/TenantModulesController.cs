using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Property.Api.Authorization;
using Property.Application.Authorization;
using Property.Application.Common.Interfaces;
using Property.Application.Features.Organization.TenantSettings;

namespace Property.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/tenant/modules")]
public sealed class TenantModulesController : ControllerBase
{
    private readonly ITenantModuleService _modules;

    public TenantModulesController(ITenantModuleService modules) => _modules = modules;

    /// <summary>Any authenticated user — used to filter nav (ARCH-002).</summary>
    [HttpGet]
    public async Task<ActionResult<TenantModuleFlagsDto>> Get(CancellationToken cancellationToken = default) =>
        Ok(await _modules.GetFlagsAsync(cancellationToken));

    [HttpPut]
    [RequireFeature(FeatureCodes.SettingsTenant)]
    public async Task<ActionResult<TenantModuleFlagsDto>> Update(
        [FromBody] TenantModuleFlagsDto flags,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        return Ok(await _modules.UpdateFlagsAsync(flags, cancellationToken));
    }
}
