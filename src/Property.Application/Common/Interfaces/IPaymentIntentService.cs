using Property.Application.Common.Models;
using Property.Application.Features.Billing.PaymentIntents;

namespace Property.Application.Common.Interfaces;

public interface IPaymentIntentService
{
    Task<PagedResult<PaymentIntentDto>> ListAsync(ListQuery query, CancellationToken cancellationToken = default);

    Task<PaymentIntentDto> CreateOrGetAsync(
        CreatePaymentIntentDto dto,
        CancellationToken cancellationToken = default);

    Task<PaymentIntentDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<PaymentIntentDto> AttachCheckoutAsync(
        Guid id,
        string provider,
        string providerRef,
        CancellationToken cancellationToken = default);

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
