using Microsoft.Extensions.Options;
using Property.Application.Common.Interfaces;
using Property.Application.Common.Options;
using Property.Application.Features.Billing.PaymentIntents;

namespace Property.Infrastructure.Payments;

public sealed class WompiPspCheckoutService : IPspCheckoutService
{
    public const string ProviderCode = "Wompi";

    private readonly WompiOptions _options;

    public WompiPspCheckoutService(IOptions<WompiOptions> options) => _options = options.Value;

    public Task<PspCheckoutResult> CreateCheckoutAsync(
        PaymentIntentDto intent,
        CancellationToken cancellationToken = default)
    {
        var useMock = _options.UseMockCheckout
            || string.IsNullOrWhiteSpace(_options.PublicKey)
            || string.IsNullOrWhiteSpace(_options.PrivateKey);

        var providerRef = useMock
            ? $"wompi-mock-{intent.Id:N}"
            : $"wompi-pending-{intent.Id:N}";

        var baseUrl = (_options.PortalPublicBaseUrl ?? "http://localhost:5173").TrimEnd('/');
        var checkoutUrl = $"{baseUrl}/portal/cuenta/pagar/resultado?intentId={intent.Id:D}";

        // Live Wompi Widget/API wiring stays behind UseMockCheckout=false + real keys (P0 mock is default).
        return Task.FromResult(new PspCheckoutResult
        {
            Provider = ProviderCode,
            ProviderRef = providerRef,
            CheckoutUrl = checkoutUrl,
            IsMock = useMock,
        });
    }
}
