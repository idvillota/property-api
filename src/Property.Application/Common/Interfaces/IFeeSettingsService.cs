using Property.Application.Features.Billing.FeeSettings;

namespace Property.Application.Common.Interfaces;

public interface IFeeSettingsService
{
    Task<FeeSettingsDto> GetOrCreateAsync(Guid horizontalPropertyId, CancellationToken cancellationToken = default);
    Task<FeeSettingsDto?> UpdateAsync(Guid horizontalPropertyId, UpdateFeeSettingsDto dto, CancellationToken cancellationToken = default);
}
