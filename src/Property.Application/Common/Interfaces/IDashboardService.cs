using Property.Application.Features.Dashboard;

namespace Property.Application.Common.Interfaces;

public interface IDashboardService
{
    Task<DashboardLayoutDto> GetLayoutAsync(CancellationToken cancellationToken = default);

    Task<DashboardLayoutDto> UpdateLayoutAsync(
        DashboardLayoutDto layout,
        CancellationToken cancellationToken = default);

    IReadOnlyList<DashboardWidgetDefinitionDto> GetCatalog();
}
