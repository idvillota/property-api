namespace Property.Application.Features.Dashboard;

public static class DefaultDashboardLayout
{
    public static DashboardLayoutDto Create() =>
        new()
        {
            Version = 1,
            Panels =
            [
                new DashboardPanelDto
                {
                    Id = "quick-links",
                    WidgetType = "quick_links",
                    X = 0,
                    Y = 0,
                    W = 12,
                    H = 3,
                },
            ],
        };
}
