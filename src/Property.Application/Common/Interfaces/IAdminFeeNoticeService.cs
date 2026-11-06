using Property.Application.Features.Billing.BreB;

namespace Property.Application.Common.Interfaces;

public interface IAdminFeeNoticeService
{
    Task<AdminFeeNoticePlan> ScheduleAsync(
        Guid horizontalPropertyId,
        Guid billingPeriodId,
        IReadOnlyList<AdminFeeNoticeCharge> charges,
        CancellationToken cancellationToken = default);

    Task DispatchAsync(AdminFeeNoticePayload payload, CancellationToken cancellationToken = default);
}
