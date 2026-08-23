using Microsoft.AspNetCore.Mvc;
using Property.Api.Authorization;
using Property.Application.Authorization;
using Property.Application.Common.Interfaces;
using Property.Application.Features.Platform.Audit;

namespace Property.Api.Controllers;

[ApiController]
[RequireFeature(FeatureCodes.AuditView)]
[Route("api/audit-events")]
public sealed class AuditEventsController : ControllerBase
{
    private readonly IAuditQueryService _query;

    public AuditEventsController(IAuditQueryService query) => _query = query;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AuditEventDto>>> List(
        [FromQuery] Guid? actorUserId = null,
        [FromQuery] string? entityType = null,
        [FromQuery] string? entityId = null,
        [FromQuery] DateTime? fromUtc = null,
        [FromQuery] DateTime? toUtc = null,
        [FromQuery] int take = 50,
        CancellationToken cancellationToken = default) =>
        Ok(await _query.ListAsync(
            new AuditEventListQuery
            {
                ActorUserId = actorUserId,
                EntityType = entityType,
                EntityId = entityId,
                FromUtc = fromUtc,
                ToUtc = toUtc,
                Take = take,
            },
            cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AuditEventDto>> GetById(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var item = await _query.GetByIdAsync(id, cancellationToken);
        return item is null ? NotFound() : Ok(item);
    }
}
