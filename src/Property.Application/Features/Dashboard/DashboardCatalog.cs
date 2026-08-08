using Property.Application.Authorization;

namespace Property.Application.Features.Dashboard;

public static class DashboardCatalog
{
    public static readonly DashboardWidgetDefinitionDto[] Widgets =
    [
        new()
        {
            WidgetType = "quick_links",
            Name = "Quick links",
            Description = "Shortcuts to the most used areas of the system.",
            Category = "General",
            RequiredPermission = FeatureCodes.DashboardView,
            DefaultWidth = 12,
            DefaultHeight = 3,
            MinWidth = 4,
            MinHeight = 2,
        },
    ];

    public static IReadOnlyDictionary<string, DashboardWidgetDefinitionDto> ByType { get; } =
        Widgets.ToDictionary(w => w.WidgetType, StringComparer.Ordinal);
}
