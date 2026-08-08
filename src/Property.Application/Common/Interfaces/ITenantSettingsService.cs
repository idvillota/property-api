using Property.Application.Features.Organization.TenantSettings;

namespace Property.Application.Common.Interfaces;

public interface ITenantSettingsService
{
    Task<TenantSettingsDto> GetAsync(CancellationToken cancellationToken = default);

    Task<TenantSettingsDto> UpdateAsync(UpdateTenantSettingsDto dto, CancellationToken cancellationToken = default);
}
