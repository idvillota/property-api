using Property.Application.Features.Billing.BreB;

namespace Property.Application.Common.Interfaces;

public interface IBreBPaymentProfileService
{
    Task<BreBPaymentProfileDto> GetOrCreateAsync(Guid horizontalPropertyId, CancellationToken cancellationToken = default);
    Task<BreBPaymentProfileDto?> UpdateAsync(
        Guid horizontalPropertyId,
        UpdateBreBPaymentProfileDto dto,
        CancellationToken cancellationToken = default);
}
