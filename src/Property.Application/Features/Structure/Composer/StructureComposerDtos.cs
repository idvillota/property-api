using System.ComponentModel.DataAnnotations;
using Property.Application.Features.Structure.Units;
using Property.Domain.Enums;

namespace Property.Application.Features.Structure.Composer;

public sealed class StructureComposerStatusDto
{
    public Guid HorizontalPropertyId { get; set; }
    public bool HasStructure { get; set; }
    public int BuildingCount { get; set; }
    public int FloorCount { get; set; }
    public int UnitCount { get; set; }
}

public sealed class StructureComposerRequestDto
{
    [Required]
    public Guid HorizontalPropertyId { get; set; }

    [Required]
    [MinLength(1)]
    public List<StructureComposerBuildingDto> Buildings { get; set; } = [];

    public StructureComposerParkingDto? Parking { get; set; }

    public StructureComposerStorageDto? Storage { get; set; }

    /// <summary>
    /// Optional unit edits keyed by generated preview code (e.g. change area/type before commit).
    /// </summary>
    public List<StructureComposerUnitOverrideDto> UnitOverrides { get; set; } = [];
}

public sealed class StructureComposerBuildingDto
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(40)]
    public string? Code { get; set; }

    public int FloorFrom { get; set; } = 1;

    public int FloorTo { get; set; } = 1;

    [Range(1, 40)]
    public int UnitsPerFloor { get; set; } = 4;

    public bool IncludeBasement { get; set; }

    public bool IncludeRooftop { get; set; }

    [MaxLength(80)]
    public string CodePattern { get; set; } = "{piso}{u:00}";

    public UnitType DefaultUnitType { get; set; } = UnitType.Apartment;

    [Range(0, 100000)]
    public decimal DefaultAreaM2 { get; set; } = 72m;

    /// <summary>Optional per-position matrix; length must equal UnitsPerFloor when provided.</summary>
    public List<StructureComposerSlotDto>? Slots { get; set; }
}

public sealed class StructureComposerSlotDto
{
    public UnitType UnitType { get; set; } = UnitType.Apartment;

    [Range(0, 100000)]
    public decimal AreaM2 { get; set; } = 72m;

    [MaxLength(20)]
    public string? CodeSuffix { get; set; }
}

public sealed class StructureComposerParkingDto
{
    [Range(0, 2000)]
    public int Count { get; set; }

    [MaxLength(20)]
    public string Prefix { get; set; } = "P-";

    [Range(0, 100000)]
    public decimal AreaM2 { get; set; } = 12.5m;

    public bool LinkToApartmentsInOrder { get; set; } = true;
}

public sealed class StructureComposerStorageDto
{
    [Range(0, 2000)]
    public int Count { get; set; }

    [MaxLength(20)]
    public string Prefix { get; set; } = "D-";

    [Range(0, 100000)]
    public decimal AreaM2 { get; set; } = 4m;
}

public sealed class StructureComposerUnitOverrideDto
{
    [Required]
    [MaxLength(80)]
    public string Code { get; set; } = string.Empty;

    public UnitType? UnitType { get; set; }

    public decimal? AreaM2 { get; set; }

    [Range(0, 50)]
    public int? ParkingCount { get; set; }

    [Range(0, 20)]
    public int? TerraceCount { get; set; }

    [Range(0, 50)]
    public int? StorageCount { get; set; }

    public bool Remove { get; set; }
}

public sealed class StructureWorkspaceDto
{
    public Guid HorizontalPropertyId { get; set; }
    public bool HasStructure { get; set; }
    public int BuildingCount { get; set; }
    public int FloorCount { get; set; }
    public int UnitCount { get; set; }
    public IReadOnlyList<StructureWorkspaceBuildingDto> Buildings { get; set; } =
        Array.Empty<StructureWorkspaceBuildingDto>();
}

public sealed class StructureWorkspaceBuildingDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public int SortOrder { get; set; }
    public IReadOnlyList<StructureWorkspaceFloorDto> Floors { get; set; } =
        Array.Empty<StructureWorkspaceFloorDto>();
}

public sealed class StructureWorkspaceFloorDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int? Number { get; set; }
    public int SortOrder { get; set; }
    public IReadOnlyList<UnitDto> Units { get; set; } = Array.Empty<UnitDto>();
}

public sealed class StructureComposerPreviewDto
{
    public int BuildingCount { get; set; }
    public int FloorCount { get; set; }
    public int UnitCount { get; set; }
    public int DwellingCount { get; set; }
    public int ParkingCount { get; set; }
    public int StorageCount { get; set; }
    public decimal TotalAreaM2 { get; set; }
    public IReadOnlyList<string> Warnings { get; set; } = Array.Empty<string>();
    public IReadOnlyList<StructureComposerPreviewBuildingDto> Buildings { get; set; } =
        Array.Empty<StructureComposerPreviewBuildingDto>();
    public IReadOnlyList<StructureComposerPreviewUnitDto> AncillaryUnits { get; set; } =
        Array.Empty<StructureComposerPreviewUnitDto>();
}

public sealed class StructureComposerPreviewBuildingDto
{
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public IReadOnlyList<StructureComposerPreviewFloorDto> Floors { get; set; } =
        Array.Empty<StructureComposerPreviewFloorDto>();
}

public sealed class StructureComposerPreviewFloorDto
{
    public string Name { get; set; } = string.Empty;
    public int? Number { get; set; }
    public IReadOnlyList<StructureComposerPreviewUnitDto> Units { get; set; } =
        Array.Empty<StructureComposerPreviewUnitDto>();
}

public sealed class StructureComposerPreviewUnitDto
{
    public string Code { get; set; } = string.Empty;
    public UnitType UnitType { get; set; }
    public decimal AreaM2 { get; set; }
    public string? BuildingCode { get; set; }
    public string? FloorName { get; set; }
    public string? LinkedUnitCode { get; set; }
    public int ParkingCount { get; set; }
    public int TerraceCount { get; set; }
}

public sealed class StructureComposerCommitResultDto
{
    public int BuildingsCreated { get; set; }
    public int FloorsCreated { get; set; }
    public int UnitsCreated { get; set; }
    public decimal TotalCoefficient { get; set; }
}
