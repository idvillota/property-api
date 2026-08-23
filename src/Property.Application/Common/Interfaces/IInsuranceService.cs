using Property.Application.Features.Community;

namespace Property.Application.Common.Interfaces;

public interface IInsuranceService
{
    Task<InsurancePolicyDto> CreateAsync(CreateInsurancePolicyDto dto, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<InsurancePolicyDto>> ListAsync(Guid horizontalPropertyId, CancellationToken cancellationToken = default);
    Task<int> RunExpiryAlertsAsync(CancellationToken cancellationToken = default);
}
