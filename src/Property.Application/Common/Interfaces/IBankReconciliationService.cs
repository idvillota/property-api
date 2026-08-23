using Property.Application.Features.Accounting;

namespace Property.Application.Common.Interfaces;

public interface IBankReconciliationService
{
    Task<BankStatementDto> CreateStatementAsync(CreateBankStatementDto dto, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BankStatementDto>> ListStatementsAsync(Guid horizontalPropertyId, CancellationToken cancellationToken = default);
    Task<BankStatementLineDto> MatchAsync(MatchBankLineDto dto, CancellationToken cancellationToken = default);
}
