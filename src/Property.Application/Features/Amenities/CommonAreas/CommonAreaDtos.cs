using System.ComponentModel.DataAnnotations;
using Property.Domain.Enums;

namespace Property.Application.Features.Amenities.CommonAreas;

public sealed class CommonAreaDto
{
    public Guid Id { get; set; }
    public Guid HorizontalPropertyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public CommonAreaType AreaType { get; set; }
    public int Capacity { get; set; }
    public bool RequiresDeposit { get; set; }
    public decimal? DepositAmount { get; set; }
    public string? RulesNotes { get; set; }
    public int? MinDurationMinutes { get; set; }
    public int? MaxDurationMinutes { get; set; }
    public bool IsActive { get; set; }
}

public sealed class CreateCommonAreaDto
{
    [Required]
    public Guid HorizontalPropertyId { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public CommonAreaType AreaType { get; set; }

    [Range(1, int.MaxValue)]
    public int Capacity { get; set; } = 1;

    public bool RequiresDeposit { get; set; }

    [Range(0, double.MaxValue)]
    public decimal? DepositAmount { get; set; }

    [MaxLength(4000)]
    public string? RulesNotes { get; set; }

    [Range(1, int.MaxValue)]
    public int? MinDurationMinutes { get; set; }

    [Range(1, int.MaxValue)]
    public int? MaxDurationMinutes { get; set; }
}

public sealed class UpdateCommonAreaDto
{
    [Required]
    public Guid HorizontalPropertyId { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public CommonAreaType AreaType { get; set; }

    [Range(1, int.MaxValue)]
    public int Capacity { get; set; } = 1;

    public bool RequiresDeposit { get; set; }

    [Range(0, double.MaxValue)]
    public decimal? DepositAmount { get; set; }

    [MaxLength(4000)]
    public string? RulesNotes { get; set; }

    [Range(1, int.MaxValue)]
    public int? MinDurationMinutes { get; set; }

    [Range(1, int.MaxValue)]
    public int? MaxDurationMinutes { get; set; }

    public bool IsActive { get; set; } = true;
}
