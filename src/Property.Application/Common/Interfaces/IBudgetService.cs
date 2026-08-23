using Property.Application.Features.Accounting;

namespace Property.Application.Common.Interfaces;

public interface IBudgetService
{
    Task<BudgetDto> CreateAsync(CreateBudgetDto dto, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BudgetDto>> ListAsync(Guid horizontalPropertyId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BudgetExecutionRowDto>> GetExecutionAsync(
        Guid horizontalPropertyId, int year, CancellationToken cancellationToken = default);
}
