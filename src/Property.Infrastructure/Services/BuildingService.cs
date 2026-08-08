using Microsoft.EntityFrameworkCore;
using Property.Application.Common.Interfaces;
using Property.Application.Common.Models;
using Property.Application.Features.Structure.Buildings;
using Property.Domain.Entities;
using Property.Infrastructure.Common;

namespace Property.Infrastructure.Services;

public sealed class BuildingService : IBuildingService
{
    private readonly IRepository<Building> _buildings;
    private readonly IRepository<HorizontalProperty> _properties;
    private readonly IUnitOfWork _unitOfWork;

    public BuildingService(
        IRepository<Building> buildings,
        IRepository<HorizontalProperty> properties,
        IUnitOfWork unitOfWork)
    {
        _buildings = buildings;
        _properties = properties;
        _unitOfWork = unitOfWork;
    }

    public Task<PagedResult<BuildingDto>> ListAsync(ListQuery query, CancellationToken cancellationToken = default) =>
        ListQueryHelpers.ToPagedResultAsync(
            _buildings.Query().AsNoTracking(),
            query,
            q => PagedEntityQueries.ShapeBuildings(q, query),
            entities => Task.FromResult<IReadOnlyList<BuildingDto>>(entities.Select(Map).ToList()),
            cancellationToken);

    public async Task<BuildingDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _buildings.Query().AsNoTracking().FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        return entity is null ? null : Map(entity);
    }

    public async Task<BuildingDto> CreateAsync(CreateBuildingDto dto, CancellationToken cancellationToken = default)
    {
        await EnsureHorizontalPropertyAsync(dto.HorizontalPropertyId, cancellationToken);

        var entity = new Building
        {
            Id = Guid.NewGuid(),
            HorizontalPropertyId = dto.HorizontalPropertyId,
            Name = dto.Name.Trim(),
            Code = dto.Code?.Trim(),
            SortOrder = dto.SortOrder,
            IsActive = true,
        };
        await _buildings.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    public async Task<BuildingDto?> UpdateAsync(Guid id, UpdateBuildingDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await _buildings.GetByIdAsync(id, cancellationToken);
        if (entity is null)
            return null;

        await EnsureHorizontalPropertyAsync(dto.HorizontalPropertyId, cancellationToken);

        entity.HorizontalPropertyId = dto.HorizontalPropertyId;
        entity.Name = dto.Name.Trim();
        entity.Code = dto.Code?.Trim();
        entity.SortOrder = dto.SortOrder;
        entity.IsActive = dto.IsActive;
        _buildings.Update(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    public async Task<bool> SoftDeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _buildings.GetByIdAsync(id, cancellationToken);
        if (entity is null || !entity.IsActive)
            return false;

        entity.IsActive = false;
        _buildings.Update(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task EnsureHorizontalPropertyAsync(Guid horizontalPropertyId, CancellationToken cancellationToken)
    {
        var exists = await _properties.Query().AnyAsync(p => p.Id == horizontalPropertyId, cancellationToken);
        if (!exists)
            throw new InvalidOperationException("Horizontal property was not found.");
    }

    private static BuildingDto Map(Building entity) =>
        new()
        {
            Id = entity.Id,
            HorizontalPropertyId = entity.HorizontalPropertyId,
            Name = entity.Name,
            Code = entity.Code,
            SortOrder = entity.SortOrder,
            IsActive = entity.IsActive,
        };
}
