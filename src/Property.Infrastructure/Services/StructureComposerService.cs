using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Property.Application.Common.Interfaces;
using Property.Application.Features.Structure.Composer;
using Property.Application.Features.Structure.Units;
using Property.Domain.Common;
using Property.Domain.Entities;
using Property.Domain.Enums;
using Property.Infrastructure.Persistence;

namespace Property.Infrastructure.Services;

public sealed class StructureComposerService : IStructureComposerService
{
    private static readonly Regex PadPattern = new(@"\{u:0+\}", RegexOptions.Compiled);
    private const int MaxTotalUnits = 2500;

    private readonly ApplicationDbContext _db;

    public StructureComposerService(ApplicationDbContext db) => _db = db;

    public async Task<StructureComposerStatusDto> GetStatusAsync(
        Guid horizontalPropertyId,
        CancellationToken cancellationToken = default)
    {
        await EnsurePropertyAsync(horizontalPropertyId, cancellationToken);

        var buildingCount = await _db.Buildings.CountAsync(
            b => b.HorizontalPropertyId == horizontalPropertyId && b.IsActive,
            cancellationToken);
        var floorCount = await (
            from f in _db.Floors
            join b in _db.Buildings on f.BuildingId equals b.Id
            where b.HorizontalPropertyId == horizontalPropertyId && f.IsActive && b.IsActive
            select f.Id
        ).CountAsync(cancellationToken);
        var unitCount = await _db.Units.CountAsync(
            u => u.HorizontalPropertyId == horizontalPropertyId && u.IsActive,
            cancellationToken);

        return new StructureComposerStatusDto
        {
            HorizontalPropertyId = horizontalPropertyId,
            HasStructure = buildingCount > 0 || unitCount > 0,
            BuildingCount = buildingCount,
            FloorCount = floorCount,
            UnitCount = unitCount,
        };
    }

    public async Task<StructureWorkspaceDto> GetWorkspaceAsync(
        Guid horizontalPropertyId,
        CancellationToken cancellationToken = default)
    {
        await EnsurePropertyAsync(horizontalPropertyId, cancellationToken);

        var buildings = await _db.Buildings.AsNoTracking()
            .Where(b => b.HorizontalPropertyId == horizontalPropertyId && b.IsActive)
            .OrderBy(b => b.SortOrder)
            .ThenBy(b => b.Name)
            .ToListAsync(cancellationToken);

        var buildingIds = buildings.Select(b => b.Id).ToList();

        var floors = await _db.Floors.AsNoTracking()
            .Where(f => buildingIds.Contains(f.BuildingId) && f.IsActive)
            .OrderBy(f => f.SortOrder)
            .ThenBy(f => f.Number)
            .ThenBy(f => f.Name)
            .ToListAsync(cancellationToken);

        var units = await _db.Units.AsNoTracking()
            .Where(u => u.HorizontalPropertyId == horizontalPropertyId && u.IsActive)
            .OrderBy(u => u.Code)
            .ToListAsync(cancellationToken);

        var buildingDtos = buildings.Select(b =>
        {
            var buildingFloors = floors
                .Where(f => f.BuildingId == b.Id)
                .Select(f => new StructureWorkspaceFloorDto
                {
                    Id = f.Id,
                    Name = f.Name,
                    Number = f.Number,
                    SortOrder = f.SortOrder,
                    Units = units.Where(u => u.FloorId == f.Id).Select(MapUnit).ToList(),
                })
                .ToList();

            var orphans = units
                .Where(u => u.BuildingId == b.Id && u.FloorId == null)
                .Select(MapUnit)
                .ToList();
            if (orphans.Count > 0)
            {
                buildingFloors.Add(new StructureWorkspaceFloorDto
                {
                    Id = Guid.Empty,
                    Name = "Sin piso",
                    Number = null,
                    SortOrder = int.MaxValue,
                    Units = orphans,
                });
            }

            return new StructureWorkspaceBuildingDto
            {
                Id = b.Id,
                Name = b.Name,
                Code = b.Code,
                SortOrder = b.SortOrder,
                Floors = buildingFloors,
            };
        }).ToList();

        return new StructureWorkspaceDto
        {
            HorizontalPropertyId = horizontalPropertyId,
            HasStructure = buildings.Count > 0 || units.Count > 0,
            BuildingCount = buildings.Count,
            FloorCount = floors.Count,
            UnitCount = units.Count,
            Buildings = buildingDtos,
        };
    }

    private static UnitDto MapUnit(Unit entity) =>
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

    public async Task<StructureComposerPreviewDto> PreviewAsync(
        StructureComposerRequestDto request,
        CancellationToken cancellationToken = default)
    {
        await EnsurePropertyAsync(request.HorizontalPropertyId, cancellationToken);
        return ToPreview(BuildPlan(request));
    }

    public async Task<StructureComposerCommitResultDto> CommitAsync(
        StructureComposerRequestDto request,
        CancellationToken cancellationToken = default)
    {
        await EnsurePropertyAsync(request.HorizontalPropertyId, cancellationToken);

        var status = await GetStatusAsync(request.HorizontalPropertyId, cancellationToken);
        if (status.HasStructure)
            throw new InvalidOperationException(
                "This property already has structure. Use advanced edit to change individual bloques, floors, or units.");

        var plan = BuildPlan(request);
        var allUnits = plan.AllUnits.ToList();
        if (allUnits.Count == 0)
            throw new InvalidOperationException("Nothing to create. Add at least one unit.");

        var utcNow = DateTime.UtcNow;

        var strategy = _db.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
        var buildingsCreated = 0;
        var floorsCreated = 0;
        var codeToUnitId = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
        Guid? firstBuildingId = null;

        await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);

        foreach (var buildingPlan in plan.Buildings)
        {
            var building = new Building
            {
                Id = Guid.NewGuid(),
                HorizontalPropertyId = request.HorizontalPropertyId,
                Name = buildingPlan.Name,
                Code = buildingPlan.Code,
                SortOrder = buildingPlan.SortOrder,
                IsActive = true,
                CreatedAtUtc = utcNow,
            };
            firstBuildingId ??= building.Id;
            await _db.Buildings.AddAsync(building, cancellationToken);
            buildingsCreated++;

            foreach (var floorPlan in buildingPlan.Floors)
            {
                var floor = new Floor
                {
                    Id = Guid.NewGuid(),
                    BuildingId = building.Id,
                    Name = floorPlan.Name,
                    Number = floorPlan.Number,
                    SortOrder = floorPlan.SortOrder,
                    IsActive = true,
                    CreatedAtUtc = utcNow,
                };
                await _db.Floors.AddAsync(floor, cancellationToken);
                floorsCreated++;

                foreach (var unitPlan in floorPlan.Units)
                {
                    var unit = new Unit
                    {
                        Id = Guid.NewGuid(),
                        HorizontalPropertyId = request.HorizontalPropertyId,
                        BuildingId = building.Id,
                        FloorId = floor.Id,
                        Code = unitPlan.Code,
                        UnitType = unitPlan.UnitType,
                        PrivateBuiltAreaM2 = unitPlan.AreaM2,
                        Coefficient = 0m,
                        ParkingCount = unitPlan.ParkingCount,
                        TerraceCount = unitPlan.TerraceCount,
                        StorageCount = unitPlan.StorageCount,
                        IsBillable = true,
                        IsActive = true,
                        CreatedAtUtc = utcNow,
                    };
                    await _db.Units.AddAsync(unit, cancellationToken);
                    codeToUnitId[unit.Code] = unit.Id;
                }
            }
        }

        foreach (var unitPlan in plan.AncillaryUnits)
        {
            Guid? linkedId = null;
            if (!string.IsNullOrWhiteSpace(unitPlan.LinkedUnitCode) &&
                codeToUnitId.TryGetValue(unitPlan.LinkedUnitCode, out var linked))
                linkedId = linked;

            var unit = new Unit
            {
                Id = Guid.NewGuid(),
                HorizontalPropertyId = request.HorizontalPropertyId,
                BuildingId = firstBuildingId,
                FloorId = null,
                Code = unitPlan.Code,
                UnitType = unitPlan.UnitType,
                PrivateBuiltAreaM2 = unitPlan.AreaM2,
                Coefficient = 0m,
                LinkedUnitId = linkedId,
                IsBillable = true,
                IsActive = true,
                CreatedAtUtc = utcNow,
            };
            await _db.Units.AddAsync(unit, cancellationToken);
            codeToUnitId[unit.Code] = unit.Id;
        }

        await _db.SaveChangesAsync(cancellationToken);

        var units = await _db.Units
            .Where(u => u.HorizontalPropertyId == request.HorizontalPropertyId && u.IsActive)
            .OrderBy(u => u.Code)
            .ToListAsync(cancellationToken);

        CoefficientCalculator.ApplyAreaProportional(units);
        await _db.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);

        return new StructureComposerCommitResultDto
        {
            BuildingsCreated = buildingsCreated,
            FloorsCreated = floorsCreated,
            UnitsCreated = units.Count,
            TotalCoefficient = units.Sum(u => u.Coefficient),
        };
        });
    }

    private async Task EnsurePropertyAsync(Guid horizontalPropertyId, CancellationToken cancellationToken)
    {
        var exists = await _db.HorizontalProperties.AnyAsync(p => p.Id == horizontalPropertyId, cancellationToken);
        if (!exists)
            throw new InvalidOperationException("Horizontal property was not found.");
    }

    private sealed class Plan
    {
        public List<BuildingPlan> Buildings { get; } = [];
        public List<UnitPlan> AncillaryUnits { get; } = [];
        public List<string> Warnings { get; } = [];

        public IEnumerable<UnitPlan> AllUnits =>
            Buildings.SelectMany(b => b.Floors.SelectMany(f => f.Units)).Concat(AncillaryUnits);
    }

    private sealed class BuildingPlan
    {
        public string Name { get; set; } = string.Empty;
        public string? Code { get; set; }
        public int SortOrder { get; set; }
        public List<FloorPlan> Floors { get; } = [];
    }

    private sealed class FloorPlan
    {
        public string Name { get; set; } = string.Empty;
        public int? Number { get; set; }
        public int SortOrder { get; set; }
        public List<UnitPlan> Units { get; } = [];
    }

    private sealed class UnitPlan
    {
        public string Code { get; set; } = string.Empty;
        public UnitType UnitType { get; set; }
        public decimal AreaM2 { get; set; }
        public int ParkingCount { get; set; }
        public int TerraceCount { get; set; }
        public int StorageCount { get; set; }
        public string? BuildingCode { get; set; }
        public string? FloorName { get; set; }
        public string? LinkedUnitCode { get; set; }
    }

    private static Plan BuildPlan(StructureComposerRequestDto request)
    {
        if (request.Buildings.Count == 0)
            throw new InvalidOperationException("Add at least one bloque.");

        var plan = new Plan();
        var usedCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var dwellingCodes = new List<string>();

        for (var bi = 0; bi < request.Buildings.Count; bi++)
        {
            var b = request.Buildings[bi];
            ValidateBuilding(b, bi);

            var buildingPlan = new BuildingPlan
            {
                Name = b.Name.Trim(),
                Code = string.IsNullOrWhiteSpace(b.Code) ? null : b.Code.Trim(),
                SortOrder = bi + 1,
            };

            var floorNumbers = new List<(int? Number, string Name, int Sort)>();
            var sort = 0;
            if (b.IncludeBasement)
                floorNumbers.Add((0, "Sótano", sort++));

            for (var n = b.FloorFrom; n <= b.FloorTo; n++)
                floorNumbers.Add((n, $"Piso {n}", sort++));

            if (b.IncludeRooftop)
                floorNumbers.Add((b.FloorTo + 1, "Azotea", sort++));

            var slots = ResolveSlots(b);

            foreach (var (number, name, floorSort) in floorNumbers)
            {
                var floorPlan = new FloorPlan
                {
                    Name = name,
                    Number = number,
                    SortOrder = floorSort,
                };

                for (var u = 1; u <= b.UnitsPerFloor; u++)
                {
                    var slot = slots[u - 1];
                    var floorKey = number ?? 0;
                    var code = ApplyPattern(b.CodePattern, buildingPlan.Code, floorKey, u, slot.CodeSuffix);

                    if (!usedCodes.Add(code))
                        throw new InvalidOperationException($"Duplicate unit code generated: {code}.");

                    floorPlan.Units.Add(new UnitPlan
                    {
                        Code = code,
                        UnitType = slot.UnitType,
                        AreaM2 = slot.AreaM2,
                        BuildingCode = buildingPlan.Code,
                        FloorName = name,
                    });

                    if (slot.UnitType is UnitType.Apartment or UnitType.House)
                        dwellingCodes.Add(code);
                }

                buildingPlan.Floors.Add(floorPlan);
            }

            plan.Buildings.Add(buildingPlan);
        }

        ApplyOverrides(plan, request.UnitOverrides, usedCodes, dwellingCodes);

        if (request.Parking is { Count: > 0 } parking)
            AddAncillary(plan, usedCodes, dwellingCodes, parking.Count, parking.Prefix, parking.AreaM2, UnitType.Parking, parking.LinkToApartmentsInOrder);

        if (request.Storage is { Count: > 0 } storage)
            AddAncillary(plan, usedCodes, dwellingCodes, storage.Count, storage.Prefix, storage.AreaM2, UnitType.Storage, link: false);

        var total = plan.AllUnits.Count();
        if (total > MaxTotalUnits)
            throw new InvalidOperationException($"Too many units ({total}). Maximum is {MaxTotalUnits}.");

        if (total > 300)
            plan.Warnings.Add($"Se generarán {total} unidades. Confirma que es correcto.");

        if (plan.AllUnits.Any(u => u.AreaM2 <= 0))
            plan.Warnings.Add("Hay unidades con área 0 m²; el coeficiente de esas unidades será 0.");

        return plan;
    }

    private static void ValidateBuilding(StructureComposerBuildingDto b, int index)
    {
        if (string.IsNullOrWhiteSpace(b.Name))
            throw new InvalidOperationException($"Bloque #{index + 1}: nombre obligatorio.");
        if (b.FloorTo < b.FloorFrom)
            throw new InvalidOperationException($"Bloque '{b.Name}': piso hasta debe ser ≥ piso desde.");
        if (b.UnitsPerFloor < 1)
            throw new InvalidOperationException($"Bloque '{b.Name}': unidades por piso debe ser ≥ 1.");
        if (b.Slots is { Count: > 0 } && b.Slots.Count != b.UnitsPerFloor)
            throw new InvalidOperationException(
                $"Bloque '{b.Name}': la matriz de tipologías debe tener {b.UnitsPerFloor} posiciones.");
        if (string.IsNullOrWhiteSpace(b.CodePattern))
            throw new InvalidOperationException($"Bloque '{b.Name}': patrón de código obligatorio.");
    }

    private static List<StructureComposerSlotDto> ResolveSlots(StructureComposerBuildingDto b)
    {
        if (b.Slots is { Count: > 0 })
            return b.Slots;

        return Enumerable.Range(0, b.UnitsPerFloor)
            .Select(_ => new StructureComposerSlotDto
            {
                UnitType = b.DefaultUnitType,
                AreaM2 = b.DefaultAreaM2,
            })
            .ToList();
    }

    private static string ApplyPattern(string pattern, string? buildingCode, int floorNumber, int unitIndex, string? suffix)
    {
        var result = pattern.Trim();
        result = result.Replace("{bloque}", buildingCode ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        result = result.Replace("{piso}", floorNumber.ToString(CultureInfo.InvariantCulture), StringComparison.OrdinalIgnoreCase);

        result = PadPattern.Replace(result, match =>
        {
            var zeros = match.Value.Count(c => c == '0');
            return unitIndex.ToString(CultureInfo.InvariantCulture).PadLeft(Math.Max(zeros, 1), '0');
        });

        result = result.Replace("{u}", unitIndex.ToString(CultureInfo.InvariantCulture), StringComparison.OrdinalIgnoreCase);

        if (!string.IsNullOrWhiteSpace(suffix))
            result += suffix.Trim();

        if (string.IsNullOrWhiteSpace(result))
            throw new InvalidOperationException("Generated unit code is empty. Check the code pattern.");

        return result;
    }

    private static void ApplyOverrides(
        Plan plan,
        List<StructureComposerUnitOverrideDto> overrides,
        HashSet<string> usedCodes,
        List<string> dwellingCodes)
    {
        if (overrides.Count == 0)
            return;

        var map = overrides.ToDictionary(o => o.Code.Trim(), o => o, StringComparer.OrdinalIgnoreCase);

        foreach (var building in plan.Buildings)
        {
            foreach (var floor in building.Floors)
            {
                for (var i = floor.Units.Count - 1; i >= 0; i--)
                {
                    var unit = floor.Units[i];
                    if (!map.TryGetValue(unit.Code, out var ov))
                        continue;

                    if (ov.Remove)
                    {
                        floor.Units.RemoveAt(i);
                        usedCodes.Remove(unit.Code);
                        dwellingCodes.RemoveAll(c => c.Equals(unit.Code, StringComparison.OrdinalIgnoreCase));
                        continue;
                    }

                    if (ov.UnitType.HasValue)
                        unit.UnitType = ov.UnitType.Value;
                    if (ov.AreaM2.HasValue)
                        unit.AreaM2 = ov.AreaM2.Value;
                    if (ov.ParkingCount.HasValue)
                        unit.ParkingCount = ov.ParkingCount.Value;
                    if (ov.TerraceCount.HasValue)
                        unit.TerraceCount = ov.TerraceCount.Value;
                    if (ov.StorageCount.HasValue)
                        unit.StorageCount = ov.StorageCount.Value;
                }
            }
        }
    }

    private static void AddAncillary(
        Plan plan,
        HashSet<string> usedCodes,
        List<string> dwellingCodes,
        int count,
        string prefix,
        decimal areaM2,
        UnitType type,
        bool link)
    {
        var pad = Math.Max(2, count.ToString(CultureInfo.InvariantCulture).Length);
        for (var i = 1; i <= count; i++)
        {
            var code = $"{prefix.Trim()}{i.ToString(CultureInfo.InvariantCulture).PadLeft(pad, '0')}";
            if (!usedCodes.Add(code))
                throw new InvalidOperationException($"Duplicate ancillary code: {code}.");

            string? linked = null;
            if (link && i <= dwellingCodes.Count)
                linked = dwellingCodes[i - 1];

            plan.AncillaryUnits.Add(new UnitPlan
            {
                Code = code,
                UnitType = type,
                AreaM2 = areaM2,
                LinkedUnitCode = linked,
            });
        }
    }

    private static StructureComposerPreviewDto ToPreview(Plan plan)
    {
        var all = plan.AllUnits.ToList();
        return new StructureComposerPreviewDto
        {
            BuildingCount = plan.Buildings.Count,
            FloorCount = plan.Buildings.Sum(b => b.Floors.Count),
            UnitCount = all.Count,
            DwellingCount = all.Count(u => u.UnitType is UnitType.Apartment or UnitType.House),
            ParkingCount = all.Count(u => u.UnitType == UnitType.Parking),
            StorageCount = all.Count(u => u.UnitType == UnitType.Storage),
            TotalAreaM2 = all.Sum(u => u.AreaM2),
            Warnings = plan.Warnings,
            Buildings = plan.Buildings.Select(b => new StructureComposerPreviewBuildingDto
            {
                Name = b.Name,
                Code = b.Code,
                Floors = b.Floors.Select(f => new StructureComposerPreviewFloorDto
                {
                    Name = f.Name,
                    Number = f.Number,
                    Units = f.Units.Select(u => new StructureComposerPreviewUnitDto
                    {
                        Code = u.Code,
                        UnitType = u.UnitType,
                        AreaM2 = u.AreaM2,
                        BuildingCode = u.BuildingCode,
                        FloorName = u.FloorName,
                        ParkingCount = u.ParkingCount,
                        TerraceCount = u.TerraceCount,
                    }).ToList(),
                }).ToList(),
            }).ToList(),
            AncillaryUnits = plan.AncillaryUnits.Select(u => new StructureComposerPreviewUnitDto
            {
                Code = u.Code,
                UnitType = u.UnitType,
                AreaM2 = u.AreaM2,
                LinkedUnitCode = u.LinkedUnitCode,
                ParkingCount = u.ParkingCount,
                TerraceCount = u.TerraceCount,
            }).ToList(),
        };
    }
}
