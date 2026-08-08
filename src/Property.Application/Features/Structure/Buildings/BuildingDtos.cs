using System.ComponentModel.DataAnnotations;

namespace Property.Application.Features.Structure.Buildings;

public sealed class BuildingDto
{
    public Guid Id { get; set; }
    public Guid HorizontalPropertyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
}

public sealed class CreateBuildingDto
{
    [Required]
    public Guid HorizontalPropertyId { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(40)]
    public string? Code { get; set; }

    public int SortOrder { get; set; }
}

public sealed class UpdateBuildingDto
{
    [Required]
    public Guid HorizontalPropertyId { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(40)]
    public string? Code { get; set; }

    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;
}
