using Property.Domain.Enums;

namespace Property.Application.Features.Structure.Units;

public sealed class UnitHouseholdPersonDto
{
    public Guid PersonId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string DocumentLabel { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public OccupancyRole Role { get; set; }
    public bool IsPrimary { get; set; }
    public bool ReceivesBilling { get; set; }
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

    /// <summary>Owners who are not already listed as habitants.</summary>
    public IReadOnlyList<UnitHouseholdPersonDto> Owners { get; set; } = Array.Empty<UnitHouseholdPersonDto>();

    /// <summary>Tenants, residents, and authorized occupants.</summary>
    public IReadOnlyList<UnitHouseholdPersonDto> Habitants { get; set; } = Array.Empty<UnitHouseholdPersonDto>();
}
