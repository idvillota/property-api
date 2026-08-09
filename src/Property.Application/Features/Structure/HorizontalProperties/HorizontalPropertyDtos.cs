using System.ComponentModel.DataAnnotations;
using Property.Domain.Enums;

namespace Property.Application.Features.Structure.HorizontalProperties;

public sealed class HorizontalPropertyDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? TradeName { get; set; }
    public string? LegalName { get; set; }
    public string? Nit { get; set; }
    public string? TaxRegime { get; set; }
    public string? LegalRepresentative { get; set; }
    public string AddressLine { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Country { get; set; } = "Colombia";
    public string? PostalCode { get; set; }
    public string? Phone { get; set; }
    public Guid? NeighborhoodId { get; set; }
    public PropertyUseType PropertyUseType { get; set; }
    public string? MatriculaInmobiliaria { get; set; }
    public string? RegulationNotes { get; set; }
    public string? CoefficientBasisNotes { get; set; }
    public bool IsActive { get; set; }
}

public sealed class CreateHorizontalPropertyDto
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? TradeName { get; set; }

    [MaxLength(200)]
    public string? LegalName { get; set; }

    [MaxLength(40)]
    public string? Nit { get; set; }

    [MaxLength(80)]
    public string? TaxRegime { get; set; }

    [MaxLength(200)]
    public string? LegalRepresentative { get; set; }

    [Required]
    [MaxLength(300)]
    public string AddressLine { get; set; } = string.Empty;

    [Required]
    [MaxLength(120)]
    public string City { get; set; } = string.Empty;

    [MaxLength(80)]
    public string Country { get; set; } = "Colombia";

    [MaxLength(20)]
    public string? PostalCode { get; set; }

    [MaxLength(40)]
    public string? Phone { get; set; }

    public Guid? NeighborhoodId { get; set; }

    [Required]
    public PropertyUseType PropertyUseType { get; set; }

    [MaxLength(80)]
    public string? MatriculaInmobiliaria { get; set; }

    [MaxLength(4000)]
    public string? RegulationNotes { get; set; }

    [MaxLength(4000)]
    public string? CoefficientBasisNotes { get; set; }
}

public sealed class UpdateHorizontalPropertyDto
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? TradeName { get; set; }

    [MaxLength(200)]
    public string? LegalName { get; set; }

    [MaxLength(40)]
    public string? Nit { get; set; }

    [MaxLength(80)]
    public string? TaxRegime { get; set; }

    [MaxLength(200)]
    public string? LegalRepresentative { get; set; }

    [Required]
    [MaxLength(300)]
    public string AddressLine { get; set; } = string.Empty;

    [Required]
    [MaxLength(120)]
    public string City { get; set; } = string.Empty;

    [MaxLength(80)]
    public string Country { get; set; } = "Colombia";

    [MaxLength(20)]
    public string? PostalCode { get; set; }

    [MaxLength(40)]
    public string? Phone { get; set; }

    public Guid? NeighborhoodId { get; set; }

    [Required]
    public PropertyUseType PropertyUseType { get; set; }

    [MaxLength(80)]
    public string? MatriculaInmobiliaria { get; set; }

    [MaxLength(4000)]
    public string? RegulationNotes { get; set; }

    [MaxLength(4000)]
    public string? CoefficientBasisNotes { get; set; }

    public bool IsActive { get; set; } = true;
}
