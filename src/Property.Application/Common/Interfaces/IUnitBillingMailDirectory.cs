using Property.Application.Features.Billing.BreB;

namespace Property.Application.Common.Interfaces;

public interface IUnitBillingMailDirectory
{
    Task<IReadOnlyDictionary<Guid, IReadOnlyList<BillingMailContact>>> ListByUnitAsync(
        IReadOnlyCollection<Guid> unitIds,
        CancellationToken cancellationToken = default);
}
