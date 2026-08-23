using Property.Application.Features.Accounting;

namespace Property.Application.Common.Interfaces;

public interface IAccountingPeriodService
{
    Task<AccountingPeriodDto> CloseAsync(ClosePeriodDto dto, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AccountingPeriodDto>> ListAsync(Guid horizontalPropertyId, CancellationToken cancellationToken = default);
    Task<FinancialStatementsDto> GetStatementsAsync(
        Guid horizontalPropertyId, int year, int month, CancellationToken cancellationToken = default);
}
