using Property.Domain.Common;

namespace Property.Domain.Entities;

/// <summary>Bre-B key used to build the administration-fee QR for one property.</summary>
public class BreBPaymentProfile : ITenantScoped
{
    public Guid HorizontalPropertyId { get; set; }
    public Guid TenantId { get; set; }
    public string Key { get; set; } = string.Empty;
    public int KeyType { get; set; }
    public string MerchantName { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;

    public HorizontalProperty? HorizontalProperty { get; set; }
}
