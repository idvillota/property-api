using Property.Application.Features.Billing.Dunning;

namespace Property.Application.Common.Interfaces;

public interface IDunningAdminService
{
    Task<IReadOnlyList<DunningRuleDto>> ListRulesAsync(
        Guid horizontalPropertyId,
        CancellationToken cancellationToken = default);

    Task<DunningRuleDto> UpsertRuleAsync(
        UpsertDunningRuleDto dto,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteRuleAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DunningSendLogDto>> ListRecentSendsAsync(
        Guid horizontalPropertyId,
        int take = 50,
        CancellationToken cancellationToken = default);
}
