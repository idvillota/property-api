using Microsoft.EntityFrameworkCore;
using Property.Application.Common.Interfaces;
using Property.Application.Common.Models;
using Property.Application.Features.Structure.Units;
using Property.Domain.Common;
using Property.Domain.Entities;
using Property.Infrastructure.Common;

namespace Property.Infrastructure.Services;

public sealed class UnitService : IUnitService
{
    private readonly IRepository<Unit> _units;
    private readonly IRepository<HorizontalProperty> _properties;
    private readonly IRepository<Building> _buildings;
    private readonly IRepository<Floor> _floors;
    private readonly IUnitOfWork _unitOfWork;

    public UnitService(
        IRepository<Unit> units,
        IRepository<HorizontalProperty> properties,
        IRepository<Building> buildings,
        IRepository<Floor> floors,
        IUnitOfWork unitOfWork)
    {
        _units = units;
        _properties = properties;
        _buildings = buildings;
        _floors = floors;
        _unitOfWork = unitOfWork;
    }

    public Task<PagedResult<UnitDto>> ListAsync(ListQuery query, CancellationToken cancellationToken = default) =>
        ListQueryHelpers.ToPagedResultAsync(
            _units.Query().AsNoTracking(),
            query,
            q => PagedEntityQueries.ShapeUnits(q, query),
            entities => Task.FromResult<IReadOnlyList<UnitDto>>(entities.Select(Map).ToList()),
            cancellationToken);

    public async Task<UnitDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _units.Query().AsNoTracking().FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        return entity is null ? null : Map(entity);
    }

    public async Task<UnitDto> CreateAsync(CreateUnitDto dto, CancellationToken cancellationToken = default)
    {
        ValidateStructure(dto.BuildingId, dto.FloorId, dto.LinkedUnitId, unitId: null);
        CoefficientRules.EnsureValid(dto.Coefficient);
        await EnsureReferencesAsync(dto.HorizontalPropertyId, dto.BuildingId, dto.FloorId, dto.LinkedUnitId, cancellationToken);

        var entity = new Unit
        {
            Id = Guid.NewGuid(),
            HorizontalPropertyId = dto.HorizontalPropertyId,
            BuildingId = dto.BuildingId,
            FloorId = dto.FloorId,
            Code = dto.Code.Trim(),
            UnitType = dto.UnitType,
            PrivateBuiltAreaM2 = dto.PrivateBuiltAreaM2,
            PrivateOpenAreaM2 = dto.PrivateOpenAreaM2,
            Coefficient = dto.Coefficient,
            ContributionModuleCode = dto.ContributionModuleCode?.Trim(),
            LinkedUnitId = dto.LinkedUnitId,
            IsBillable = dto.IsBillable,
            IsActive = true,
        };
        await _units.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    public async Task<UnitDto?> UpdateAsync(Guid id, UpdateUnitDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await _units.GetByIdAsync(id, cancellationToken);
        if (entity is null)
            return null;

        ValidateStructure(dto.BuildingId, dto.FloorId, dto.LinkedUnitId, unitId: id);
        CoefficientRules.EnsureValid(dto.Coefficient);
        await EnsureReferencesAsync(dto.HorizontalPropertyId, dto.BuildingId, dto.FloorId, dto.LinkedUnitId, cancellationToken);

        entity.HorizontalPropertyId = dto.HorizontalPropertyId;
        entity.BuildingId = dto.BuildingId;
        entity.FloorId = dto.FloorId;
        entity.Code = dto.Code.Trim();
        entity.UnitType = dto.UnitType;
        entity.PrivateBuiltAreaM2 = dto.PrivateBuiltAreaM2;
        entity.PrivateOpenAreaM2 = dto.PrivateOpenAreaM2;
        entity.Coefficient = dto.Coefficient;
        entity.ContributionModuleCode = dto.ContributionModuleCode?.Trim();
        entity.LinkedUnitId = dto.LinkedUnitId;
        entity.IsBillable = dto.IsBillable;
        entity.IsActive = dto.IsActive;
        _units.Update(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    public async Task<bool> SoftDeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _units.GetByIdAsync(id, cancellationToken);
        if (entity is null || !entity.IsActive)
            return false;

        entity.IsActive = false;
        _units.Update(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<UnitCoefficientSummaryDto> GetCoefficientSummaryAsync(
        Guid horizontalPropertyId,
        CancellationToken cancellationToken = default)
    {
        var exists = await _properties.Query().AnyAsync(p => p.Id == horizontalPropertyId, cancellationToken);
        if (!exists)
            throw new InvalidOperationException("Horizontal property was not found.");

        var activeUnits = _units.Query().AsNoTracking()
            .Where(u => u.HorizontalPropertyId == horizontalPropertyId && u.IsActive);

        var totalCoefficient = await activeUnits.SumAsync(u => (decimal?)u.Coefficient, cancellationToken) ?? 0m;
        var unitCount = await activeUnits.CountAsync(cancellationToken);

        return new UnitCoefficientSummaryDto
        {
            TotalCoefficient = totalCoefficient,
            UnitCount = unitCount,
            RemainingToHundred = CoefficientRules.Total - totalCoefficient,
        };
    }

    private static void ValidateStructure(Guid? buildingId, Guid? floorId, Guid? linkedUnitId, Guid? unitId)
    {
        if (floorId.HasValue && !buildingId.HasValue)
            throw new InvalidOperationException("FloorId requires BuildingId.");

        if (linkedUnitId.HasValue && unitId.HasValue && linkedUnitId.Value == unitId.Value)
            throw new InvalidOperationException("LinkedUnitId cannot equal the unit itself.");
    }

    private async Task EnsureReferencesAsync(
        Guid horizontalPropertyId,
        Guid? buildingId,
        Guid? floorId,
        Guid? linkedUnitId,
        CancellationToken cancellationToken)
    {
        var propertyExists = await _properties.Query().AnyAsync(p => p.Id == horizontalPropertyId, cancellationToken);
        if (!propertyExists)
            throw new InvalidOperationException("Horizontal property was not found.");

        if (buildingId.HasValue)
        {
            var building = await _buildings.Query().AsNoTracking()
                .FirstOrDefaultAsync(b => b.Id == buildingId.Value, cancellationToken)
                ?? throw new InvalidOperationException("Building was not found.");

            if (building.HorizontalPropertyId != horizontalPropertyId)
                throw new InvalidOperationException("Building does not belong to the horizontal property.");
        }

        if (floorId.HasValue)
        {
            var floor = await _floors.Query().AsNoTracking()
                .FirstOrDefaultAsync(f => f.Id == floorId.Value, cancellationToken)
                ?? throw new InvalidOperationException("Floor was not found.");

            if (!buildingId.HasValue || floor.BuildingId != buildingId.Value)
                throw new InvalidOperationException("Floor does not belong to the building.");
        }

        if (linkedUnitId.HasValue)
        {
            var linkedExists = await _units.Query().AnyAsync(u => u.Id == linkedUnitId.Value, cancellationToken);
            if (!linkedExists)
                throw new InvalidOperationException("Linked unit was not found.");
        }
    }

    private static UnitDto Map(Unit entity) =>
        new()
        {
            Id = entity.Id,
            HorizontalPropertyId = entity.HorizontalPropertyId,
            BuildingId = entity.BuildingId,
            FloorId = entity.FloorId,
            Code = entity.Code,
            UnitType = entity.UnitType,
            PrivateBuiltAreaM2 = entity.PrivateBuiltAreaM2,
            PrivateOpenAreaM2 = entity.PrivateOpenAreaM2,
            Coefficient = entity.Coefficient,
            ContributionModuleCode = entity.ContributionModuleCode,
            LinkedUnitId = entity.LinkedUnitId,
            IsBillable = entity.IsBillable,
            IsActive = entity.IsActive,
        };
}
