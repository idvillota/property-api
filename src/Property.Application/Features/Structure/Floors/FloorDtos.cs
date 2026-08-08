using System.ComponentModel.DataAnnotations;

namespace Property.Application.Features.Structure.Floors;

public sealed class FloorDto
{
    public Guid Id { get; set; }
    public Guid BuildingId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int? Number { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
}

public sealed class CreateFloorDto
{
    [Required]
    public Guid BuildingId { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    public int? Number { get; set; }

    public int SortOrder { get; set; }
}

public sealed class UpdateFloorDto
{
    [Required]
    public Guid BuildingId { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    public int? Number { get; set; }

    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;
}
