using System.ComponentModel.DataAnnotations;
using Property.Domain.Enums;

namespace Property.Application.Features.Structure.Vehicles;

public sealed class VehicleDto
{
    public Guid Id { get; set; }
    public Guid HorizontalPropertyId { get; set; }
    public Guid? UnitId { get; set; }
    public string? UnitCode { get; set; }
    public Guid VehicleBrandId { get; set; }
    public VehicleType VehicleType { get; set; }
    public string Brand { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public string Plate { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public sealed class CreateVehicleDto
{
    [Required]
    public Guid HorizontalPropertyId { get; set; }

    public Guid? UnitId { get; set; }

    [Required]
    public Guid VehicleBrandId { get; set; }

    [Required]
    public VehicleType VehicleType { get; set; }

    [Required]
    [MaxLength(80)]
    public string Model { get; set; } = string.Empty;

    [Required]
    [MaxLength(40)]
    public string Color { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string Plate { get; set; } = string.Empty;
}

public sealed class UpdateVehicleDto
{
    [Required]
    public Guid HorizontalPropertyId { get; set; }

    public Guid? UnitId { get; set; }

    [Required]
    public Guid VehicleBrandId { get; set; }

    [Required]
    public VehicleType VehicleType { get; set; }

    [Required]
    [MaxLength(80)]
    public string Model { get; set; } = string.Empty;

    [Required]
    [MaxLength(40)]
    public string Color { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string Plate { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}

public sealed class AssignVehicleUnitDto
{
    [Required]
    public Guid UnitId { get; set; }
}
