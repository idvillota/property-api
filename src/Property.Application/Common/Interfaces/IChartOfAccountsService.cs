using Property.Application.Features.Accounting;

namespace Property.Application.Common.Interfaces;

public interface IChartOfAccountsService
{
    Task EnsureSeededAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LedgerAccountDto>> ListAsync(CancellationToken cancellationToken = default);
    Task<LedgerAccountDto?> UpdateAsync(Guid id, UpdateLedgerAccountDto dto, CancellationToken cancellationToken = default);
}
