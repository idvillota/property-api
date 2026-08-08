using System.ComponentModel.DataAnnotations;

namespace Property.Application.Features.Organization.TenantSettings;

public sealed class TenantSettingsDto
{
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
}

public sealed class UpdateTenantSettingsDto
{
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

    [MaxLength(300)]
    public string AddressLine { get; set; } = string.Empty;

    [MaxLength(120)]
    public string City { get; set; } = string.Empty;

    [MaxLength(80)]
    public string Country { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? PostalCode { get; set; }

    [MaxLength(40)]
    public string? Phone { get; set; }
}
