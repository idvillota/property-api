namespace Property.Application.Features.Accounting;

public sealed class ChargePostedPayload
{
    public Guid HorizontalPropertyId { get; set; }
    public Guid BillingPeriodId { get; set; }
    public List<Guid> FeeChargeIds { get; set; } = [];
}

public sealed class PaymentPostedPayload
{
    public Guid PaymentId { get; set; }
    public Guid HorizontalPropertyId { get; set; }
}
