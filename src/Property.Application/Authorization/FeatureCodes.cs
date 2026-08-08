namespace Property.Application.Authorization;

public static class FeatureCodes
{
    public const string DashboardView = "dashboard.view";
    public const string DashboardConfigure = "dashboard.configure";
    public const string SettingsTenant = "settings.tenant";
    public const string OrganizationEmployees = "organization.employees";
    public const string OrganizationTeam = "organization.team";
    public const string OrganizationRoles = "organization.roles";
    public const string StructureNeighborhoods = "structure.neighborhoods";
    public const string StructureProperties = "structure.properties";
    public const string StructureBuildings = "structure.buildings";
    public const string StructureFloors = "structure.floors";
    public const string StructureUnits = "structure.units";
    public const string PeopleManage = "people.manage";
    public const string BillingSettings = "billing.settings";
    public const string BillingCharges = "billing.charges";
    public const string BillingPayments = "billing.payments";
    public const string AmenitiesAreas = "amenities.areas";
    public const string AmenitiesReservations = "amenities.reservations";
    public const string ReceptionManage = "reception.manage";
    public const string ReportsView = "reports.view";

    public static IReadOnlyList<string> All { get; } =
    [
        DashboardView,
        DashboardConfigure,
        SettingsTenant,
        OrganizationEmployees,
        OrganizationTeam,
        OrganizationRoles,
        StructureNeighborhoods,
        StructureProperties,
        StructureBuildings,
        StructureFloors,
        StructureUnits,
        PeopleManage,
        BillingSettings,
        BillingCharges,
        BillingPayments,
        AmenitiesAreas,
        AmenitiesReservations,
        ReceptionManage,
        ReportsView,
    ];
}
