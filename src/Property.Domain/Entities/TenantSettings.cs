using Property.Domain.Common;

namespace Property.Domain.Entities;

public class TenantSettings : ITenantScoped
{
    public Guid TenantId { get; set; }

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

    /// <summary>JSON layout for tenant dashboard panels (react-grid-layout).</summary>
    public string? DashboardLayoutJson { get; set; }

    public Tenant Tenant { get; set; } = null!;
}
