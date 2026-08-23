using Property.Application.Features.Accounting;

namespace Property.Application.Common.Interfaces;

public interface IPettyCashService
{
    Task<PettyCashFundDto> CreateFundAsync(CreatePettyCashFundDto dto, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PettyCashFundDto>> ListFundsAsync(Guid horizontalPropertyId, CancellationToken cancellationToken = default);
    Task<PettyCashExpenseDto> CreateExpenseAsync(CreatePettyCashExpenseDto dto, CancellationToken cancellationToken = default);
}
