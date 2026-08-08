using Microsoft.EntityFrameworkCore;
using Property.Application.Common.Interfaces;
using Property.Application.Common.Models;
using Property.Application.Features.Structure.HorizontalProperties;
using Property.Domain.Entities;
using Property.Infrastructure.Common;

namespace Property.Infrastructure.Services;

public sealed class HorizontalPropertyService : IHorizontalPropertyService
{
    private readonly IRepository<HorizontalProperty> _properties;
    private readonly IRepository<Neighborhood> _neighborhoods;
    private readonly IUnitOfWork _unitOfWork;

    public HorizontalPropertyService(
        IRepository<HorizontalProperty> properties,
        IRepository<Neighborhood> neighborhoods,
        IUnitOfWork unitOfWork)
    {
        _properties = properties;
        _neighborhoods = neighborhoods;
        _unitOfWork = unitOfWork;
    }

    public Task<PagedResult<HorizontalPropertyDto>> ListAsync(ListQuery query, CancellationToken cancellationToken = default) =>
        ListQueryHelpers.ToPagedResultAsync(
            _properties.Query().AsNoTracking(),
            query,
            q => PagedEntityQueries.ShapeHorizontalProperties(q, query),
            entities => Task.FromResult<IReadOnlyList<HorizontalPropertyDto>>(entities.Select(Map).ToList()),
            cancellationToken);

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
            TradeName = dto.TradeName?.Trim(),
            Nit = dto.Nit?.Trim(),
            AddressLine = dto.AddressLine.Trim(),
            City = dto.City.Trim(),
            Country = string.IsNullOrWhiteSpace(dto.Country) ? "Colombia" : dto.Country.Trim(),
            NeighborhoodId = dto.NeighborhoodId,
            PropertyUseType = dto.PropertyUseType,
            MatriculaInmobiliaria = dto.MatriculaInmobiliaria?.Trim(),
            RegulationNotes = dto.RegulationNotes?.Trim(),
            CoefficientBasisNotes = dto.CoefficientBasisNotes?.Trim(),
            IsActive = true,
        };
        await _properties.AddAsync(entity, cancellationToken);
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
        entity.TradeName = dto.TradeName?.Trim();
        entity.Nit = dto.Nit?.Trim();
        entity.AddressLine = dto.AddressLine.Trim();
        entity.City = dto.City.Trim();
        entity.Country = string.IsNullOrWhiteSpace(dto.Country) ? "Colombia" : dto.Country.Trim();
        entity.NeighborhoodId = dto.NeighborhoodId;
        entity.PropertyUseType = dto.PropertyUseType;
        entity.MatriculaInmobiliaria = dto.MatriculaInmobiliaria?.Trim();
        entity.RegulationNotes = dto.RegulationNotes?.Trim();
        entity.CoefficientBasisNotes = dto.CoefficientBasisNotes?.Trim();
        entity.IsActive = dto.IsActive;
        _properties.Update(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    public async Task<bool> SoftDeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _properties.GetByIdAsync(id, cancellationToken);
        if (entity is null || !entity.IsActive)
            return false;

        entity.IsActive = false;
        _properties.Update(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task EnsureNeighborhoodAsync(Guid? neighborhoodId, CancellationToken cancellationToken)
    {
        if (!neighborhoodId.HasValue)
            return;

        var exists = await _neighborhoods.Query().AnyAsync(n => n.Id == neighborhoodId.Value, cancellationToken);
        if (!exists)
            throw new InvalidOperationException("Neighborhood was not found.");
    }

    private static HorizontalPropertyDto Map(HorizontalProperty entity) =>
        new()
        {
            Id = entity.Id,
            Name = entity.Name,
            TradeName = entity.TradeName,
            Nit = entity.Nit,
            AddressLine = entity.AddressLine,
            City = entity.City,
            Country = entity.Country,
            NeighborhoodId = entity.NeighborhoodId,
            PropertyUseType = entity.PropertyUseType,
            MatriculaInmobiliaria = entity.MatriculaInmobiliaria,
            RegulationNotes = entity.RegulationNotes,
            CoefficientBasisNotes = entity.CoefficientBasisNotes,
            IsActive = entity.IsActive,
        };
}
