using Microsoft.AspNetCore.Mvc;
using Property.Api.Authorization;
using Property.Application.Authorization;
using Property.Application.Common.Interfaces;
using Property.Application.Features.Assemblies;

namespace Property.Api.Controllers;

[ApiController]
[RequireTenantModule(TenantModuleKeys.Assemblies)]
[Route("api/assemblies")]
public sealed class AssembliesController : ControllerBase
{
    private readonly IAssemblyService _assemblies;

    public AssembliesController(IAssemblyService assemblies) => _assemblies = assemblies;

    [HttpGet]
    [RequireFeature(FeatureCodes.AssemblyManage)]
    public async Task<ActionResult<IReadOnlyList<AssemblyDto>>> List(
        [FromQuery] Guid horizontalPropertyId,
        CancellationToken cancellationToken = default) =>
        Ok(await _assemblies.ListAsync(horizontalPropertyId, cancellationToken));

    [HttpGet("portal")]
    [RequireFeature(FeatureCodes.AssemblyVote)]
    public async Task<ActionResult<IReadOnlyList<AssemblyDto>>> ListPublished(
        [FromQuery] Guid horizontalPropertyId,
        CancellationToken cancellationToken = default) =>
        Ok(await _assemblies.ListPublishedAsync(horizontalPropertyId, cancellationToken));

    [HttpGet("{assemblyId:guid}/motions/open")]
    [RequireFeature(FeatureCodes.AssemblyVote)]
    public async Task<ActionResult<IReadOnlyList<AssemblyMotionDto>>> ListOpenMotions(
        Guid assemblyId,
        CancellationToken cancellationToken = default) =>
        Ok(await _assemblies.ListOpenMotionsAsync(assemblyId, cancellationToken));

    [HttpGet("{id:guid}")]
    [RequireFeature(FeatureCodes.AssemblyManage)]
    public async Task<ActionResult<AssemblyDto>> Get(Guid id, CancellationToken cancellationToken = default)
    {
        var item = await _assemblies.GetAsync(id, cancellationToken);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    [RequireFeature(FeatureCodes.AssemblyManage)]
    public async Task<ActionResult<AssemblyDto>> Create(
        [FromBody] CreateAssemblyDto dto,
        CancellationToken cancellationToken = default) =>
        Ok(await _assemblies.CreateAsync(dto, cancellationToken));

    [HttpPost("{id:guid}/publish")]
    [RequireFeature(FeatureCodes.AssemblyManage)]
    public async Task<ActionResult<AssemblyDto>> Publish(Guid id, CancellationToken cancellationToken = default) =>
        Ok(await _assemblies.PublishAsync(id, cancellationToken));

    [HttpPost("attendance")]
    [RequireFeature(FeatureCodes.AssemblyManage)]
    public async Task<ActionResult<QuorumStatusDto>> RegisterAttendance(
        [FromBody] RegisterAttendanceDto dto,
        CancellationToken cancellationToken = default) =>
        Ok(await _assemblies.RegisterAttendanceAsync(dto, cancellationToken));

    [HttpPost("proxies")]
    [RequireFeature(FeatureCodes.AssemblyManage)]
    public async Task<ActionResult<QuorumStatusDto>> RegisterProxy(
        [FromBody] RegisterProxyDto dto,
        CancellationToken cancellationToken = default) =>
        Ok(await _assemblies.RegisterProxyAsync(dto, cancellationToken));

    [HttpGet("{assemblyId:guid}/quorum")]
    [RequireFeature(FeatureCodes.AssemblyManage)]
    public async Task<ActionResult<QuorumStatusDto>> GetQuorum(
        Guid assemblyId,
        CancellationToken cancellationToken = default) =>
        Ok(await _assemblies.GetQuorumAsync(assemblyId, cancellationToken));

    [HttpPost("motions")]
    [RequireFeature(FeatureCodes.AssemblyManage)]
    public async Task<ActionResult<AssemblyMotionDto>> CreateMotion(
        [FromBody] CreateMotionDto dto,
        CancellationToken cancellationToken = default) =>
        Ok(await _assemblies.CreateMotionAsync(dto, cancellationToken));

    [HttpPost("votes")]
    [RequireFeature(FeatureCodes.AssemblyVote)]
    public async Task<ActionResult<MotionResultDto>> CastVote(
        [FromBody] CastVoteDto dto,
        CancellationToken cancellationToken = default) =>
        Ok(await _assemblies.CastVoteAsync(dto, cancellationToken));

    [HttpPost("motions/{motionId:guid}/close")]
    [RequireFeature(FeatureCodes.AssemblyManage)]
    public async Task<ActionResult<MotionResultDto>> CloseMotion(
        Guid motionId,
        CancellationToken cancellationToken = default) =>
        Ok(await _assemblies.CloseMotionAsync(motionId, cancellationToken));

    [HttpGet("{assemblyId:guid}/results")]
    [RequireFeature(FeatureCodes.AssemblyManage)]
    public async Task<ActionResult<IReadOnlyList<MotionResultDto>>> ListResults(
        Guid assemblyId,
        CancellationToken cancellationToken = default) =>
        Ok(await _assemblies.ListMotionResultsAsync(assemblyId, cancellationToken));

    [HttpPost("{assemblyId:guid}/minutes")]
    [RequireFeature(FeatureCodes.AssemblyManage)]
    public async Task<ActionResult<AssemblyDto>> GenerateMinutes(
        Guid assemblyId,
        CancellationToken cancellationToken = default) =>
        Ok(await _assemblies.GenerateMinutesAsync(assemblyId, cancellationToken));
}
