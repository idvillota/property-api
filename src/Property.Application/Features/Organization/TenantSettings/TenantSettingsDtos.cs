using System.ComponentModel.DataAnnotations;
using Property.Domain.Enums;

namespace Property.Application.Features.Organization.TenantSettings;

/// <summary>
/// Tenant configuration DTO. When a HorizontalProperty exists, it is the source of truth.
/// </summary>
public sealed class TenantSettingsDto
{
    public Guid? PropertyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string TradeName { get; set; } = string.Empty;
    public string LegalName { get; set; } = string.Empty;
    public string TaxRegime { get; set; } = string.Empty;
    public string TaxId { get; set; } = string.Empty;
    public string? LegalRepresentative { get; set; }
    public string AddressLine { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string? PostalCode { get; set; }
    public string? Phone { get; set; }
    public Guid? NeighborhoodId { get; set; }
    public PropertyUseType? PropertyUseType { get; set; }
    public string? MatriculaInmobiliaria { get; set; }
    public string? RegulationNotes { get; set; }
    public string? CoefficientBasisNotes { get; set; }
    public bool IsActive { get; set; } = true;
    public bool HasProperty { get; set; }

    /// <summary>Tenant commercial modules (ARCH-002). Independent of user FeatureCodes.</summary>
    public TenantModuleFlagsDto Modules { get; set; } = new();
}

public sealed class UpdateTenantSettingsDto
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(200)]
    public string TradeName { get; set; } = string.Empty;

    [MaxLength(200)]
    public string LegalName { get; set; } = string.Empty;

    [MaxLength(80)]
    public string TaxRegime { get; set; } = string.Empty;

    [MaxLength(40)]
    public string TaxId { get; set; } = string.Empty;

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

    public PropertyUseType? PropertyUseType { get; set; }

    [MaxLength(80)]
    public string? MatriculaInmobiliaria { get; set; }

    [MaxLength(4000)]
    public string? RegulationNotes { get; set; }

    [MaxLength(4000)]
    public string? CoefficientBasisNotes { get; set; }

    public bool IsActive { get; set; } = true;

    /// <summary>When null, existing module flags are left unchanged.</summary>
    public TenantModuleFlagsDto? Modules { get; set; }
}
