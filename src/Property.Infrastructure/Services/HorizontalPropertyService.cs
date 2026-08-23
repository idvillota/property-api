using Microsoft.EntityFrameworkCore;
using Property.Application.Common.Interfaces;
using Property.Application.Common.Models;
using Property.Application.Features.Structure.HorizontalProperties;
using Property.Domain.Entities;
using Property.Infrastructure.Common;
using Property.Infrastructure.Persistence;

namespace Property.Infrastructure.Services;

public sealed class HorizontalPropertyService : IHorizontalPropertyService
{
    private readonly IRepository<HorizontalProperty> _properties;
    private readonly IRepository<Neighborhood> _neighborhoods;
    private readonly ApplicationDbContext _db;
    private readonly ICurrentTenantContext _tenantContext;
    private readonly IUnitOfWork _unitOfWork;

    public HorizontalPropertyService(
        IRepository<HorizontalProperty> properties,
        IRepository<Neighborhood> neighborhoods,
        ApplicationDbContext db,
        ICurrentTenantContext tenantContext,
        IUnitOfWork unitOfWork)
    {
        _properties = properties;
        _neighborhoods = neighborhoods;
        _db = db;
        _tenantContext = tenantContext;
        _unitOfWork = unitOfWork;
    }

    public Task<PagedResult<HorizontalPropertyDto>> ListAsync(ListQuery query, CancellationToken cancellationToken = default) =>
        ListQueryHelpers.ToPagedResultAsync(
            _properties.Query().AsNoTracking(),
            query,
            q => PagedEntityQueries.ShapeHorizontalProperties(q, query),
            entities => Task.FromResult<IReadOnlyList<HorizontalPropertyDto>>(entities.Select(Map).ToList()),
            cancellationToken);

    public async Task<HorizontalPropertyDto?> GetCurrentAsync(CancellationToken cancellationToken = default)
    {
        var entity = await _properties.Query().AsNoTracking().FirstOrDefaultAsync(cancellationToken);
        return entity is null ? null : Map(entity);
    }

    public async Task<HorizontalPropertyDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _properties.Query().AsNoTracking().FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        return entity is null ? null : Map(entity);
    }

    public async Task<HorizontalPropertyDto> CreateAsync(CreateHorizontalPropertyDto dto, CancellationToken cancellationToken = default)
    {
        await EnsureNeighborhoodAsync(dto.NeighborhoodId, cancellationToken);

        var entity = new HorizontalProperty
        {
            Id = Guid.NewGuid(),
            Name = dto.Name.Trim(),
            TradeName = TrimOrNull(dto.TradeName),
            LegalName = TrimOrNull(dto.LegalName),
            Nit = TrimOrNull(dto.Nit),
            TaxRegime = TrimOrNull(dto.TaxRegime),
            LegalRepresentative = TrimOrNull(dto.LegalRepresentative),
            AddressLine = dto.AddressLine.Trim(),
            City = dto.City.Trim(),
            Country = string.IsNullOrWhiteSpace(dto.Country) ? "Colombia" : dto.Country.Trim(),
            PostalCode = TrimOrNull(dto.PostalCode),
            Phone = TrimOrNull(dto.Phone),
            NeighborhoodId = dto.NeighborhoodId,
            PropertyUseType = dto.PropertyUseType,
            MatriculaInmobiliaria = TrimOrNull(dto.MatriculaInmobiliaria),
            RegulationNotes = TrimOrNull(dto.RegulationNotes),
            CoefficientBasisNotes = TrimOrNull(dto.CoefficientBasisNotes),
            IsActive = true,
        };
        await _properties.AddAsync(entity, cancellationToken);
        await SyncTenantSettingsFromPropertyAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    public async Task<HorizontalPropertyDto?> UpdateAsync(Guid id, UpdateHorizontalPropertyDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await _properties.GetByIdAsync(id, cancellationToken);
        if (entity is null)
            return null;

        await EnsureNeighborhoodAsync(dto.NeighborhoodId, cancellationToken);

        entity.Name = dto.Name.Trim();
        entity.TradeName = TrimOrNull(dto.TradeName);
        entity.LegalName = TrimOrNull(dto.LegalName);
        entity.Nit = TrimOrNull(dto.Nit);
        entity.TaxRegime = TrimOrNull(dto.TaxRegime);
        entity.LegalRepresentative = TrimOrNull(dto.LegalRepresentative);
        entity.AddressLine = dto.AddressLine.Trim();
        entity.City = dto.City.Trim();
        entity.Country = string.IsNullOrWhiteSpace(dto.Country) ? "Colombia" : dto.Country.Trim();
        entity.PostalCode = TrimOrNull(dto.PostalCode);
        entity.Phone = TrimOrNull(dto.Phone);
        entity.NeighborhoodId = dto.NeighborhoodId;
        entity.PropertyUseType = dto.PropertyUseType;
        entity.MatriculaInmobiliaria = TrimOrNull(dto.MatriculaInmobiliaria);
        entity.RegulationNotes = TrimOrNull(dto.RegulationNotes);
        entity.CoefficientBasisNotes = TrimOrNull(dto.CoefficientBasisNotes);
        entity.IsActive = dto.IsActive;
        _properties.Update(entity);
        await SyncTenantSettingsFromPropertyAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    public Task<bool> SoftDeleteAsync(Guid id, CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException(
            "The tenant property cannot be deleted. Deactivate it from configuration if needed.");

    private async Task SyncTenantSettingsFromPropertyAsync(HorizontalProperty property, CancellationToken cancellationToken)
    {
        var tenantId = property.TenantId != Guid.Empty
            ? property.TenantId
            : _tenantContext.TenantId
              ?? throw new InvalidOperationException("Tenant context is not available.");

        var settings = await _db.TenantSettings.FirstOrDefaultAsync(s => s.TenantId == tenantId, cancellationToken);
        if (settings is null)
        {
            settings = new TenantSettings { TenantId = tenantId };
            await _db.TenantSettings.AddAsync(settings, cancellationToken);
        }

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

    private async Task EnsureNeighborhoodAsync(Guid? neighborhoodId, CancellationToken cancellationToken)
    {
        if (!neighborhoodId.HasValue)
            return;

        var exists = await _neighborhoods.Query().AnyAsync(n => n.Id == neighborhoodId.Value, cancellationToken);
        if (!exists)
            throw new InvalidOperationException("Neighborhood was not found.");
    }

    private static string? TrimOrNull(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static HorizontalPropertyDto Map(HorizontalProperty entity) =>
        new()
        {
            Id = entity.Id,
            Name = entity.Name,
            TradeName = entity.TradeName,
            LegalName = entity.LegalName,
            Nit = entity.Nit,
            TaxRegime = entity.TaxRegime,
            LegalRepresentative = entity.LegalRepresentative,
            AddressLine = entity.AddressLine,
            City = entity.City,
            Country = entity.Country,
            PostalCode = entity.PostalCode,
            Phone = entity.Phone,
            NeighborhoodId = entity.NeighborhoodId,
            PropertyUseType = entity.PropertyUseType,
            MatriculaInmobiliaria = entity.MatriculaInmobiliaria,
            RegulationNotes = entity.RegulationNotes,
            CoefficientBasisNotes = entity.CoefficientBasisNotes,
            IsActive = entity.IsActive,
        };
}
