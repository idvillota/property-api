using System.ComponentModel.DataAnnotations;

namespace Property.Application.Features.Structure.VehicleBrands;

public sealed class VehicleBrandDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public sealed class CreateVehicleBrandDto
{
    [Required]
    [MaxLength(80)]
    public string Name { get; set; } = string.Empty;
}

public sealed class UpdateVehicleBrandDto
{
    [Required]
    [MaxLength(80)]
    public string Name { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}
