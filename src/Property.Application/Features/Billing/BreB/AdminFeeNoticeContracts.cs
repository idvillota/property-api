namespace Property.Application.Features.Billing.BreB;

public sealed class AdminFeeNoticeCharge
{
    public Guid FeeChargeId { get; init; }
    public Guid UnitId { get; init; }
    public decimal Amount { get; init; }
}

public sealed class AdminFeeNoticePlan
{
    public bool PaymentQrConfigured { get; init; }
    public string? PaymentQrSkipReason { get; init; }
    public int PaymentNoticeEmailsQueued { get; init; }
    public int PaymentNoticeUnitsWithoutEmail { get; init; }
}

public sealed class AdminFeeNoticePayload
{
    public Guid HorizontalPropertyId { get; set; }
    public Guid BillingPeriodId { get; set; }
    public List<Guid> FeeChargeIds { get; set; } = [];
}

public sealed record BillingMailContact(Guid PersonId, string Email, string DisplayName);
