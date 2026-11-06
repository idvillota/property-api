using Microsoft.EntityFrameworkCore;
using Property.Application.Common.Interfaces;
using Property.Domain.Common;
using Property.Domain.Entities;

namespace Property.Infrastructure.Services;

/// <summary>
/// Soft-deletes a floor or building together with the units that belong to it,
/// then redistributes coefficients once for the property.
/// </summary>
public sealed class StructureNodeRemoval
{
    private readonly IRepository<Building> _buildings;
    private readonly IRepository<Floor> _floors;
    private readonly IRepository<Unit> _units;
    private readonly IUnitOfWork _unitOfWork;

    public StructureNodeRemoval(
        IRepository<Building> buildings,
        IRepository<Floor> floors,
        IRepository<Unit> units,
        IUnitOfWork unitOfWork)
    {
        _buildings = buildings;
        _floors = floors;
        _units = units;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> RemoveFloorAsync(Guid floorId, CancellationToken cancellationToken = default)
    {
        var floor = await _floors.GetByIdAsync(floorId, cancellationToken);
        if (floor is null || !floor.IsActive)
            return false;

        var building = await _buildings.GetByIdAsync(floor.BuildingId, cancellationToken);
        var units = await _units.Query()
            .Where(u => u.FloorId == floorId && u.IsActive)
            .ToListAsync(cancellationToken);

        Retire(units);
        foreach (var unit in units)
            _units.Update(unit);
        if (building is not null)
            await RedistributeAsync(building.HorizontalPropertyId, cancellationToken);

        floor.IsActive = false;
        _floors.Update(floor);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> RemoveBuildingAsync(Guid buildingId, CancellationToken cancellationToken = default)
    {
        var building = await _buildings.GetByIdAsync(buildingId, cancellationToken);
        if (building is null || !building.IsActive)
            return false;

        var floors = await _floors.Query()
            .Where(f => f.BuildingId == buildingId && f.IsActive)
            .ToListAsync(cancellationToken);
        var floorIds = floors.Select(f => f.Id).ToList();

        var units = await _units.Query()
            .Where(u =>
                u.IsActive &&
                (u.BuildingId == buildingId || (u.FloorId != null && floorIds.Contains(u.FloorId.Value))))
            .ToListAsync(cancellationToken);

        Retire(units);
        foreach (var unit in units)
            _units.Update(unit);
        await RedistributeAsync(building.HorizontalPropertyId, cancellationToken);

        foreach (var floor in floors)
        {
            floor.IsActive = false;
            _floors.Update(floor);
        }

        building.IsActive = false;
        _buildings.Update(building);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static void Retire(IEnumerable<Unit> units)
    {
        foreach (var unit in units)
        {
            unit.IsActive = false;
            unit.Coefficient = 0m;
        }
    }

    private async Task RedistributeAsync(Guid horizontalPropertyId, CancellationToken cancellationToken)
    {
        var units = (await _units.Query()
            .Where(u => u.HorizontalPropertyId == horizontalPropertyId && u.IsActive)
            .OrderBy(u => u.Code)
            .ToListAsync(cancellationToken))
            .Where(u => u.IsActive)
            .ToList();

        CoefficientCalculator.ApplyAreaProportional(units);
        foreach (var unit in units)
            _units.Update(unit);
    }
}
