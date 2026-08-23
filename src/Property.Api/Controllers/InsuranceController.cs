using Microsoft.AspNetCore.Mvc;
using Property.Api.Authorization;
using Property.Application.Authorization;
using Property.Application.Common.Interfaces;
using Property.Application.Features.Community;

namespace Property.Api.Controllers;

[ApiController]
[Route("api/insurance")]
public sealed class InsuranceController : ControllerBase
{
    private readonly IInsuranceService _insurance;

    public InsuranceController(IInsuranceService insurance) => _insurance = insurance;

    [HttpGet]
    [RequireFeature(FeatureCodes.InsurancePolicies)]
    public async Task<ActionResult<IReadOnlyList<InsurancePolicyDto>>> List(
        [FromQuery] Guid horizontalPropertyId,
        CancellationToken cancellationToken = default) =>
        Ok(await _insurance.ListAsync(horizontalPropertyId, cancellationToken));

    [HttpPost]
    [RequireFeature(FeatureCodes.InsurancePolicies)]
    public async Task<ActionResult<InsurancePolicyDto>> Create(
        [FromBody] CreateInsurancePolicyDto dto,
        CancellationToken cancellationToken = default) =>
        Ok(await _insurance.CreateAsync(dto, cancellationToken));

    [HttpPost("run-alerts")]
    [RequireFeature(FeatureCodes.InsurancePolicies)]
    public async Task<ActionResult<int>> RunAlerts(CancellationToken cancellationToken = default) =>
        Ok(await _insurance.RunExpiryAlertsAsync(cancellationToken));
}
