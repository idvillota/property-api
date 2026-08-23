using Property.Application.Features.Accounting;
using Property.Domain.Enums;

namespace Property.Application.Common.Interfaces;

public interface IJournalService
{
    Task EnsurePeriodOpenAsync(Guid horizontalPropertyId, DateTime entryDate, CancellationToken cancellationToken = default);
    Task<JournalEntryDto> PostBalancedAsync(
        Guid horizontalPropertyId,
        DateTime entryDate,
        JournalSourceType sourceType,
        Guid? sourceId,
        string description,
        IReadOnlyList<(string AccountCode, decimal Debit, decimal Credit, string? Memo)> lines,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<JournalEntryDto>> ListAsync(
        Guid horizontalPropertyId,
        JournalSourceType? sourceType = null,
        CancellationToken cancellationToken = default);
}
