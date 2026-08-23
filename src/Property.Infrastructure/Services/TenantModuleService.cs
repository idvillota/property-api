using Microsoft.EntityFrameworkCore;
using Property.Application.Authorization;
using Property.Application.Common.Interfaces;
using Property.Application.Features.Organization.TenantSettings;
using Property.Domain.Entities;
using Property.Infrastructure.Persistence;

namespace Property.Infrastructure.Services;

public sealed class TenantModuleService : ITenantModuleService
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentTenantContext _tenantContext;

    public TenantModuleService(ApplicationDbContext db, ICurrentTenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    public async Task<TenantModuleFlagsDto> GetFlagsAsync(CancellationToken cancellationToken = default)
    {
        var settings = await GetOrCreateSettingsAsync(cancellationToken);
        return Map(settings);
    }

    public async Task<TenantModuleFlagsDto> UpdateFlagsAsync(
        TenantModuleFlagsDto flags,
        CancellationToken cancellationToken = default)
    {
        var settings = await GetOrCreateSettingsAsync(cancellationToken);
        Apply(settings, flags);
        _db.TenantSettings.Update(settings);
        await _db.SaveChangesAsync(cancellationToken);
        return Map(settings);
    }

    public async Task<bool> IsEnabledAsync(string moduleKey, CancellationToken cancellationToken = default)
    {
        var flags = await GetFlagsAsync(cancellationToken);
        return moduleKey switch
        {
            TenantModuleKeys.Portal => flags.Portal,
            TenantModuleKeys.DigitalPayments => flags.DigitalPayments,
            TenantModuleKeys.Accounting => flags.Accounting,
            TenantModuleKeys.Visits => flags.Visits,
            TenantModuleKeys.Assemblies => flags.Assemblies,
            _ => false,
        };
    }

    public static TenantModuleFlagsDto Map(TenantSettings settings) =>
        new()
        {
            Portal = settings.ModulePortalEnabled,
            DigitalPayments = settings.ModuleDigitalPaymentsEnabled,
            Accounting = settings.ModuleAccountingEnabled,
            Visits = settings.ModuleVisitsEnabled,
            Assemblies = settings.ModuleAssembliesEnabled,
        };

    public static void Apply(TenantSettings settings, TenantModuleFlagsDto flags)
    {
        settings.ModulePortalEnabled = flags.Portal;
        settings.ModuleDigitalPaymentsEnabled = flags.DigitalPayments;
        settings.ModuleAccountingEnabled = flags.Accounting;
        settings.ModuleVisitsEnabled = flags.Visits;
        settings.ModuleAssembliesEnabled = flags.Assemblies;
    }

    private async Task<TenantSettings> GetOrCreateSettingsAsync(CancellationToken cancellationToken)
    {
        var tenantId = ResolveTenantId();
        var settings = await _db.TenantSettings.FirstOrDefaultAsync(s => s.TenantId == tenantId, cancellationToken);
        if (settings is not null)
            return settings;

        settings = new TenantSettings
        {
            TenantId = tenantId,
            ModulePortalEnabled = true,
        };
        await _db.TenantSettings.AddAsync(settings, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        return settings;
    }

    private Guid ResolveTenantId()
    {
        if (_tenantContext.TenantId is { } tenantId && tenantId != Guid.Empty)
            return tenantId;
        throw new InvalidOperationException("Tenant context is not available.");
    }
}
