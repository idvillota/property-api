using System.ComponentModel.DataAnnotations;
using Property.Application.Features.Billing.PaymentIntents;

namespace Property.Application.Features.Portal;

public sealed class StartPortalPaymentDto
{
    [Required]
    public Guid UnitId { get; set; }

    /// <summary>Optional idempotency key; defaults to a new key per request when omitted.</summary>
    [MaxLength(120)]
    public string? IdempotencyKey { get; set; }
}

public sealed class PortalCheckoutDto
{
    public PaymentIntentDto Intent { get; set; } = null!;
    public string CheckoutUrl { get; set; } = string.Empty;
    public bool IsMockCheckout { get; set; }
    public string Provider { get; set; } = "Wompi";
}
