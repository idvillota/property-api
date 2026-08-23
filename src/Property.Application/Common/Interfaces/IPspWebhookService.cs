using Property.Application.Features.Billing.PaymentIntents;

namespace Property.Application.Common.Interfaces;

public interface IPspWebhookService
{
    /// <summary>
    /// Validates Wompi event signature and applies Approved/Declined to the matching PaymentIntent.
    /// </summary>
    Task ProcessWompiEventAsync(
        string rawJsonBody,
        string? checksumHeader,
        CancellationToken cancellationToken = default);
}
