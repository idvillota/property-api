using System.ComponentModel.DataAnnotations;

namespace Property.Application.Features.Billing.BreB;

public sealed class BreBPaymentProfileDto
{
    public Guid HorizontalPropertyId { get; set; }
    public string Key { get; set; } = string.Empty;
    public int KeyType { get; set; }
    public string MerchantName { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public bool IsReady { get; set; }
}

public sealed class UpdateBreBPaymentProfileDto
{
    [MaxLength(40)]
    public string Key { get; set; } = string.Empty;

    [Range(0, 5)]
    public int KeyType { get; set; }

    [MaxLength(80)]
    public string MerchantName { get; set; } = string.Empty;

    [MaxLength(40)]
    public string City { get; set; } = string.Empty;

    [MaxLength(10)]
    public string PostalCode { get; set; } = string.Empty;
}
