using Property.Domain.Common;
using Property.Domain.Enums;

namespace Property.Domain.Entities;

/// <summary>PSP-neutral payment intent (ARCH-005). Paid transition posts Payment once.</summary>
public class PaymentIntent : EntityBase, ITenantScoped
{
    public Guid TenantId { get; set; }

    /// <summary>Unique per tenant — repeated create returns the same intent.</summary>
    public string IdempotencyKey { get; set; } = string.Empty;

    /// <summary>External PSP reference; null until DEC-PSP / provider callback.</summary>
    public string? ProviderRef { get; set; }

    public PaymentIntentStatus Status { get; set; } = PaymentIntentStatus.Pending;

    public Guid HorizontalPropertyId { get; set; }
    public Guid UnitId { get; set; }
    public Guid? PersonId { get; set; }

    public decimal Amount { get; set; }

    /// <summary>JSON array of { feeChargeId, amount } used when posting Payment.</summary>
    public string AllocationsJson { get; set; } = "[]";

    /// <summary>Set once when Status becomes Paid.</summary>
    public Guid? PaymentId { get; set; }

    public string? FailureReason { get; set; }

    public DateTime? PaidAtUtc { get; set; }

    public HorizontalProperty? HorizontalProperty { get; set; }
    public Unit? Unit { get; set; }
    public Person? Person { get; set; }
    public Payment? Payment { get; set; }
}
