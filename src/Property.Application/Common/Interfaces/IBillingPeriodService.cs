using Property.Application.Common.Models;
using Property.Application.Features.Billing.BillingPeriods;

namespace Property.Application.Common.Interfaces;

public interface IBillingPeriodService
{
    Task<PagedResult<BillingPeriodDto>> ListAsync(ListQuery query, CancellationToken cancellationToken = default);
    Task<BillingPeriodDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<BillingPeriodDto> CreateAsync(CreateBillingPeriodDto dto, CancellationToken cancellationToken = default);
    Task<BillingPeriodDto?> UpdateAsync(Guid id, UpdateBillingPeriodDto dto, CancellationToken cancellationToken = default);
    Task<bool> SoftCloseAsync(Guid id, CancellationToken cancellationToken = default);
}
