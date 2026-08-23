using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Property.Api.Authorization;
using Property.Application.Authorization;

namespace Property.Api.Controllers;

/// <summary>
/// Thin guard-shell probe so FE/BE can assert reception.manage on /guard routes (REC-011).
/// Full guard UI remains FE-only with ProtectedRoute.
/// </summary>
[ApiController]
[Authorize]
[RequireFeature(FeatureCodes.ReceptionManage)]
[Route("api/guard")]
public sealed class GuardAccessController : ControllerBase
{
    [HttpGet("ping")]
    public ActionResult<object> Ping() => Ok(new { ok = true });
}
