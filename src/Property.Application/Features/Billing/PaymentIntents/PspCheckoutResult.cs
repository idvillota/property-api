namespace Property.Application.Features.Billing.PaymentIntents;

public sealed class PspCheckoutResult
{
    public string Provider { get; set; } = "Wompi";
    public string ProviderRef { get; set; } = string.Empty;
    public string CheckoutUrl { get; set; } = string.Empty;
    public bool IsMock { get; set; }
}
