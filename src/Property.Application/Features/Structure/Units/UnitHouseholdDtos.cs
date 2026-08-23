using Property.Domain.Enums;

namespace Property.Application.Features.Structure.Units;

public sealed class UnitHouseholdPersonDto
{
    public Guid OccupancyId { get; set; }
    public Guid PersonId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string DocumentLabel { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public OccupancyRole Role { get; set; }
    public bool IsPrimary { get; set; }
    public bool ReceivesBilling { get; set; }
    public bool IsCommunicationResponsible { get; set; }
}

public sealed class UnitAmenityLinkDto
{
    public Guid UnitId { get; set; }
    public string Code { get; set; } = string.Empty;
}

public sealed class UnitHouseholdDto
{
    public Guid UnitId { get; set; }
    public string UnitCode { get; set; } = string.Empty;
    public UnitType UnitType { get; set; }
    public Guid? BuildingId { get; set; }
    public string? BuildingName { get; set; }
    public Guid? FloorId { get; set; }
    public string? FloorName { get; set; }
    public decimal PrivateBuiltAreaM2 { get; set; }
    public decimal Coefficient { get; set; }
    public int ParkingCount { get; set; }
    public int TerraceCount { get; set; }
    public int StorageCount { get; set; }

    /// <summary>Owners who are not already listed as habitants.</summary>
    public IReadOnlyList<UnitHouseholdPersonDto> Owners { get; set; } = Array.Empty<UnitHouseholdPersonDto>();

    /// <summary>Tenants, residents, and authorized occupants.</summary>
    public IReadOnlyList<UnitHouseholdPersonDto> Habitants { get; set; } = Array.Empty<UnitHouseholdPersonDto>();

    /// <summary>Parking units linked to this dwelling via LinkedUnitId.</summary>
    public IReadOnlyList<UnitAmenityLinkDto> RelatedParkings { get; set; } = Array.Empty<UnitAmenityLinkDto>();

    /// <summary>Terrace units linked to this dwelling via LinkedUnitId.</summary>
    public IReadOnlyList<UnitAmenityLinkDto> RelatedTerraces { get; set; } = Array.Empty<UnitAmenityLinkDto>();

    /// <summary>Storage units linked to this dwelling via LinkedUnitId.</summary>
    public IReadOnlyList<UnitAmenityLinkDto> RelatedStorages { get; set; } = Array.Empty<UnitAmenityLinkDto>();

    /// <summary>Open FeeCharge balance (Pending|Partial) — DIFF-008.</summary>
    public decimal OpenBalance { get; set; }

    /// <summary>Open balance past due date.</summary>
    public decimal OverdueBalance { get; set; }
}
