using Property.Application.Features.Billing.PaymentIntents;

namespace Property.Application.Common.Interfaces;

public interface IPaymentIntentService
{
    Task<PaymentIntentDto> CreateOrGetAsync(
        CreatePaymentIntentDto dto,
        CancellationToken cancellationToken = default);

    Task<PaymentIntentDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<PaymentIntentDto> MarkPaidAsync(
        Guid id,
        MarkPaymentIntentPaidDto dto,
        CancellationToken cancellationToken = default);

    Task<PaymentIntentDto> MarkFailedAsync(
        Guid id,
        MarkPaymentIntentFailedDto dto,
        CancellationToken cancellationToken = default);

    Task<PaymentIntentDto> MarkRequiresActionAsync(Guid id, CancellationToken cancellationToken = default);

    Task<PaymentIntentDto> CancelAsync(Guid id, CancellationToken cancellationToken = default);
}
