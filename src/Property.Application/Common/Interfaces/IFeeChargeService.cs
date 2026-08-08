using Property.Application.Common.Models;
using Property.Application.Features.Billing.FeeCharges;

namespace Property.Application.Common.Interfaces;

public interface IFeeChargeService
{
    Task<PagedResult<FeeChargeDto>> ListAsync(ListQuery query, CancellationToken cancellationToken = default);
    Task<FeeChargeDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<FeeChargeDto> CreateAsync(CreateFeeChargeDto dto, CancellationToken cancellationToken = default);
    Task<FeeChargeDto?> UpdateAsync(Guid id, UpdateFeeChargeDto dto, CancellationToken cancellationToken = default);
    Task<bool> SoftDeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<GenerateChargesResultDto> GenerateOrdinaryAsync(GenerateOrdinaryChargesDto dto, CancellationToken cancellationToken = default);
    Task<GenerateChargesResultDto> GenerateInterestAsync(GenerateInterestChargesDto dto, CancellationToken cancellationToken = default);
    Task<ArrearsSummaryDto> GetArrearsSummaryAsync(Guid horizontalPropertyId, CancellationToken cancellationToken = default);
}
