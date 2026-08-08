using Property.Application.Common.Models;
using Property.Application.Features.Billing.Payments;

namespace Property.Application.Common.Interfaces;

public interface IPaymentService
{
    Task<PagedResult<PaymentDto>> ListAsync(ListQuery query, CancellationToken cancellationToken = default);
    Task<PaymentDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PaymentDto> CreateAsync(CreatePaymentDto dto, CancellationToken cancellationToken = default);
    Task<bool> SoftDeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
