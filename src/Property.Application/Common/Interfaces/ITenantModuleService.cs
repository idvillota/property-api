using Property.Application.Features.Organization.TenantSettings;

namespace Property.Application.Common.Interfaces;

public interface ITenantModuleService
{
    Task<TenantModuleFlagsDto> GetFlagsAsync(CancellationToken cancellationToken = default);

    Task<TenantModuleFlagsDto> UpdateFlagsAsync(
        TenantModuleFlagsDto flags,
        CancellationToken cancellationToken = default);

    Task<bool> IsEnabledAsync(string moduleKey, CancellationToken cancellationToken = default);
}
