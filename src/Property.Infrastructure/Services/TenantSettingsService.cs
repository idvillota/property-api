using Microsoft.EntityFrameworkCore;
using Property.Application.Common.Interfaces;
using Property.Application.Features.Organization.TenantSettings;
using Property.Domain.Entities;
using Property.Infrastructure.Persistence;

namespace Property.Infrastructure.Services;

public sealed class TenantSettingsService : ITenantSettingsService
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentTenantContext _tenantContext;

    public TenantSettingsService(ApplicationDbContext db, ICurrentTenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    public async Task<TenantSettingsDto> GetAsync(CancellationToken cancellationToken = default)
    {
        var settings = await GetOrCreateSettingsAsync(cancellationToken);
        return Map(settings);
    }

    public async Task<TenantSettingsDto> UpdateAsync(
        UpdateTenantSettingsDto dto,
        CancellationToken cancellationToken = default)
    {
        var settings = await GetOrCreateSettingsAsync(cancellationToken);
        settings.TradeName = dto.TradeName.Trim();
        settings.LegalName = dto.LegalName.Trim();
        settings.TaxRegime = dto.TaxRegime.Trim();
        settings.TaxId = dto.TaxId.Trim();
        settings.LegalRepresentative = dto.LegalRepresentative?.Trim();
        settings.AddressLine = dto.AddressLine.Trim();
        settings.City = dto.City.Trim();
        settings.Country = dto.Country.Trim();
        settings.PostalCode = dto.PostalCode?.Trim();
        settings.Phone = dto.Phone?.Trim();

        _db.TenantSettings.Update(settings);
        await _db.SaveChangesAsync(cancellationToken);
        return Map(settings);
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

    private static TenantSettingsDto Map(TenantSettings settings) =>
        new()
        {
            TradeName = settings.TradeName,
            LegalName = settings.LegalName,
            TaxRegime = settings.TaxRegime,
            TaxId = settings.TaxId,
            LegalRepresentative = settings.LegalRepresentative,
            AddressLine = settings.AddressLine,
            City = settings.City,
            Country = settings.Country,
            PostalCode = settings.PostalCode,
            Phone = settings.Phone,
        };
}
