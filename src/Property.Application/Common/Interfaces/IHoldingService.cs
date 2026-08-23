using Property.Application.Features.Holding;

namespace Property.Application.Common.Interfaces;

public interface IHoldingService
{
    Task<HoldingSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default);
}
