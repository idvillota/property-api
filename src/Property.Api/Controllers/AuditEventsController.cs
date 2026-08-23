using Microsoft.AspNetCore.Mvc;
using Property.Api.Authorization;
using Property.Application.Authorization;
using Property.Application.Common.Interfaces;
using Property.Application.Features.Platform.Audit;

namespace Property.Api.Controllers;

/// <summary>Read-only audit trail (ARCH-006). Full UI is ORG-010. No update/delete endpoints.</summary>
[ApiController]
[RequireFeature(FeatureCodes.AuditView)]
[Route("api/audit-events")]
public sealed class AuditEventsController : ControllerBase
{
    private readonly IAuditQueryService _query;

    public AuditEventsController(IAuditQueryService query) => _query = query;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AuditEventDto>>> List(
        [FromQuery] int take = 50,
        CancellationToken cancellationToken = default) =>
        Ok(await _query.ListRecentAsync(take, cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AuditEventDto>> GetById(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var match = await _query.GetByIdAsync(id, cancellationToken);
        return match is null ? NotFound() : Ok(match);
    }
}
