using Microsoft.AspNetCore.Mvc;
using Property.Api.Authorization;

namespace Property.Api.Controllers;

[ApiController]
[Route("api/platform/tenants")]
[RequirePlatformAdmin]
public sealed class PlatformTenantsController : ControllerBase
{
    /// <summary>Platform readiness stub for property management skeleton.</summary>
    [HttpGet("status")]
    public IActionResult Status() => Ok(new { status = "ready" });
}
