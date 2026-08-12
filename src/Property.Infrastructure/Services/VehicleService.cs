using Microsoft.EntityFrameworkCore;
using Property.Application.Common.Interfaces;
using Property.Application.Common.Models;
using Property.Application.Features.Structure.Vehicles;
using Property.Domain.Entities;
using Property.Domain.Enums;
using Property.Infrastructure.Common;

namespace Property.Infrastructure.Services;

public sealed class VehicleService : IVehicleService
{
    private readonly IRepository<Vehicle> _vehicles;
    private readonly IRepository<Unit> _units;
    private readonly IRepository<VehicleBrand> _brands;
    private readonly IRepository<HorizontalProperty> _properties;
    private readonly IUnitOfWork _unitOfWork;

    public VehicleService(
        IRepository<Vehicle> vehicles,
        IRepository<Unit> units,
        IRepository<VehicleBrand> brands,
        IRepository<HorizontalProperty> properties,
        IUnitOfWork unitOfWork)
    {
        _vehicles = vehicles;
        _units = units;
        _brands = brands;
        _properties = properties;
        _unitOfWork = unitOfWork;
    }

    public Task<PagedResult<VehicleDto>> ListAsync(ListQuery query, CancellationToken cancellationToken = default) =>
        ListQueryHelpers.ToPagedResultAsync(
            _vehicles.Query().AsNoTracking(),
            query,
            q => PagedEntityQueries.ShapeVehicles(q, query),
            async entities =>
            {
                var unitIds = entities.Where(e => e.UnitId.HasValue).Select(e => e.UnitId!.Value).Distinct().ToList();
                var codes = await _units.Query().AsNoTracking()
                    .Where(u => unitIds.Contains(u.Id))
                    .Select(u => new { u.Id, u.Code })
                    .ToListAsync(cancellationToken);
                var map = codes.ToDictionary(x => x.Id, x => x.Code);
                IReadOnlyList<VehicleDto> items = entities
                    .Select(e => Map(e, e.UnitId is { } id ? map.GetValueOrDefault(id) : null))
                    .ToList();
                return items;
            },
            cancellationToken);

    public async Task<VehicleDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _vehicles.Query().AsNoTracking().FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        if (entity is null)
            return null;

        string? code = null;
        if (entity.UnitId.HasValue)
        {
            code = await _units.Query().AsNoTracking()
                .Where(u => u.Id == entity.UnitId.Value)
                .Select(u => u.Code)
                .FirstOrDefaultAsync(cancellationToken);
        }

        return Map(entity, code);
    }

    public async Task<VehicleDto> CreateAsync(CreateVehicleDto dto, CancellationToken cancellationToken = default)
    {
        await EnsurePropertyAsync(dto.HorizontalPropertyId, cancellationToken);
        Unit? unit = null;
        if (dto.UnitId.HasValue)
        {
            unit = await EnsureParkingUnitAsync(dto.UnitId.Value, dto.HorizontalPropertyId, cancellationToken);
            await EnsureParkingCapacityAsync(unit, excludeVehicleId: null, cancellationToken);
        }

        var brand = await EnsureBrandAsync(dto.VehicleBrandId, cancellationToken);
        var plate = NormalizePlate(dto.Plate);
        await EnsureUniquePlateAsync(plate, excludeId: null, cancellationToken);

        var entity = new Vehicle
        {
            Id = Guid.NewGuid(),
            HorizontalPropertyId = dto.HorizontalPropertyId,
            UnitId = unit?.Id,
            VehicleBrandId = brand.Id,
            VehicleType = dto.VehicleType,
            Brand = brand.Name,
            Model = dto.Model.Trim(),
            Color = dto.Color.Trim(),
            Plate = plate,
            IsActive = true,
        };
        await _vehicles.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(entity, unit?.Code);
    }

    public async Task<VehicleDto?> UpdateAsync(Guid id, UpdateVehicleDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await _vehicles.GetByIdAsync(id, cancellationToken);
        if (entity is null)
            return null;

        await EnsurePropertyAsync(dto.HorizontalPropertyId, cancellationToken);
        Unit? unit = null;
        if (dto.UnitId.HasValue)
        {
            unit = await EnsureParkingUnitAsync(dto.UnitId.Value, dto.HorizontalPropertyId, cancellationToken);
            var movingToDifferentParking = entity.UnitId != unit.Id;
            if (movingToDifferentParking || entity.UnitId is null)
                await EnsureParkingCapacityAsync(unit, excludeVehicleId: id, cancellationToken);
        }

        var brand = await EnsureBrandAsync(dto.VehicleBrandId, cancellationToken);
        var plate = NormalizePlate(dto.Plate);
        await EnsureUniquePlateAsync(plate, excludeId: id, cancellationToken);

        entity.HorizontalPropertyId = dto.HorizontalPropertyId;
        entity.UnitId = unit?.Id;
        entity.VehicleBrandId = brand.Id;
        entity.VehicleType = dto.VehicleType;
        entity.Brand = brand.Name;
        entity.Model = dto.Model.Trim();
        entity.Color = dto.Color.Trim();
        entity.Plate = plate;
        entity.IsActive = dto.IsActive;
        _vehicles.Update(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(entity, unit?.Code);
    }

    public async Task<VehicleDto?> AssignUnitAsync(Guid id, AssignVehicleUnitDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await _vehicles.GetByIdAsync(id, cancellationToken);
        if (entity is null)
            return null;

        var unit = await EnsureParkingUnitAsync(dto.UnitId, entity.HorizontalPropertyId, cancellationToken);
        if (entity.UnitId != unit.Id)
            await EnsureParkingCapacityAsync(unit, excludeVehicleId: id, cancellationToken);

        entity.UnitId = unit.Id;
        _vehicles.Update(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(entity, unit.Code);
    }

    public async Task<VehicleDto?> UnassignUnitAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _vehicles.GetByIdAsync(id, cancellationToken);
        if (entity is null)
            return null;

        entity.UnitId = null;
        _vehicles.Update(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(entity, null);
    }

    public async Task<bool> SoftDeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _vehicles.GetByIdAsync(id, cancellationToken);
        if (entity is null || !entity.IsActive)
            return false;

        entity.IsActive = false;
        _vehicles.Update(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task EnsurePropertyAsync(Guid horizontalPropertyId, CancellationToken cancellationToken)
    {
        var exists = await _properties.Query().AsNoTracking()
            .AnyAsync(p => p.Id == horizontalPropertyId, cancellationToken);
        if (!exists)
            throw new InvalidOperationException("Horizontal property was not found.");
    }

    private async Task<Unit> EnsureParkingUnitAsync(
        Guid unitId,
        Guid horizontalPropertyId,
        CancellationToken cancellationToken)
    {
        var unit = await _units.Query().AsNoTracking().FirstOrDefaultAsync(u => u.Id == unitId, cancellationToken)
            ?? throw new InvalidOperationException("Unit was not found.");

        if (unit.UnitType != UnitType.Parking)
            throw new InvalidOperationException("Los vehículos solo se pueden asociar a unidades tipo parqueadero.");

        if (!unit.IsActive)
            throw new InvalidOperationException("El parqueadero no está activo.");

        if (unit.HorizontalPropertyId != horizontalPropertyId)
            throw new InvalidOperationException("El parqueadero no pertenece a la misma propiedad.");

        return unit;
    }

    private async Task EnsureParkingCapacityAsync(
        Unit parking,
        Guid? excludeVehicleId,
        CancellationToken cancellationToken)
    {
        var capacity = VehicleCapacity(parking);
        var occupied = await _vehicles.Query().AsNoTracking().CountAsync(
            v => v.IsActive &&
                 v.UnitId == parking.Id &&
                 (!excludeVehicleId.HasValue || v.Id != excludeVehicleId.Value),
            cancellationToken);

        if (occupied >= capacity)
        {
            var kind = parking.IsDoubleParking ? "doble (máx. 2)" : "sencillo (máx. 1)";
            throw new InvalidOperationException(
                $"El parqueadero {parking.Code} está lleno: es {kind} y ya tiene {occupied} vehículo(s).");
        }
    }

    internal static int VehicleCapacity(Unit parking) =>
        parking.UnitType == UnitType.Parking
            ? (parking.IsDoubleParking ? 2 : 1)
            : 0;

    private async Task<VehicleBrand> EnsureBrandAsync(Guid brandId, CancellationToken cancellationToken)
    {
        var brand = await _brands.Query().AsNoTracking().FirstOrDefaultAsync(b => b.Id == brandId, cancellationToken)
            ?? throw new InvalidOperationException("La marca de vehículo no existe.");

        if (!brand.IsActive)
            throw new InvalidOperationException("La marca de vehículo no está activa.");

        return brand;
    }

    private async Task EnsureUniquePlateAsync(string plate, Guid? excludeId, CancellationToken cancellationToken)
    {
        var exists = await _vehicles.Query().AsNoTracking().AnyAsync(
            v => v.IsActive &&
                 v.Plate == plate &&
                 (!excludeId.HasValue || v.Id != excludeId.Value),
            cancellationToken);

        if (exists)
            throw new InvalidOperationException($"Ya existe un vehículo activo con placa {plate}.");
    }

    private static string NormalizePlate(string plate) =>
        plate.Trim().ToUpperInvariant().Replace(" ", "", StringComparison.Ordinal);

    private static VehicleDto Map(Vehicle entity, string? unitCode) =>
        new()
        {
            Id = entity.Id,
            HorizontalPropertyId = entity.HorizontalPropertyId,
            UnitId = entity.UnitId,
            UnitCode = unitCode,
            VehicleBrandId = entity.VehicleBrandId,
            VehicleType = entity.VehicleType,
            Brand = entity.Brand,
            Model = entity.Model,
            Color = entity.Color,
            Plate = entity.Plate,
            IsActive = entity.IsActive,
        };
}
