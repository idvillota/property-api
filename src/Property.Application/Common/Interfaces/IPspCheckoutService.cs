using Property.Application.Features.Billing.PaymentIntents;

namespace Property.Application.Common.Interfaces;

public interface IPspCheckoutService
{
    Task<PspCheckoutResult> CreateCheckoutAsync(
        PaymentIntentDto intent,
        CancellationToken cancellationToken = default);
}
