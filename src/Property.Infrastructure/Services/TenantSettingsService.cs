using Microsoft.EntityFrameworkCore;
using Property.Application.Common.Interfaces;
using Property.Application.Features.Organization.TenantSettings;
using Property.Domain.Entities;
using Property.Domain.Enums;
using Property.Infrastructure.Persistence;

namespace Property.Infrastructure.Services;

/// <summary>
/// Tenant identity settings mirror the single HorizontalProperty for the tenant.
/// Dashboard layout JSON and module flags remain on TenantSettings.
/// </summary>
public sealed class TenantSettingsService : ITenantSettingsService
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentTenantContext _tenantContext;
    private readonly ITenantModuleService _modules;

    public TenantSettingsService(
        ApplicationDbContext db,
        ICurrentTenantContext tenantContext,
        ITenantModuleService modules)
    {
        _db = db;
        _tenantContext = tenantContext;
        _modules = modules;
    }

    public async Task<TenantSettingsDto> GetAsync(CancellationToken cancellationToken = default)
    {
        var flags = await _modules.GetFlagsAsync(cancellationToken);
        var property = await _db.HorizontalProperties.AsNoTracking().FirstOrDefaultAsync(cancellationToken);
        if (property is not null)
        {
            var dto = MapFromProperty(property);
            dto.Modules = flags;
            return dto;
        }

        var settings = await GetOrCreateSettingsRowAsync(cancellationToken);
        var empty = Map(settings);
        empty.Modules = flags;
        return empty;
    }

    public async Task<TenantSettingsDto> UpdateAsync(
        UpdateTenantSettingsDto dto,
        CancellationToken cancellationToken = default)
    {
        var property = await _db.HorizontalProperties.FirstOrDefaultAsync(cancellationToken);
        if (property is null)
            throw new InvalidOperationException(
                "Register the tenant property before saving configuration.");

        if (dto.NeighborhoodId.HasValue)
        {
            var neighborhoodExists = await _db.Neighborhoods.AnyAsync(
                n => n.Id == dto.NeighborhoodId.Value,
                cancellationToken);
            if (!neighborhoodExists)
                throw new InvalidOperationException("Neighborhood was not found.");
        }

        property.Name = dto.Name.Trim();
        property.TradeName = TrimOrNull(dto.TradeName);
        property.LegalName = TrimOrNull(dto.LegalName);
        property.TaxRegime = TrimOrNull(dto.TaxRegime);
        property.Nit = TrimOrNull(dto.TaxId);
        property.LegalRepresentative = TrimOrNull(dto.LegalRepresentative);
        property.AddressLine = dto.AddressLine.Trim();
        property.City = dto.City.Trim();
        property.Country = string.IsNullOrWhiteSpace(dto.Country) ? "Colombia" : dto.Country.Trim();
        property.PostalCode = TrimOrNull(dto.PostalCode);
        property.Phone = TrimOrNull(dto.Phone);
        property.NeighborhoodId = dto.NeighborhoodId;
        if (dto.PropertyUseType.HasValue)
            property.PropertyUseType = dto.PropertyUseType.Value;
        property.MatriculaInmobiliaria = TrimOrNull(dto.MatriculaInmobiliaria);
        property.RegulationNotes = TrimOrNull(dto.RegulationNotes);
        property.CoefficientBasisNotes = TrimOrNull(dto.CoefficientBasisNotes);
        property.IsActive = dto.IsActive;
        _db.HorizontalProperties.Update(property);

        var settings = await GetOrCreateSettingsRowAsync(cancellationToken);
        ApplyIdentityToSettings(settings, property);
        if (dto.Modules is not null)
            TenantModuleService.Apply(settings, dto.Modules);
        _db.TenantSettings.Update(settings);
        await _db.SaveChangesAsync(cancellationToken);

        var result = MapFromProperty(property);
        result.Modules = TenantModuleService.Map(settings);
        return result;
    }

    private static void ApplyIdentityToSettings(TenantSettings settings, HorizontalProperty property)
    {
        settings.TradeName = property.TradeName ?? property.Name;
        settings.LegalName = property.LegalName ?? property.Name;
        settings.TaxRegime = property.TaxRegime ?? string.Empty;
        settings.TaxId = property.Nit ?? string.Empty;
        settings.LegalRepresentative = property.LegalRepresentative;
        settings.AddressLine = property.AddressLine;
        settings.City = property.City;
        settings.Country = property.Country;
        settings.PostalCode = property.PostalCode;
        settings.Phone = property.Phone;
    }

    private async Task<TenantSettings> GetOrCreateSettingsRowAsync(CancellationToken cancellationToken)
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

    private static string? TrimOrNull(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static TenantSettingsDto MapFromProperty(HorizontalProperty property) =>
        new()
        {
            PropertyId = property.Id,
            Name = property.Name,
            TradeName = property.TradeName ?? string.Empty,
            LegalName = property.LegalName ?? string.Empty,
            TaxRegime = property.TaxRegime ?? string.Empty,
            TaxId = property.Nit ?? string.Empty,
            LegalRepresentative = property.LegalRepresentative,
            AddressLine = property.AddressLine,
            City = property.City,
            Country = property.Country,
            PostalCode = property.PostalCode,
            Phone = property.Phone,
            NeighborhoodId = property.NeighborhoodId,
            PropertyUseType = property.PropertyUseType,
            MatriculaInmobiliaria = property.MatriculaInmobiliaria,
            RegulationNotes = property.RegulationNotes,
            CoefficientBasisNotes = property.CoefficientBasisNotes,
            IsActive = property.IsActive,
            HasProperty = true,
        };

    private static TenantSettingsDto Map(TenantSettings settings) =>
        new()
        {
            PropertyId = null,
            Name = string.Empty,
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
            NeighborhoodId = null,
            PropertyUseType = PropertyUseType.Residential,
            MatriculaInmobiliaria = null,
            RegulationNotes = null,
            CoefficientBasisNotes = null,
            IsActive = true,
            HasProperty = false,
        };
}
