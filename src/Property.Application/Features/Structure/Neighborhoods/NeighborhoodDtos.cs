using System.ComponentModel.DataAnnotations;

namespace Property.Application.Features.Structure.Neighborhoods;

public sealed class NeighborhoodDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? City { get; set; }
    public string Country { get; set; } = "Colombia";
    public bool IsActive { get; set; }
}

public sealed class CreateNeighborhoodDto
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(120)]
    public string? City { get; set; }

    [MaxLength(80)]
    public string Country { get; set; } = "Colombia";
}

public sealed class UpdateNeighborhoodDto
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(120)]
    public string? City { get; set; }

    [MaxLength(80)]
    public string Country { get; set; } = "Colombia";

    public bool IsActive { get; set; } = true;
}
