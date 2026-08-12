using Microsoft.EntityFrameworkCore;
using Property.Application.Common.Interfaces;
using Property.Application.Common.Models;
using Property.Application.Features.Structure.Units;
using Property.Domain.Common;
using Property.Domain.Entities;
using Property.Domain.Enums;
using Property.Infrastructure.Common;

namespace Property.Infrastructure.Services;

public sealed class UnitService : IUnitService
{
    private readonly IRepository<Unit> _units;
    private readonly IRepository<HorizontalProperty> _properties;
    private readonly IRepository<Building> _buildings;
    private readonly IRepository<Floor> _floors;
    private readonly IRepository<UnitOccupancy> _occupancies;
    private readonly IRepository<Person> _people;
    private readonly IRepository<Vehicle> _vehicles;
    private readonly IUnitOfWork _unitOfWork;

    public UnitService(
        IRepository<Unit> units,
        IRepository<HorizontalProperty> properties,
        IRepository<Building> buildings,
        IRepository<Floor> floors,
        IRepository<UnitOccupancy> occupancies,
        IRepository<Person> people,
        IRepository<Vehicle> vehicles,
        IUnitOfWork unitOfWork)
    {
        _units = units;
        _properties = properties;
        _buildings = buildings;
        _floors = floors;
        _occupancies = occupancies;
        _people = people;
        _vehicles = vehicles;
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
        var linkedUnitId = NormalizeLinkedUnitId(dto.UnitType, dto.LinkedUnitId);
        await EnsureReferencesAsync(
            dto.HorizontalPropertyId,
            dto.BuildingId,
            dto.FloorId,
            linkedUnitId,
            cancellationToken);
        await EnsureAmenityLinkRulesAsync(
            dto.HorizontalPropertyId,
            dto.UnitType,
            linkedUnitId,
            cancellationToken);

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
            Coefficient = 0m,
            ContributionModuleCode = dto.ContributionModuleCode?.Trim(),
            LinkedUnitId = linkedUnitId,
            ParkingCount = dto.ParkingCount,
            TerraceCount = dto.TerraceCount,
            StorageCount = dto.StorageCount,
            IsDoubleParking = dto.UnitType == UnitType.Parking && dto.IsDoubleParking,
            IsBillable = dto.IsBillable,
            IsPaymentUpToDate = dto.IsPaymentUpToDate,
            IsActive = true,
        };
        await _units.AddAsync(entity, cancellationToken);
        await RecalculateCoefficientsAsync(dto.HorizontalPropertyId, entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    public async Task<UnitDto?> UpdateAsync(Guid id, UpdateUnitDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await _units.GetByIdAsync(id, cancellationToken);
        if (entity is null)
            return null;

        ValidateStructure(dto.BuildingId, dto.FloorId, dto.LinkedUnitId, unitId: id);
        var linkedUnitId = NormalizeLinkedUnitId(dto.UnitType, dto.LinkedUnitId);
        await EnsureReferencesAsync(
            dto.HorizontalPropertyId,
            dto.BuildingId,
            dto.FloorId,
            linkedUnitId,
            cancellationToken);
        await EnsureAmenityLinkRulesAsync(
            dto.HorizontalPropertyId,
            dto.UnitType,
            linkedUnitId,
            cancellationToken);

        entity.HorizontalPropertyId = dto.HorizontalPropertyId;
        entity.BuildingId = dto.BuildingId;
        entity.FloorId = dto.FloorId;
        entity.Code = dto.Code.Trim();
        entity.UnitType = dto.UnitType;
        entity.PrivateBuiltAreaM2 = dto.PrivateBuiltAreaM2;
        entity.PrivateOpenAreaM2 = dto.PrivateOpenAreaM2;
        entity.ContributionModuleCode = dto.ContributionModuleCode?.Trim();
        entity.LinkedUnitId = linkedUnitId;
        entity.ParkingCount = dto.ParkingCount;
        entity.TerraceCount = dto.TerraceCount;
        entity.StorageCount = dto.StorageCount;
        var isDoubleParking = dto.UnitType == UnitType.Parking && dto.IsDoubleParking;
        if (dto.UnitType == UnitType.Parking)
            await EnsureDoubleParkingChangeAllowedAsync(id, isDoubleParking, cancellationToken);
        entity.IsDoubleParking = isDoubleParking;
        entity.IsBillable = dto.IsBillable;
        entity.IsPaymentUpToDate = dto.IsPaymentUpToDate;
        entity.IsActive = dto.IsActive;
        _units.Update(entity);

        await RecalculateCoefficientsAsync(dto.HorizontalPropertyId, newlyAdded: null, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    private async Task EnsureDoubleParkingChangeAllowedAsync(
        Guid parkingUnitId,
        bool isDoubleParking,
        CancellationToken cancellationToken)
    {
        if (isDoubleParking)
            return;

        var activeVehicles = await _vehicles.Query().AsNoTracking()
            .CountAsync(v => v.IsActive && v.UnitId == parkingUnitId, cancellationToken);
        if (activeVehicles > 1)
            throw new InvalidOperationException(
                "No se puede cambiar a parqueadero sencillo: tiene 2 vehículos asociados. Desvincula uno primero.");
    }

    public async Task<bool> SoftDeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _units.GetByIdAsync(id, cancellationToken);
        if (entity is null || !entity.IsActive)
            return false;

        var propertyId = entity.HorizontalPropertyId;
        entity.IsActive = false;
        entity.Coefficient = 0m;
        _units.Update(entity);
        await RecalculateCoefficientsAsync(propertyId, newlyAdded: null, cancellationToken);
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

        return await BuildSummaryAsync(horizontalPropertyId, cancellationToken);
    }

    public async Task<UnitCoefficientSummaryDto> RecalculateCoefficientsAsync(
        Guid horizontalPropertyId,
        CancellationToken cancellationToken = default)
    {
        var exists = await _properties.Query().AnyAsync(p => p.Id == horizontalPropertyId, cancellationToken);
        if (!exists)
            throw new InvalidOperationException("Horizontal property was not found.");

        await RecalculateCoefficientsAsync(horizontalPropertyId, newlyAdded: null, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return await BuildSummaryAsync(horizontalPropertyId, cancellationToken);
    }

    private async Task<UnitCoefficientSummaryDto> BuildSummaryAsync(
        Guid horizontalPropertyId,
        CancellationToken cancellationToken)
    {
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

    /// <summary>
    /// Recalculates coefficients for all active units in the property from private built area.
    /// </summary>
    private async Task RecalculateCoefficientsAsync(
        Guid horizontalPropertyId,
        Unit? newlyAdded,
        CancellationToken cancellationToken)
    {
        var units = await _units.Query()
            .Where(u => u.HorizontalPropertyId == horizontalPropertyId && u.IsActive)
            .OrderBy(u => u.Code)
            .ToListAsync(cancellationToken);

        if (newlyAdded is not null && units.All(u => u.Id != newlyAdded.Id))
            units.Add(newlyAdded);

        CoefficientCalculator.ApplyAreaProportional(units);

        foreach (var unit in units)
        {
            if (newlyAdded is not null && unit.Id == newlyAdded.Id)
                continue;
            _units.Update(unit);
        }
    }

    private static void ValidateStructure(Guid? buildingId, Guid? floorId, Guid? linkedUnitId, Guid? unitId)
    {
        if (floorId.HasValue && !buildingId.HasValue)
            throw new InvalidOperationException("FloorId requires BuildingId.");

        if (linkedUnitId.HasValue && unitId.HasValue && linkedUnitId.Value == unitId.Value)
            throw new InvalidOperationException("LinkedUnitId cannot equal the unit itself.");
    }

    private static bool IsAmenityUnit(UnitType unitType) =>
        unitType is UnitType.Parking or UnitType.Terrace or UnitType.Storage;

    private static bool IsDwellingUnit(UnitType unitType) =>
        unitType is UnitType.Apartment or UnitType.House;

    private static Guid? NormalizeLinkedUnitId(UnitType unitType, Guid? linkedUnitId) =>
        IsAmenityUnit(unitType) ? linkedUnitId : null;

    private async Task EnsureAmenityLinkRulesAsync(
        Guid horizontalPropertyId,
        UnitType unitType,
        Guid? linkedUnitId,
        CancellationToken cancellationToken)
    {
        if (!linkedUnitId.HasValue)
            return;

        if (!IsAmenityUnit(unitType))
            throw new InvalidOperationException(
                "Solo parqueaderos, terrazas y bodegas pueden vincularse a un apartamento o casa.");

        var host = await _units.Query().AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == linkedUnitId.Value, cancellationToken)
            ?? throw new InvalidOperationException("La unidad vinculada no existe.");

        if (host.HorizontalPropertyId != horizontalPropertyId)
            throw new InvalidOperationException("La unidad vinculada no pertenece a la misma propiedad.");

        if (!IsDwellingUnit(host.UnitType))
            throw new InvalidOperationException(
                "Parqueaderos, terrazas y bodegas solo se pueden vincular a apartamentos o casas.");
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

    public async Task<UnitHouseholdDto?> GetHouseholdAsync(Guid unitId, CancellationToken cancellationToken = default)
    {
        var unit = await _units.Query().AsNoTracking().FirstOrDefaultAsync(u => u.Id == unitId, cancellationToken);
        if (unit is null)
            return null;

        string? buildingName = null;
        if (unit.BuildingId is { } buildingId)
        {
            buildingName = await _buildings.Query().AsNoTracking()
                .Where(b => b.Id == buildingId)
                .Select(b => b.Name)
                .FirstOrDefaultAsync(cancellationToken);
        }

        string? floorName = null;
        if (unit.FloorId is { } floorId)
        {
            floorName = await _floors.Query().AsNoTracking()
                .Where(f => f.Id == floorId)
                .Select(f => f.Name)
                .FirstOrDefaultAsync(cancellationToken);
        }

        var rows = await (
            from o in _occupancies.Query().AsNoTracking()
            join p in _people.Query().AsNoTracking() on o.PersonId equals p.Id
            where o.UnitId == unitId && o.IsActive
            orderby o.Role, p.LastName, p.FirstName
            select new { Occupancy = o, Person = p }
        ).ToListAsync(cancellationToken);

        static UnitHouseholdPersonDto ToPerson(UnitOccupancy o, Person p) =>
            new()
            {
                OccupancyId = o.Id,
                PersonId = p.Id,
                FullName = $"{p.FirstName} {p.LastName}".Trim(),
                DocumentLabel = $"{p.DocumentType} {p.DocumentNumber}",
                Email = p.Email,
                Phone = p.Phone,
                Role = o.Role,
                IsPrimary = o.IsPrimary,
                ReceivesBilling = o.ReceivesBilling,
                IsCommunicationResponsible = o.IsCommunicationResponsible,
            };

        var habitants = rows
            .Where(r => r.Occupancy.Role != OccupancyRole.Owner)
            .Select(r => ToPerson(r.Occupancy, r.Person))
            .ToList();

        var habitantIds = habitants.Select(h => h.PersonId).ToHashSet();
        var owners = rows
            .Where(r => r.Occupancy.Role == OccupancyRole.Owner && !habitantIds.Contains(r.Person.Id))
            .Select(r => ToPerson(r.Occupancy, r.Person))
            .ToList();

        var relatedAmenities = await _units.Query().AsNoTracking()
            .Where(u => u.LinkedUnitId == unitId && u.IsActive)
            .OrderBy(u => u.Code)
            .Select(u => new { u.Id, u.Code, u.UnitType })
            .ToListAsync(cancellationToken);

        var relatedParkings = relatedAmenities
            .Where(u => u.UnitType == UnitType.Parking)
            .Select(u => new UnitAmenityLinkDto { UnitId = u.Id, Code = u.Code })
            .ToList();

        var relatedTerraces = relatedAmenities
            .Where(u => u.UnitType == UnitType.Terrace)
            .Select(u => new UnitAmenityLinkDto { UnitId = u.Id, Code = u.Code })
            .ToList();

        var relatedStorages = relatedAmenities
            .Where(u => u.UnitType == UnitType.Storage)
            .Select(u => new UnitAmenityLinkDto { UnitId = u.Id, Code = u.Code })
            .ToList();

        return new UnitHouseholdDto
        {
            UnitId = unit.Id,
            UnitCode = unit.Code,
            UnitType = unit.UnitType,
            BuildingId = unit.BuildingId,
            BuildingName = buildingName,
            FloorId = unit.FloorId,
            FloorName = floorName,
            PrivateBuiltAreaM2 = unit.PrivateBuiltAreaM2,
            Coefficient = unit.Coefficient,
            ParkingCount = relatedParkings.Count,
            TerraceCount = relatedTerraces.Count,
            StorageCount = relatedStorages.Count,
            Owners = owners,
            Habitants = habitants,
            RelatedParkings = relatedParkings,
            RelatedTerraces = relatedTerraces,
            RelatedStorages = relatedStorages,
        };
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
            ParkingCount = entity.ParkingCount,
            TerraceCount = entity.TerraceCount,
            StorageCount = entity.StorageCount,
            IsDoubleParking = entity.IsDoubleParking,
            IsBillable = entity.IsBillable,
            IsPaymentUpToDate = entity.IsPaymentUpToDate,
            IsActive = entity.IsActive,
        };
}
