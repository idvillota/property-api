namespace Property.Application.Authorization;

public static class FeatureCodes
{
    public const string DashboardView = "dashboard.view";
    public const string DashboardConfigure = "dashboard.configure";
    public const string SettingsTenant = "settings.tenant";
    public const string OrganizationEmployees = "organization.employees";
    public const string OrganizationTeam = "organization.team";
    public const string OrganizationRoles = "organization.roles";

    public static IReadOnlyList<string> All { get; } =
    [
        DashboardView,
        DashboardConfigure,
        SettingsTenant,
        OrganizationEmployees,
        OrganizationTeam,
        OrganizationRoles,
    ];
}
