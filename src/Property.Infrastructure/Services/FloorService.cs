using Microsoft.EntityFrameworkCore;
using Property.Application.Common.Interfaces;
using Property.Application.Common.Models;
using Property.Application.Features.Structure.Floors;
using Property.Domain.Entities;
using Property.Infrastructure.Common;

namespace Property.Infrastructure.Services;

public sealed class FloorService : IFloorService
{
    private readonly IRepository<Floor> _floors;
    private readonly IRepository<Building> _buildings;
    private readonly IUnitOfWork _unitOfWork;

    public FloorService(
        IRepository<Floor> floors,
        IRepository<Building> buildings,
        IUnitOfWork unitOfWork)
    {
        _floors = floors;
        _buildings = buildings;
        _unitOfWork = unitOfWork;
    }

    public Task<PagedResult<FloorDto>> ListAsync(ListQuery query, CancellationToken cancellationToken = default) =>
        ListQueryHelpers.ToPagedResultAsync(
            _floors.Query().AsNoTracking(),
            query,
            q => PagedEntityQueries.ShapeFloors(q, _buildings.Query().AsNoTracking(), query),
            entities => Task.FromResult<IReadOnlyList<FloorDto>>(entities.Select(Map).ToList()),
            cancellationToken);

    public async Task<FloorDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _floors.Query().AsNoTracking().FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        return entity is null ? null : Map(entity);
    }

    public async Task<FloorDto> CreateAsync(CreateFloorDto dto, CancellationToken cancellationToken = default)
    {
        await EnsureBuildingAsync(dto.BuildingId, cancellationToken);

        var entity = new Floor
        {
            Id = Guid.NewGuid(),
            BuildingId = dto.BuildingId,
            Name = dto.Name.Trim(),
            Number = dto.Number,
            SortOrder = dto.SortOrder,
            IsActive = true,
        };
        await _floors.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    public async Task<FloorDto?> UpdateAsync(Guid id, UpdateFloorDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await _floors.GetByIdAsync(id, cancellationToken);
        if (entity is null)
            return null;

        await EnsureBuildingAsync(dto.BuildingId, cancellationToken);

        entity.BuildingId = dto.BuildingId;
        entity.Name = dto.Name.Trim();
        entity.Number = dto.Number;
        entity.SortOrder = dto.SortOrder;
        entity.IsActive = dto.IsActive;
        _floors.Update(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    public async Task<bool> SoftDeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _floors.GetByIdAsync(id, cancellationToken);
        if (entity is null || !entity.IsActive)
            return false;

        entity.IsActive = false;
        _floors.Update(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task EnsureBuildingAsync(Guid buildingId, CancellationToken cancellationToken)
    {
        var exists = await _buildings.Query().AnyAsync(b => b.Id == buildingId, cancellationToken);
        if (!exists)
            throw new InvalidOperationException("Building was not found.");
    }

    private static FloorDto Map(Floor entity) =>
        new()
        {
            Id = entity.Id,
            BuildingId = entity.BuildingId,
            Name = entity.Name,
            Number = entity.Number,
            SortOrder = entity.SortOrder,
            IsActive = entity.IsActive,
        };
}
