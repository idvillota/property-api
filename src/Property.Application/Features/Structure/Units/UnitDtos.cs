using System.ComponentModel.DataAnnotations;
using Property.Domain.Enums;

namespace Property.Application.Features.Structure.Units;

public sealed class UnitDto
{
    public Guid Id { get; set; }
    public Guid HorizontalPropertyId { get; set; }
    public Guid? BuildingId { get; set; }
    public Guid? FloorId { get; set; }
    public string Code { get; set; } = string.Empty;
    public UnitType UnitType { get; set; }
    public decimal PrivateBuiltAreaM2 { get; set; }
    public decimal? PrivateOpenAreaM2 { get; set; }
    public decimal Coefficient { get; set; }
    public string? ContributionModuleCode { get; set; }
    public Guid? LinkedUnitId { get; set; }
    public int ParkingCount { get; set; }
    public int TerraceCount { get; set; }
    public int StorageCount { get; set; }
    public bool IsDoubleParking { get; set; }
    public bool IsBillable { get; set; }
    public bool IsPaymentUpToDate { get; set; }
    public bool IsActive { get; set; }
}

public sealed class CreateUnitDto
{
    [Required]
    public Guid HorizontalPropertyId { get; set; }

    public Guid? BuildingId { get; set; }

    public Guid? FloorId { get; set; }

    [Required]
    [MaxLength(80)]
    public string Code { get; set; } = string.Empty;

    [Required]
    public UnitType UnitType { get; set; }

    [Range(0, double.MaxValue)]
    public decimal PrivateBuiltAreaM2 { get; set; }

    [Range(0, double.MaxValue)]
    public decimal? PrivateOpenAreaM2 { get; set; }

    /// <summary>Ignored; coefficient is calculated from private built area.</summary>
    public decimal? Coefficient { get; set; }

    [MaxLength(40)]
    public string? ContributionModuleCode { get; set; }

    public Guid? LinkedUnitId { get; set; }

    [Range(0, 50)]
    public int ParkingCount { get; set; }

    [Range(0, 20)]
    public int TerraceCount { get; set; }

    [Range(0, 50)]
    public int StorageCount { get; set; }

    public bool IsDoubleParking { get; set; }

    public bool IsBillable { get; set; } = true;

    public bool IsPaymentUpToDate { get; set; } = true;
}

public sealed class UpdateUnitDto
{
    [Required]
    public Guid HorizontalPropertyId { get; set; }

    public Guid? BuildingId { get; set; }

    public Guid? FloorId { get; set; }

    [Required]
    [MaxLength(80)]
    public string Code { get; set; } = string.Empty;

    [Required]
    public UnitType UnitType { get; set; }

    [Range(0, double.MaxValue)]
    public decimal PrivateBuiltAreaM2 { get; set; }

    [Range(0, double.MaxValue)]
    public decimal? PrivateOpenAreaM2 { get; set; }

    /// <summary>Ignored; coefficient is calculated from private built area.</summary>
    public decimal? Coefficient { get; set; }

    [MaxLength(40)]
    public string? ContributionModuleCode { get; set; }

    public Guid? LinkedUnitId { get; set; }

    [Range(0, 50)]
    public int ParkingCount { get; set; }

    [Range(0, 20)]
    public int TerraceCount { get; set; }

    [Range(0, 50)]
    public int StorageCount { get; set; }

    public bool IsDoubleParking { get; set; }

    public bool IsBillable { get; set; } = true;

    public bool IsPaymentUpToDate { get; set; } = true;

    public bool IsActive { get; set; } = true;
}

public sealed class UnitCoefficientSummaryDto
{
    public decimal TotalCoefficient { get; set; }
    public int UnitCount { get; set; }
    public decimal RemainingToHundred { get; set; }
}
